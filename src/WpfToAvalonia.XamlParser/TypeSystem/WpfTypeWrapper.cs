using System.Reflection;
using XamlX.TypeSystem;
using WpfToAvalonia.Core.Diagnostics;

// Alias to disambiguate between XamlX types and our internal types
using XamlXType = XamlX.TypeSystem.IXamlType;
using XamlXProperty = XamlX.TypeSystem.IXamlProperty;
using XamlXMethod = XamlX.TypeSystem.IXamlMethod;
using XamlXField = XamlX.TypeSystem.IXamlField;
using XamlXConstructor = XamlX.TypeSystem.IXamlConstructor;
using XamlXEventInfo = XamlX.TypeSystem.IXamlEventInfo;
using XamlXAssembly = XamlX.TypeSystem.IXamlAssembly;
using XamlXCustomAttribute = XamlX.TypeSystem.IXamlCustomAttribute;
using XamlXParameterInfo = XamlX.TypeSystem.IXamlParameterInfo;

namespace WpfToAvalonia.XamlParser.TypeSystem;

/// <summary>
/// Wraps a CLR Type as an IXamlType for XamlX.
/// Handles WPF-specific concepts like DependencyProperty.
/// </summary>
internal sealed class WpfTypeWrapper : XamlXType
{
    private readonly Type _type;
    private readonly WpfAssemblyWrapper _assembly;
    private readonly WpfTypeSystemProvider _typeSystem;
    private readonly DiagnosticCollector _diagnostics;
    private readonly Lazy<List<XamlXProperty>> _properties;
    private readonly Lazy<List<XamlXMethod>> _methods;
    private readonly Lazy<List<XamlXField>> _fields;
    private readonly Lazy<List<XamlXConstructor>> _constructors;
    private readonly Lazy<List<XamlXCustomAttribute>> _customAttributes;
    private readonly Lazy<List<XamlXEventInfo>> _events;

