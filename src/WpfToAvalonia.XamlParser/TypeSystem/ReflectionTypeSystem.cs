using System.Reflection;

namespace WpfToAvalonia.XamlParser.TypeSystem;

/// <summary>
/// System.Reflection-based implementation of IXamlType.
/// </summary>
public sealed class ReflectionXamlType : IXamlType
{
    private readonly Type _type;

    public ReflectionXamlType(Type type)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
    }

    public object Id => _type;

    public string Name => _type.Name;

    public string? Namespace => _type.Namespace;

    public string FullName => _type.FullName ?? _type.Name;

    public bool IsPublic => _type.IsPublic;

    public IXamlAssembly? Assembly => _type.Assembly != null ? new ReflectionXamlAssembly(_type.Assembly) : null;

    private IReadOnlyList<IXamlProperty>? _properties;
    public IReadOnlyList<IXamlProperty> Properties
    {
        get
        {
            if (_properties == null)
            {
                _properties = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(p => (IXamlProperty)new ReflectionXamlProperty(p))
                    .ToList();
            }
            return _properties;
        }
    }

    public IXamlType? BaseType => _type.BaseType != null ? new ReflectionXamlType(_type.BaseType) : null;

    public bool IsValueType => _type.IsValueType;

    public bool IsEnum => _type.IsEnum;

    private IReadOnlyList<IXamlType>? _interfaces;
    public IReadOnlyList<IXamlType> Interfaces
    {
        get
        {
            if (_interfaces == null)
            {
                _interfaces = _type.GetInterfaces()
                    .Select(i => (IXamlType)new ReflectionXamlType(i))
                    .ToList();
            }
            return _interfaces;
        }
    }

    public bool IsAssignableFrom(IXamlType type)
    {
        if (type is ReflectionXamlType reflectionType)
        {
            return _type.IsAssignableFrom(reflectionType._type);
        }
        return false;
    }

    public Type UnderlyingType => _type;

    public override bool Equals(object? obj)
    {
        return obj is ReflectionXamlType other && _type == other._type;
    }

    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }

    public override string ToString()
    {
        return FullName;
    }
}

/// <summary>
/// System.Reflection-based implementation of IXamlProperty.
/// </summary>
public sealed class ReflectionXamlProperty : IXamlProperty
{
    private readonly PropertyInfo _property;

    public ReflectionXamlProperty(PropertyInfo property)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public string Name => _property.Name;

    public IXamlType PropertyType => new ReflectionXamlType(_property.PropertyType);

    public IXamlType? DeclaringType => _property.DeclaringType != null
        ? new ReflectionXamlType(_property.DeclaringType)
        : null;

    public bool IsAttached => false; // Regular reflection doesn't distinguish attached properties

    public bool CanRead => _property.CanRead;

    public bool CanWrite => _property.CanWrite;

    public PropertyInfo UnderlyingProperty => _property;

    public override bool Equals(object? obj)
    {
        return obj is ReflectionXamlProperty other && _property == other._property;
    }

    public override int GetHashCode()
    {
        return _property.GetHashCode();
    }

    public override string ToString()
    {
        return $"{DeclaringType?.Name}.{Name}";
    }
}

/// <summary>
/// System.Reflection-based implementation of IXamlAssembly.
/// </summary>
public sealed class ReflectionXamlAssembly : IXamlAssembly
{
    private readonly Assembly _assembly;

    public ReflectionXamlAssembly(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public string Name => _assembly.GetName().Name ?? _assembly.FullName ?? "Unknown";

    public IXamlType? FindType(string fullName)
    {
        var type = _assembly.GetType(fullName);
        return type != null ? new ReflectionXamlType(type) : null;
    }

    public Assembly UnderlyingAssembly => _assembly;

    public override bool Equals(object? obj)
    {
        return obj is ReflectionXamlAssembly other && _assembly == other._assembly;
    }

    public override int GetHashCode()
    {
        return _assembly.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }
}

/// <summary>
/// System.Reflection-based implementation of IXamlTypeResolver.
/// </summary>
public sealed class ReflectionTypeResolver : IXamlTypeResolver
{
    private readonly Dictionary<string, string> _xmlNamespaceToClrNamespace = new();
    private readonly List<Assembly> _assemblies = new();

    public ReflectionTypeResolver()
    {
        // Add common WPF XML namespace mappings
        _xmlNamespaceToClrNamespace["http://schemas.microsoft.com/winfx/2006/xaml/presentation"] = "System.Windows";
        _xmlNamespaceToClrNamespace["http://schemas.microsoft.com/winfx/2006/xaml"] = "System.Windows.Markup";
    }

    /// <summary>
    /// Adds an assembly to search for types.
    /// </summary>
    public void AddAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }
    }

    /// <summary>
    /// Adds an XML namespace mapping.
    /// </summary>
    public void AddNamespaceMapping(string xmlNamespace, string clrNamespace)
    {
        _xmlNamespaceToClrNamespace[xmlNamespace] = clrNamespace;
    }

    public IXamlType? ResolveType(string xmlNamespace, string typeName)
    {
        // Try to find CLR namespace mapping
        if (_xmlNamespaceToClrNamespace.TryGetValue(xmlNamespace, out var clrNamespace))
        {
            var fullName = $"{clrNamespace}.{typeName}";
            return ResolveType(fullName);
        }

        // Try using-clr-namespace syntax
        if (xmlNamespace.StartsWith("clr-namespace:"))
        {
            var parts = xmlNamespace.Split(';');
            var ns = parts[0].Substring("clr-namespace:".Length);
            var assemblyName = parts.Length > 1 ? parts[1].Substring("assembly=".Length) : null;

            var fullName = $"{ns}.{typeName}";

            if (assemblyName != null)
            {
                // Search specific assembly
                var assembly = _assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);
                if (assembly != null)
                {
                    var type = assembly.GetType(fullName);
                    if (type != null)
                    {
                        return new ReflectionXamlType(type);
                    }
                }
            }

            return ResolveType(fullName);
        }

        return null;
    }

    public IXamlType? ResolveType(string fullTypeName)
    {
        // Search all loaded assemblies
        foreach (var assembly in _assemblies)
        {
            var type = assembly.GetType(fullTypeName);
            if (type != null)
            {
                return new ReflectionXamlType(type);
            }
        }

        // Try Type.GetType as fallback
        var fallbackType = Type.GetType(fullTypeName);
        if (fallbackType != null)
        {
            return new ReflectionXamlType(fallbackType);
        }

        return null;
    }

    public IXamlType? GetType(object clrType)
    {
        if (clrType is Type type)
        {
            return new ReflectionXamlType(type);
        }

        return null;
    }
}
