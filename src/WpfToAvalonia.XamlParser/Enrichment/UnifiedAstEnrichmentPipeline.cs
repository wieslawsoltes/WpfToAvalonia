using Microsoft.CodeAnalysis;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.Converters;
using WpfToAvalonia.XamlParser.UnifiedAst;
using XamlX.Ast;

namespace WpfToAvalonia.XamlParser.Enrichment;

/// <summary>
/// Pipeline for enriching XML-based Unified AST with semantic information from XamlX.
/// This is the key integration point that combines fast XML parsing with type-safe semantic analysis.
/// </summary>
public sealed class UnifiedAstEnrichmentPipeline
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly XamlAstToUnifiedConverter _semanticConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedAstEnrichmentPipeline"/> class.
    /// </summary>
    public UnifiedAstEnrichmentPipeline(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _semanticConverter = new XamlAstToUnifiedConverter(diagnostics);
    }

    /// <summary>
    /// Enriches an XML-based UnifiedXamlDocument with semantic information from XamlX and Roslyn.
    /// </summary>
    /// <param name="xmlDocument">The XML-based document (from XmlToUnifiedConverter)</param>
    /// <param name="xamlXDocument">The XamlX semantic document (optional)</param>
    /// <param name="semanticModel">The Roslyn semantic model for the code-behind (optional)</param>
    /// <returns>The enriched document with both XML formatting and type information</returns>
    public UnifiedXamlDocument Enrich(UnifiedXamlDocument xmlDocument, XamlDocument? xamlXDocument = null, SemanticModel? semanticModel = null)
    {
        if (xmlDocument == null)
        {
            throw new ArgumentNullException(nameof(xmlDocument));
        }

        // If no XamlX document provided, return XML-only document
        if (xamlXDocument == null)
        {
            _diagnostics.AddInfo(
                "ENRICHMENT_SKIPPED",
                "Semantic enrichment skipped - no XamlX document provided",
                xmlDocument.FilePath);
            return xmlDocument;
        }

        try
        {
            // Store the XamlX document reference
            xmlDocument.SemanticDocument = xamlXDocument;

            // Enrich the root element and all descendants
            if (xmlDocument.Root != null && xamlXDocument.Root is XamlAstObjectNode semanticRoot)
            {
                EnrichElement(xmlDocument.Root, semanticRoot);
            }

            // Build unified symbol table
            BuildSymbolTable(xmlDocument);

            // Cross-reference with Roslyn semantic model if provided
            if (semanticModel != null)
            {
                CrossReferenceWithRoslyn(xmlDocument, semanticModel);
            }

            // Validate consistency between XML and semantic layers
            ValidateConsistency(xmlDocument);

            _diagnostics.AddInfo(
                "ENRICHMENT_COMPLETED",
                $"Semantic enrichment completed successfully",
                xmlDocument.FilePath);

            return xmlDocument;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                "ENRICHMENT_FAILED",
                $"Semantic enrichment failed: {ex.Message}",
                xmlDocument.FilePath);
            return xmlDocument; // Return partially enriched document
        }
    }

    /// <summary>
    /// Enriches a UnifiedXamlElement with semantic information from XamlX AST node.
    /// Preserves all XML formatting while adding type information.
    /// </summary>
    private void EnrichElement(UnifiedXamlElement xmlElement, XamlAstObjectNode semanticNode)
    {
        // Attach semantic information using the converter
        _semanticConverter.EnrichElement(xmlElement, semanticNode);

        // Mark as semantically enriched
        xmlElement.State = TransformationState.Analyzed;

        // Recursively enrich children by matching XML and semantic nodes
        EnrichChildren(xmlElement, semanticNode);
    }

    /// <summary>
    /// Enriches child elements by matching XML children with semantic children.
    /// </summary>
    private void EnrichChildren(UnifiedXamlElement xmlElement, XamlAstObjectNode semanticNode)
    {
        // Process semantic node children
        foreach (var semanticChild in semanticNode.Children)
        {
            if (semanticChild is XamlAstXamlPropertyValueNode propertyNode)
            {
                // Find matching property in XML element
                var propertyName = ExtractPropertyName(propertyNode);
                var xmlProperty = xmlElement.GetProperty(propertyName);

                if (xmlProperty != null)
                {
                    EnrichProperty(xmlProperty, propertyNode);
                }
            }
            else if (semanticChild is XamlAstObjectNode childObjectNode)
            {
                // Try to match with XML children by position or type
                var xmlChild = FindMatchingChild(xmlElement, childObjectNode);
                if (xmlChild != null)
                {
                    EnrichElement(xmlChild, childObjectNode);
                }
            }
        }
    }

    /// <summary>
    /// Enriches a property with semantic information.
    /// </summary>
    private void EnrichProperty(UnifiedXamlProperty xmlProperty, XamlAstXamlPropertyValueNode semanticProperty)
    {
        // Enrich the property directly
        xmlProperty.SemanticProperty = semanticProperty;
        xmlProperty.SemanticNode = semanticProperty;

        // If property value is an element, enrich it
        if (xmlProperty.Value is UnifiedXamlElement elementValue &&
            semanticProperty.Values.Count > 0 &&
            semanticProperty.Values[0] is XamlAstObjectNode semanticValue)
        {
            EnrichElement(elementValue, semanticValue);
        }
    }

    /// <summary>
    /// Builds a unified symbol table for the document.
    /// Collects all types, properties, and resources from both XML and semantic layers.
    /// </summary>
    private void BuildSymbolTable(UnifiedXamlDocument document)
    {
        if (document.Root == null)
        {
            return;
        }

        var symbolTable = new UnifiedSymbolTable();

        // Collect all elements with x:Name (named elements)
        var namedElements = document.Root.DescendantsAndSelf()
            .Where(e => !string.IsNullOrEmpty(e.XName))
            .ToList();

        foreach (var element in namedElements)
        {
            var symbol = new NamedElementSymbol
            {
                Name = element.XName!,
                Element = element,
                Type = element.ElementType,
                Location = element.Location
            };

            symbolTable.NamedElements[element.XName!] = symbol;
        }

        // Collect all types used in the document
        var allElements = document.Root.DescendantsAndSelf().ToList();
        foreach (var element in allElements)
        {
            if (element.ElementType != null)
            {
                var typeName = element.ElementType.FullName;
                if (!symbolTable.Types.ContainsKey(typeName))
                {
                    symbolTable.Types[typeName] = element.ElementType;
                }
            }
        }

        // Store the symbol table in document metadata
        document.SetMetadata("SymbolTable", symbolTable);

        _diagnostics.AddInfo(
            "SYMBOL_TABLE_BUILT",
            $"Built symbol table: {symbolTable.NamedElements.Count} named elements, {symbolTable.Types.Count} types",
            document.FilePath);
    }

    /// <summary>
    /// Cross-references XAML elements with Roslyn semantic model.
    /// This links x:Name elements to their code-behind field declarations.
    /// </summary>
    private void CrossReferenceWithRoslyn(UnifiedXamlDocument document, SemanticModel semanticModel)
    {
        if (document.Root == null)
        {
            return;
        }

        // Get symbol table to access named elements
        var symbolTable = document.GetMetadata<UnifiedSymbolTable>("SymbolTable");
        if (symbolTable == null)
        {
            _diagnostics.AddWarning(
                "ROSLYN_NO_SYMBOL_TABLE",
                "Cannot cross-reference with Roslyn: Symbol table not found",
                document.FilePath);
            return;
        }

        // For each named element, try to find corresponding field in code-behind
        foreach (var namedElement in symbolTable.NamedElements)
        {
            var elementName = namedElement.Key;
            var element = namedElement.Value.Element;

            // Store the semantic model reference for later use
            element.SetMetadata("RoslynSemanticModel", semanticModel);

            // Try to find field declaration in code-behind
            var fieldSymbol = FindFieldInCodeBehind(semanticModel, elementName);
            if (fieldSymbol != null)
            {
                element.SetMetadata("CodeBehindField", fieldSymbol);

                _diagnostics.AddInfo(
                    "ROSLYN_FIELD_LINKED",
                    $"Linked XAML element '{elementName}' to code-behind field",
                    element.Location.FilePath,
                    element.Location.Line,
                    element.Location.Column);
            }
        }

        _diagnostics.AddInfo(
            "ROSLYN_CROSSREF_COMPLETED",
            $"Roslyn cross-referencing completed: {symbolTable.NamedElements.Count} named elements processed",
            document.FilePath);
    }

    /// <summary>
    /// Finds a field symbol in the code-behind semantic model by name.
    /// </summary>
    private IFieldSymbol? FindFieldInCodeBehind(SemanticModel semanticModel, string fieldName)
    {
        var root = semanticModel.SyntaxTree.GetRoot();

        // Look for field declarations with matching name
        var fieldDeclarations = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax>();

        foreach (var fieldDecl in fieldDeclarations)
        {
            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                if (variable.Identifier.Text == fieldName)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(variable);
                    if (symbol is IFieldSymbol fieldSymbol)
                    {
                        return fieldSymbol;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Validates consistency between XML and semantic layers.
    /// </summary>
    private void ValidateConsistency(UnifiedXamlDocument document)
    {
        if (document.Root == null)
        {
            return;
        }

        var inconsistencies = 0;

        // Check all elements for consistency
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            // Check if element has both XML and semantic info
            if (element.SourceXmlElement != null && element.SemanticObject == null)
            {
                _diagnostics.AddWarning(
                    "ENRICHMENT_INCOMPLETE",
                    $"Element {element.TypeName} has XML but no semantic information",
                    element.Location.FilePath,
                    element.Location.Line,
                    element.Location.Column);
                inconsistencies++;
            }

            // Validate type name consistency
            if (element.SourceXmlElement != null && element.ElementType != null)
            {
                var xmlTypeName = element.SourceXmlElement.Name.LocalName;
                var semanticTypeName = element.ElementType.Name;

                if (xmlTypeName != semanticTypeName)
                {
                    _diagnostics.AddWarning(
                        "TYPE_NAME_MISMATCH",
                        $"XML type name '{xmlTypeName}' doesn't match semantic type name '{semanticTypeName}'",
                        element.Location.FilePath,
                        element.Location.Line,
                        element.Location.Column);
                    inconsistencies++;
                }
            }
        }

        if (inconsistencies > 0)
        {
            _diagnostics.AddWarning(
                "CONSISTENCY_ISSUES",
                $"Found {inconsistencies} consistency issues between XML and semantic layers",
                document.FilePath);
        }
        else
        {
            _diagnostics.AddInfo(
                "CONSISTENCY_VALIDATED",
                "XML and semantic layers are consistent",
                document.FilePath);
        }
    }

    /// <summary>
    /// Extracts property name from XamlX property node.
    /// </summary>
    private string ExtractPropertyName(XamlAstXamlPropertyValueNode propertyNode)
    {
        if (propertyNode.Property is XamlAstClrProperty clrProperty)
        {
            return clrProperty.Name;
        }
        return "Unknown";
    }

    /// <summary>
    /// Finds a matching XML child for a semantic object node.
    /// Uses type information and position for matching.
    /// </summary>
    private UnifiedXamlElement? FindMatchingChild(UnifiedXamlElement xmlParent, XamlAstObjectNode semanticChild)
    {
        // Try to match by type name
        var semanticTypeName = semanticChild.Type.GetClrType()?.Name;
        if (semanticTypeName != null)
        {
            var match = xmlParent.Children.FirstOrDefault(c => c.TypeName == semanticTypeName);
            if (match != null)
            {
                return match;
            }
        }

        // Fallback: match by position (if counts match)
        // This is less reliable but works when types don't match exactly
        // TODO: Implement more sophisticated matching logic

        return null;
    }
}

/// <summary>
/// Unified symbol table containing all named elements, types, and resources.
/// </summary>
public sealed class UnifiedSymbolTable
{
    /// <summary>
    /// Gets named elements (x:Name) mapped by name.
    /// </summary>
    public Dictionary<string, NamedElementSymbol> NamedElements { get; } = new();

    /// <summary>
    /// Gets all types used in the document mapped by full name.
    /// </summary>
    public Dictionary<string, TypeSystem.IXamlType> Types { get; } = new();

    /// <summary>
    /// Gets resources defined in the document mapped by key.
    /// </summary>
    public Dictionary<string, ResourceSymbol> Resources { get; } = new();
}

/// <summary>
/// Represents a named element symbol (x:Name).
/// </summary>
public sealed class NamedElementSymbol
{
    /// <summary>
    /// Gets or sets the name of the element.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the element reference.
    /// </summary>
    public required UnifiedXamlElement Element { get; set; }

    /// <summary>
    /// Gets or sets the type of the element.
    /// </summary>
    public TypeSystem.IXamlType? Type { get; set; }

    /// <summary>
    /// Gets or sets the source location.
    /// </summary>
    public required SourceLocation Location { get; set; }
}

/// <summary>
/// Represents a resource symbol (x:Key in ResourceDictionary).
/// </summary>
public sealed class ResourceSymbol
{
    /// <summary>
    /// Gets or sets the resource key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the resource value element.
    /// </summary>
    public required UnifiedXamlElement Value { get; set; }

    /// <summary>
    /// Gets or sets the resource type.
    /// </summary>
    public TypeSystem.IXamlType? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the source location.
    /// </summary>
    public required SourceLocation Location { get; set; }
}
