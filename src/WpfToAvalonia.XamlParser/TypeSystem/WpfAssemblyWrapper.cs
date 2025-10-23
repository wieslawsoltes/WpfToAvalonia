using System.Reflection;
using XamlX.TypeSystem;
using WpfToAvalonia.Core.Diagnostics;

// Alias to disambiguate between XamlX types and our internal types
using XamlXType = XamlX.TypeSystem.IXamlType;
using XamlXAssembly = XamlX.TypeSystem.IXamlAssembly;
using XamlXCustomAttribute = XamlX.TypeSystem.IXamlCustomAttribute;

namespace WpfToAvalonia.XamlParser.TypeSystem;

/// <summary>
/// Wraps a CLR Assembly as an IXamlAssembly for XamlX.
/// </summary>
internal sealed class WpfAssemblyWrapper : XamlXAssembly
{
    private readonly Assembly _assembly;
    private readonly WpfTypeSystemProvider _typeSystem;
    private readonly DiagnosticCollector _diagnostics;
    private readonly Dictionary<string, WpfTypeWrapper> _typeCache = new();
    private readonly Lazy<List<XamlXCustomAttribute>> _customAttributes;

    public WpfAssemblyWrapper(
        Assembly assembly,
        WpfTypeSystemProvider typeSystem,
        DiagnosticCollector diagnostics)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        _customAttributes = new Lazy<List<XamlXCustomAttribute>>(() =>
            _assembly.GetCustomAttributesData()
                .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
                .Cast<XamlXCustomAttribute>()
                .ToList());
    }

    /// <summary>
    /// Gets the assembly name.
    /// </summary>
    public string Name => _assembly.GetName().Name ?? _assembly.FullName ?? "Unknown";

    /// <summary>
    /// Gets the custom attributes applied to this assembly.
    /// </summary>
    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes => _customAttributes.Value;

    /// <summary>
    /// Finds a type by its full name within this assembly.
    /// </summary>
    public XamlXType? FindType(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return null;
        }

        // Check cache first
        if (_typeCache.TryGetValue(fullName, out var cached))
        {
            return cached;
        }

        // Try to find the type in the assembly
        var type = _assembly.GetType(fullName);
        if (type != null)
        {
            var wrapper = _typeSystem.GetOrCreateType(type);
            _typeCache[fullName] = wrapper;
            return wrapper;
        }

        // Type not found
        return null;
    }

    /// <summary>
    /// Gets the underlying CLR assembly.
    /// </summary>
    public Assembly UnderlyingAssembly => _assembly;

    public bool Equals(XamlXAssembly? other)
    {
        if (other is WpfAssemblyWrapper wrapper)
        {
            return _assembly == wrapper._assembly;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is XamlXAssembly assembly)
        {
            return Equals(assembly);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _assembly.GetHashCode();
    }

    public override string ToString()
    {
        return $"WpfAssembly({Name})";
    }
}

/// <summary>
/// Wraps a CustomAttributeData as an IXamlCustomAttribute.
/// </summary>
internal sealed class WpfCustomAttributeWrapper : XamlXCustomAttribute
{
    private readonly CustomAttributeData _attributeData;
    private readonly WpfTypeSystemProvider _typeSystem;

    public WpfCustomAttributeWrapper(
        CustomAttributeData attributeData,
        WpfTypeSystemProvider typeSystem)
    {
        _attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    /// <summary>
    /// Gets the type of the attribute.
    /// </summary>
    public XamlXType Type => _typeSystem.GetOrCreateType(_attributeData.AttributeType);

    /// <summary>
    /// Gets the constructor arguments for the attribute.
    /// </summary>
    public List<object?> Parameters => _attributeData.ConstructorArguments
        .Select(a => a.Value)
        .ToList();

    /// <summary>
    /// Gets the named properties set on the attribute.
    /// </summary>
    public Dictionary<string, object?> Properties => _attributeData.NamedArguments
        .ToDictionary(
            a => a.MemberName,
            a => a.TypedValue.Value);

    public bool Equals(XamlXCustomAttribute? other)
    {
        if (other is WpfCustomAttributeWrapper wrapper)
        {
            return _attributeData == wrapper._attributeData;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlCustomAttribute attribute)
        {
            return Equals(attribute);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _attributeData.GetHashCode();
    }
}
