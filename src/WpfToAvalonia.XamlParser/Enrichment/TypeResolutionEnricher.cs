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
    private readonly TypeResolutionOptions _options;
    private readonly List<UnresolvedTypeInfo> _unresolvedTypes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolutionEnricher"/> class with default options.
    /// </summary>
    public TypeResolutionEnricher(IXamlTypeResolver typeResolver)
        : this(typeResolver, TypeResolutionOptions.Default())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolutionEnricher"/> class with custom options.
    /// </summary>
    public TypeResolutionEnricher(IXamlTypeResolver typeResolver, TypeResolutionOptions options)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

        // Clear any previous unresolved types
        _unresolvedTypes.Clear();

        // Visit the tree and resolve types
        VisitDocument(document);

        // Check if we have unresolved types and policy requires resolution
        if (_unresolvedTypes.Count > 0 && _options.Policy == TypeResolutionPolicy.Required)
        {
            throw new TypeResolutionException(_unresolvedTypes);
        }
    }

    /// <summary>
    /// Visits an element and resolves its type.
    /// </summary>
    public override void VisitElement(UnifiedXamlElement element)
    {
        // Resolve element type
        if (element.ResolvedType == null && element.ElementType == null)
        {
            #pragma warning disable CS0618 // TypeName/Namespace are obsolete but needed for backward compat
            var xmlNs = element.XmlNamespace?.NamespaceName ?? element.Namespace ?? string.Empty;
            var typeName = element.TypeReference?.FullName ?? element.TypeName;
            #pragma warning restore CS0618

            var resolvedType = _typeResolver.ResolveType(xmlNs, typeName);

            if (resolvedType != null)
            {
                element.ResolvedType = resolvedType;
                element.ElementType = resolvedType;
                element.State = TransformationState.Analyzed;

                // Update TypeReference if present
                if (element.TypeReference != null)
                {
                    element.TypeReference = element.TypeReference.WithResolvedType(resolvedType);
                }
            }
            else
            {
                // Type resolution failed
                HandleUnresolvedType(element, typeName, xmlNs);

                // If fail-fast is enabled and policy is Required, throw immediately
                if (_options.Policy == TypeResolutionPolicy.Required && _options.FailFast)
                {
                    throw new TypeResolutionException($"Cannot resolve type: {typeName}", element);
                }
            }
        }

        // Continue visiting children
        base.VisitElement(element);
    }

    private void HandleUnresolvedType(UnifiedXamlElement element, string typeName, string xmlNs)
    {
        element.State = TransformationState.Failed;

        var severity = _options.Policy == TypeResolutionPolicy.Required
            ? Core.Diagnostics.DiagnosticSeverity.Error
            : Core.Diagnostics.DiagnosticSeverity.Warning;

        element.AddDiagnostic(
            "TYPE_NOT_FOUND",
            $"Could not resolve type '{typeName}' in namespace '{xmlNs}'",
            severity);

        // Track unresolved type for batch reporting
        _unresolvedTypes.Add(new UnresolvedTypeInfo
        {
            TypeName = typeName,
            Location = element.Location,
            Element = element,
            Namespace = xmlNs
        });
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
