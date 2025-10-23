using WpfToAvalonia.XamlParser.TypeSystem;
using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.XamlParser.Visitors;

namespace WpfToAvalonia.XamlParser.Enrichment;

/// <summary>
/// Interface for AST enrichers.
/// </summary>
public interface IEnricher
{
    /// <summary>
    /// Enriches a XAML document with additional semantic information.
    /// </summary>
    void Enrich(UnifiedXamlDocument document);
}

/// <summary>
/// Enriches the Unified AST with type information by resolving types for elements and properties.
/// This is part of the semantic analysis phase.
/// </summary>
public sealed class TypeResolutionEnricher : UnifiedXamlVisitorBase, IEnricher
{
    private readonly IXamlTypeResolver _typeResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolutionEnricher"/> class.
    /// </summary>
    public TypeResolutionEnricher(IXamlTypeResolver typeResolver)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
    }

    /// <summary>
    /// Enriches a XAML document with type information.
    /// </summary>
    public void Enrich(UnifiedXamlDocument document)
    {
        if (document.Root == null)
        {
            return;
        }

        // Visit the tree and resolve types
        VisitDocument(document);
    }

    /// <summary>
    /// Visits an element and resolves its type.
    /// </summary>
    public override void VisitElement(UnifiedXamlElement element)
    {
        // Resolve element type
        if (element.ResolvedType == null)
        {
            var xmlNs = element.XmlNamespace?.NamespaceName ?? element.Namespace ?? string.Empty;
            var resolvedType = _typeResolver.ResolveType(xmlNs, element.TypeName);

            if (resolvedType != null)
            {
                element.ResolvedType = resolvedType;
                element.State = TransformationState.Analyzed;
            }
            else
            {
                element.State = TransformationState.Failed;
                element.AddDiagnostic(
                    "TYPE_NOT_FOUND",
                    $"Could not resolve type '{element.TypeName}' in namespace '{xmlNs}'",
                    Core.Diagnostics.DiagnosticSeverity.Warning);
            }
        }

        // Continue visiting children
        base.VisitElement(element);
    }

    /// <summary>
    /// Visits a property and resolves its type.
    /// </summary>
    public override void VisitProperty(UnifiedXamlProperty property)
    {
        // Resolve property type from parent element's type
        if (property.PropertyInfo == null && property.Parent is UnifiedXamlElement parentElement)
        {
            if (parentElement.ResolvedType != null)
            {
                // Find the property on the parent type
                var propertyInfo = parentElement.ResolvedType.Properties
                    .FirstOrDefault(p => p.Name == property.PropertyName);

                if (propertyInfo != null)
                {
                    property.PropertyInfo = propertyInfo;
                    property.PropertyType = propertyInfo.PropertyType;
                    property.ResolvedType = propertyInfo.PropertyType;
                    property.State = TransformationState.Analyzed;
                }
                else if (property.IsAttached)
                {
                    // Try to resolve attached property
                    var ownerType = _typeResolver.ResolveType(property.AttachedOwnerType ?? string.Empty);
                    if (ownerType != null)
                    {
                        // Look for Get{PropertyName} method
                        property.State = TransformationState.Analyzed;
                    }
                    else
                    {
                        property.State = TransformationState.Failed;
                        property.AddDiagnostic(
                            "ATTACHED_PROPERTY_OWNER_NOT_FOUND",
                            $"Could not resolve attached property owner type '{property.AttachedOwnerType}'",
                            Core.Diagnostics.DiagnosticSeverity.Warning);
                    }
                }
                else
                {
                    property.State = TransformationState.Failed;
                    property.AddDiagnostic(
                        "PROPERTY_NOT_FOUND",
                        $"Property '{property.PropertyName}' not found on type '{parentElement.ResolvedType.FullName}'",
                        Core.Diagnostics.DiagnosticSeverity.Warning);
                }
            }
        }

        // Continue visiting
        base.VisitProperty(property);
    }
}