    public WpfTypeWrapper(
        Type type,
        WpfAssemblyWrapper assembly,
        WpfTypeSystemProvider typeSystem,
        DiagnosticCollector diagnostics)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        _properties = new Lazy<List<XamlXProperty>>(LoadProperties);
        _methods = new Lazy<List<XamlXMethod>>(LoadMethods);
        _fields = new Lazy<List<XamlXField>>(LoadFields);
        _constructors = new Lazy<List<XamlXConstructor>>(LoadConstructors);
        _customAttributes = new Lazy<List<XamlXCustomAttribute>>(LoadCustomAttributes);
        _events = new Lazy<List<XamlXEventInfo>>(LoadEvents);
    }

    /// <summary>
    /// Gets a unique identifier for this type.
    /// </summary>
    public object Id => _type;

    /// <summary>
    /// Gets the assembly containing this type.
    /// </summary>
    public XamlXAssembly? Assembly => _assembly;

    /// <summary>
    /// Gets the namespace of this type.
    /// </summary>
    public string? Namespace => _type.Namespace;

    /// <summary>
    /// Gets the name of this type (without namespace).
    /// </summary>
    public string Name => _type.Name;

    /// <summary>
    /// Gets the full name of this type (with namespace).
    /// </summary>
    public string FullName => _type.FullName ?? $"{Namespace}.{Name}";

    /// <summary>
    /// Gets whether this type is public.
    /// </summary>
    public bool IsPublic => _type.IsPublic || _type.IsNestedPublic;

    /// <summary>
    /// Gets whether this type is nested private.
    /// </summary>
    public bool IsNestedPrivate => _type.IsNestedPrivate;

    /// <summary>
    /// Gets whether this type is an interface.
    /// </summary>
    public bool IsInterface => _type.IsInterface;

    /// <summary>
    /// Gets whether this type is abstract.
    /// </summary>
    public bool IsAbstract => _type.IsAbstract;

    /// <summary>
    /// Gets whether this type is a value type (struct or enum).
    /// </summary>
    public bool IsValueType => _type.IsValueType;

    /// <summary>
    /// Gets whether this type is an enum.
    /// </summary>
    public bool IsEnum => _type.IsEnum;

    /// <summary>
    /// Gets whether this type is an array.
    /// </summary>
    public bool IsArray => _type.IsArray;

    /// <summary>
    /// Gets the array element type if this is an array.
    /// </summary>
    public XamlXType? ArrayElementType => _type.IsArray
        ? _typeSystem.GetOrCreateType(_type.GetElementType()!)
        : null;

    /// <summary>
    /// Gets the generic type definition if this is a generic type.
    /// </summary>
    public XamlXType? GenericTypeDefinition => _type.IsGenericType
        ? _typeSystem.GetOrCreateType(_type.GetGenericTypeDefinition())
        : null;

    /// <summary>
    /// Gets the generic arguments if this is a generic type.
    /// </summary>
    public IReadOnlyList<XamlXType> GenericArguments => _type.IsGenericType
        ? _type.GetGenericArguments().Select(t => _typeSystem.GetOrCreateType(t)).ToList()
        : Array.Empty<XamlXType>();

    /// <summary>
    /// Gets the generic parameters if this is a generic type definition.
    /// </summary>
    public IReadOnlyList<XamlXType> GenericParameters => _type.IsGenericTypeDefinition
        ? _type.GetGenericArguments().Select(t => _typeSystem.GetOrCreateType(t)).ToList()
        : Array.Empty<XamlXType>();

    /// <summary>
    /// Gets the base type of this type.
    /// </summary>
    public XamlXType? BaseType => _type.BaseType != null
        ? _typeSystem.GetOrCreateType(_type.BaseType)
        : null;

    /// <summary>
    /// Gets the declaring type if this is a nested type.
    /// </summary>
    public XamlXType? DeclaringType => _type.DeclaringType != null
        ? _typeSystem.GetOrCreateType(_type.DeclaringType)
        : null;

    /// <summary>
    /// Gets whether this type is a function pointer (always false for WPF types).
    /// </summary>
    public bool IsFunctionPointer => false;

    /// <summary>
    /// Gets the interfaces implemented by this type.
    /// </summary>
    public IReadOnlyList<XamlXType> Interfaces => _type.GetInterfaces()
        .Select(i => _typeSystem.GetOrCreateType(i))
        .ToList();

    /// <summary>
    /// Gets the properties of this type.
    /// Includes both CLR properties and WPF dependency properties.
    /// </summary>
    public IReadOnlyList<XamlXProperty> Properties => _properties.Value;

    /// <summary>
    /// Gets the methods of this type.
    /// </summary>
    public IReadOnlyList<XamlXMethod> Methods => _methods.Value;

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    public IReadOnlyList<XamlXField> Fields => _fields.Value;

    /// <summary>
    /// Gets the constructors of this type.
    /// </summary>
    public IReadOnlyList<XamlXConstructor> Constructors => _constructors.Value;

    /// <summary>
    /// Gets the custom attributes applied to this type.
    /// </summary>
    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes => _customAttributes.Value;

    /// <summary>
    /// Gets the events defined on this type.
    /// </summary>
    public IReadOnlyList<XamlXEventInfo> Events => _events.Value;

    /// <summary>
    /// Gets the underlying CLR type.
    /// </summary>
    public Type UnderlyingType => _type;

    /// <summary>
    /// Checks if this type is assignable from another type.
    /// </summary>
    public bool IsAssignableFrom(XamlXType type)
    {
        if (type is WpfTypeWrapper wpfType)
        {
            return _type.IsAssignableFrom(wpfType._type);
        }
        return false;
    }

    /// <summary>
    /// Checks if this type is assignable to another type.
    /// </summary>
    public bool IsAssignableTo(XamlXType type)
    {
        if (type is WpfTypeWrapper wpfType)
        {
            return wpfType._type.IsAssignableFrom(_type);
        }
        return false;
    }

    /// <summary>
    /// Creates a generic type from this generic type definition.
    /// </summary>
    public XamlXType MakeGenericType(IReadOnlyList<XamlXType> typeArguments)
    {
        var clrTypeArguments = typeArguments
            .OfType<WpfTypeWrapper>()
            .Select(t => t._type)
            .ToArray();

        var genericType = _type.MakeGenericType(clrTypeArguments);
        return _typeSystem.GetOrCreateType(genericType);
    }

    /// <summary>
    /// Creates an array type from this element type.
    /// </summary>
    public XamlXType MakeArrayType(int dimensions)
    {
        var arrayType = dimensions == 1
            ? _type.MakeArrayType()
            : _type.MakeArrayType(dimensions);

        return _typeSystem.GetOrCreateType(arrayType);
    }

    /// <summary>
    /// Gets a property by name.
    /// </summary>
    public XamlXProperty? GetProperty(string name)
    {
        return Properties.FirstOrDefault(p => p.Name == name);
    }

    /// <summary>
    /// Gets the underlying type for an enum.
    /// </summary>
    public XamlXType GetEnumUnderlyingType()
    {
        if (!IsEnum)
        {
            throw new InvalidOperationException($"Type {FullName} is not an enum");
        }

        var underlyingType = Enum.GetUnderlyingType(_type);
        return _typeSystem.GetOrCreateType(underlyingType);
    }

    /// <summary>
    /// Loads all properties (both CLR properties and dependency properties).
    /// </summary>
    private List<XamlXProperty> LoadProperties()
    {
        var properties = new List<XamlXProperty>();

        // Load CLR properties
        var clrProperties = _type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var property in clrProperties)
        {
            properties.Add(new WpfPropertyWrapper(property, this, _typeSystem));
        }

        // Load dependency properties
        var dependencyProperties = FindDependencyProperties();
        foreach (var dpField in dependencyProperties)
        {
            // Check if we already have a CLR property wrapper for this dependency property
            var propertyName = GetDependencyPropertyName(dpField.Name);
            var existingProperty = properties.FirstOrDefault(p => p.Name == propertyName);

            if (existingProperty is WpfPropertyWrapper clrWrapper)
            {
                // Mark the CLR property as backed by a dependency property
                clrWrapper.SetDependencyPropertyField(dpField);
            }
            else
            {
                // Create a property wrapper for the dependency property
                properties.Add(new WpfDependencyPropertyWrapper(dpField, this, _typeSystem, _diagnostics));
            }
        }

        return properties;
    }

    /// <summary>
    /// Finds all dependency property fields in this type.
    /// </summary>
    private List<FieldInfo> FindDependencyProperties()
    {
        var dpFields = new List<FieldInfo>();

        var fields = _type.GetFields(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var field in fields)
        {
            // Check if field is a DependencyProperty
            if (IsDependencyPropertyField(field))
            {
                dpFields.Add(field);
                _diagnostics.AddInfo(
                    "WPF_DP_FOUND",
                    $"Found dependency property: {_type.Name}.{field.Name}",
                    null);
            }
        }

        return dpFields;
    }

    /// <summary>
    /// Determines if a field is a DependencyProperty field.
    /// </summary>
    private bool IsDependencyPropertyField(FieldInfo field)
    {
        // Must be static, readonly, and named *Property
        if (!field.IsStatic || !field.IsInitOnly)
        {
            return false;
        }

        if (!field.Name.EndsWith("Property"))
        {
            return false;
        }

        // Type must be DependencyProperty (or we can't load WPF assemblies, so check by name)
        return field.FieldType.FullName == "System.Windows.DependencyProperty";
    }

    /// <summary>
    /// Gets the property name from a dependency property field name.
    /// E.g., "TitleProperty" -> "Title"
    /// </summary>
    private string GetDependencyPropertyName(string fieldName)
    {
        if (fieldName.EndsWith("Property"))
        {
            return fieldName.Substring(0, fieldName.Length - "Property".Length);
        }
        return fieldName;
    }

    /// <summary>
    /// Loads all methods.
    /// </summary>
    private List<XamlXMethod> LoadMethods()
    {
        var methods = _type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return methods
            .Select(m => new WpfMethodWrapper(m, this, _typeSystem))
            .Cast<XamlXMethod>()
            .ToList();
    }

    /// <summary>
    /// Loads all fields.
    /// </summary>
    private List<XamlXField> LoadFields()
    {
        var fields = _type.GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields
            .Select(f => new WpfFieldWrapper(f, this, _typeSystem))
            .Cast<XamlXField>()
            .ToList();
    }

    /// <summary>
    /// Loads all constructors.
    /// </summary>
    private List<XamlXConstructor> LoadConstructors()
    {
        var constructors = _type.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance);

        return constructors
            .Select(c => new WpfConstructorWrapper(c, this, _typeSystem))
            .Cast<XamlXConstructor>()
            .ToList();
    }

    /// <summary>
    /// Loads all custom attributes.
    /// </summary>
    private List<XamlXCustomAttribute> LoadCustomAttributes()
    {
        return _type.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();
    }

    /// <summary>
    /// Loads all events.
    /// </summary>
    private List<XamlXEventInfo> LoadEvents()
    {
        var events = _type.GetEvents(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return events
            .Select(e => new WpfEventInfoWrapper(e, this, _typeSystem))
            .Cast<XamlXEventInfo>()
            .ToList();
    }

    public bool Equals(XamlXType? other)
    {
        if (other is WpfTypeWrapper wrapper)
        {
            return _type == wrapper._type;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is XamlXType xamlType)
        {
            return Equals(xamlType);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }

    public override string ToString()
    {
        return $"WpfType({FullName})";
    }
}
