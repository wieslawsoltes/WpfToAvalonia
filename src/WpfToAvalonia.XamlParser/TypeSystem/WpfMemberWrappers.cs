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
using XamlXCustomAttribute = XamlX.TypeSystem.IXamlCustomAttribute;
using XamlXParameterInfo = XamlX.TypeSystem.IXamlParameterInfo;

namespace WpfToAvalonia.XamlParser.TypeSystem;

/// <summary>
/// Wraps a CLR PropertyInfo as an IXamlProperty.
/// </summary>
internal sealed class WpfPropertyWrapper : XamlXProperty
{
    private readonly PropertyInfo _property;
    private readonly WpfTypeWrapper _declaringType;
    private readonly WpfTypeSystemProvider _typeSystem;
    private FieldInfo? _dependencyPropertyField;

    public WpfPropertyWrapper(
        PropertyInfo property,
        WpfTypeWrapper declaringType,
        WpfTypeSystemProvider typeSystem)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    public string Name => _property.Name;
    public XamlXType PropertyType => _typeSystem.GetOrCreateType(_property.PropertyType);
    public XamlXType DeclaringType => _declaringType;
    public XamlXMethod? Getter => _property.GetMethod != null
        ? new WpfMethodWrapper(_property.GetMethod, _declaringType, _typeSystem)
        : null;
    public XamlXMethod? Setter => _property.SetMethod != null
        ? new WpfMethodWrapper(_property.SetMethod, _declaringType, _typeSystem)
        : null;

    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes =>
        _property.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();

    public IReadOnlyList<XamlXType> IndexerParameters =>
        _property.GetIndexParameters()
            .Select(p => _typeSystem.GetOrCreateType(p.ParameterType))
            .ToList();

    public bool IsAttached => false; // Will be true for attached properties in a separate wrapper
    public bool CanRead => _property.CanRead;
    public bool CanWrite => _property.CanWrite;

    /// <summary>
    /// Gets whether this property is backed by a WPF dependency property.
    /// </summary>
    public bool IsDependencyProperty => _dependencyPropertyField != null;

    /// <summary>
    /// Gets the dependency property field if this is a dependency property.
    /// </summary>
    public FieldInfo? DependencyPropertyField => _dependencyPropertyField;

    /// <summary>
    /// Marks this property as backed by a dependency property.
    /// </summary>
    internal void SetDependencyPropertyField(FieldInfo field)
    {
        _dependencyPropertyField = field;
    }

    public bool Equals(XamlXProperty? other)
    {
        if (other is WpfPropertyWrapper wrapper)
        {
            return _property == wrapper._property;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlProperty property)
        {
            return Equals(property);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _property.GetHashCode();
    }

    public PropertyInfo UnderlyingProperty => _property;
}

/// <summary>
/// Wraps a WPF DependencyProperty field as an IXamlProperty.
/// Used when there's no CLR property wrapper for the dependency property.
/// </summary>
internal sealed class WpfDependencyPropertyWrapper : XamlXProperty
{
    private readonly FieldInfo _dependencyPropertyField;
    private readonly WpfTypeWrapper _declaringType;
    private readonly WpfTypeSystemProvider _typeSystem;
    private readonly DiagnosticCollector _diagnostics;
    private readonly Lazy<XamlXType> _propertyType;

    public WpfDependencyPropertyWrapper(
        FieldInfo dependencyPropertyField,
        WpfTypeWrapper declaringType,
        WpfTypeSystemProvider typeSystem,
        DiagnosticCollector diagnostics)
    {
        _dependencyPropertyField = dependencyPropertyField ?? throw new ArgumentNullException(nameof(dependencyPropertyField));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        _propertyType = new Lazy<XamlXType>(ResolvePropertyType);
    }

    public string Name
    {
        get
        {
            var fieldName = _dependencyPropertyField.Name;
            return fieldName.EndsWith("Property")
                ? fieldName.Substring(0, fieldName.Length - "Property".Length)
                : fieldName;
        }
    }

    public XamlXType PropertyType => _propertyType.Value;
    public XamlXType DeclaringType => _declaringType;
    public XamlXMethod? Getter => null; // DPs without CLR wrappers don't have getters in the type system
    public XamlXMethod? Setter => null; // DPs without CLR wrappers don't have setters in the type system

    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes =>
        _dependencyPropertyField.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();

    public IReadOnlyList<XamlXType> IndexerParameters => Array.Empty<XamlXType>();

    public bool IsAttached => false; // Could be determined by checking for Get/Set methods
    public bool CanRead => true; // Dependency properties are always readable via GetValue
    public bool CanWrite => true; // Dependency properties are always writable via SetValue

    public bool Equals(XamlXProperty? other)
    {
        if (other is WpfDependencyPropertyWrapper wrapper)
        {
            return _dependencyPropertyField == wrapper._dependencyPropertyField;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlProperty property)
        {
            return Equals(property);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _dependencyPropertyField.GetHashCode();
    }

    /// <summary>
    /// Resolves the property type by examining the DependencyProperty.Register call.
    /// This is a heuristic approach since we can't evaluate the static field at compile time.
    /// </summary>
    private XamlXType ResolvePropertyType()
    {
        // Try to find a CLR property with the same name
        var clrProperty = _declaringType.UnderlyingType.GetProperty(
            Name,
            BindingFlags.Public | BindingFlags.Instance);

        if (clrProperty != null)
        {
            return _typeSystem.GetOrCreateType(clrProperty.PropertyType);
        }

        _diagnostics.AddWarning(
            "WPF_DP_TYPE_UNKNOWN",
            $"Could not resolve type for dependency property {_declaringType.Name}.{Name}, defaulting to object",
            null);

        // Default to object type
        return _typeSystem.GetOrCreateType(typeof(object));
    }

    public FieldInfo DependencyPropertyField => _dependencyPropertyField;
}

/// <summary>
/// Wraps a CLR MethodInfo as an IXamlMethod.
/// </summary>
internal sealed class WpfMethodWrapper : XamlXMethod
{
    private readonly MethodInfo _method;
    private readonly WpfTypeWrapper _declaringType;
    private readonly WpfTypeSystemProvider _typeSystem;

    public WpfMethodWrapper(
        MethodInfo method,
        WpfTypeWrapper declaringType,
        WpfTypeSystemProvider typeSystem)
    {
        _method = method ?? throw new ArgumentNullException(nameof(method));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    public string Name => _method.Name;
    public XamlXType ReturnType => _typeSystem.GetOrCreateType(_method.ReturnType);
    public XamlXType DeclaringType => _declaringType;
    public bool IsPublic => _method.IsPublic;
    public bool IsPrivate => _method.IsPrivate;
    public bool IsFamily => _method.IsFamily;
    public bool IsStatic => _method.IsStatic;
    public bool ContainsGenericParameters => _method.ContainsGenericParameters;
    public bool IsGenericMethod => _method.IsGenericMethod;
    public bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition;

    public IReadOnlyList<XamlXType> Parameters => _method.GetParameters()
        .Select(p => _typeSystem.GetOrCreateType(p.ParameterType))
        .ToList();

    public IReadOnlyList<XamlXType> GenericParameters => _method.IsGenericMethodDefinition
        ? _method.GetGenericArguments().Select(t => _typeSystem.GetOrCreateType(t)).ToList()
        : Array.Empty<XamlXType>();

    public IReadOnlyList<XamlXType> GenericArguments => _method.IsGenericMethod
        ? _method.GetGenericArguments().Select(t => _typeSystem.GetOrCreateType(t)).ToList()
        : Array.Empty<XamlXType>();

    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes =>
        _method.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();

    public XamlXMethod MakeGenericMethod(IReadOnlyList<XamlXType> typeArguments)
    {
        var clrTypeArguments = typeArguments
            .OfType<WpfTypeWrapper>()
            .Select(t => t.UnderlyingType)
            .ToArray();

        var genericMethod = _method.MakeGenericMethod(clrTypeArguments);
        return new WpfMethodWrapper(genericMethod, _declaringType, _typeSystem);
    }

    public XamlXParameterInfo GetParameterInfo(int index)
    {
        var parameters = _method.GetParameters();
        if (index < 0 || index >= parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new WpfParameterInfo(parameters[index], _typeSystem);
    }

    public bool Equals(XamlXMethod? other)
    {
        if (other is WpfMethodWrapper wrapper)
        {
            return _method == wrapper._method;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlMethod method)
        {
            return Equals(method);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _method.GetHashCode();
    }

    public MethodInfo UnderlyingMethod => _method;
}

/// <summary>
/// Wraps a CLR ParameterInfo as an IXamlParameterInfo.
/// </summary>
internal sealed class WpfParameterInfo : XamlXParameterInfo
{
    private readonly ParameterInfo _parameter;
    private readonly WpfTypeSystemProvider _typeSystem;

    public WpfParameterInfo(ParameterInfo parameter, WpfTypeSystemProvider typeSystem)
    {
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    public XamlXType ParameterType => _typeSystem.GetOrCreateType(_parameter.ParameterType);

    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes =>
        _parameter.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();
}

/// <summary>
/// Wraps a CLR FieldInfo as an IXamlField.
/// </summary>
internal sealed class WpfFieldWrapper : XamlXField
{
    private readonly FieldInfo _field;
    private readonly WpfTypeWrapper _declaringType;
    private readonly WpfTypeSystemProvider _typeSystem;

    public WpfFieldWrapper(
        FieldInfo field,
        WpfTypeWrapper declaringType,
        WpfTypeSystemProvider typeSystem)
    {
        _field = field ?? throw new ArgumentNullException(nameof(field));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    public string Name => _field.Name;
    public XamlXType FieldType => _typeSystem.GetOrCreateType(_field.FieldType);
    public XamlXType DeclaringType => _declaringType;
    public bool IsPublic => _field.IsPublic;
    public bool IsStatic => _field.IsStatic;
    public bool IsLiteral => _field.IsLiteral;

    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes =>
        _field.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();

    public object? GetLiteralValue()
    {
        if (!IsLiteral)
        {
            throw new InvalidOperationException($"Field {Name} is not a literal");
        }

        return _field.GetValue(null);
    }

    public bool Equals(XamlXField? other)
    {
        if (other is WpfFieldWrapper wrapper)
        {
            return _field == wrapper._field;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlField field)
        {
            return Equals(field);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _field.GetHashCode();
    }

    public FieldInfo UnderlyingField => _field;
}

/// <summary>
/// Wraps a CLR ConstructorInfo as an IXamlConstructor.
/// </summary>
internal sealed class WpfConstructorWrapper : XamlXConstructor
{
    private readonly ConstructorInfo _constructor;
    private readonly WpfTypeWrapper _declaringType;
    private readonly WpfTypeSystemProvider _typeSystem;

    public WpfConstructorWrapper(
        ConstructorInfo constructor,
        WpfTypeWrapper declaringType,
        WpfTypeSystemProvider typeSystem)
    {
        _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    public string Name => ".ctor";
    public XamlXType DeclaringType => _declaringType;
    public bool IsPublic => _constructor.IsPublic;
    public bool IsStatic => _constructor.IsStatic;

    public IReadOnlyList<XamlXType> Parameters => _constructor.GetParameters()
        .Select(p => _typeSystem.GetOrCreateType(p.ParameterType))
        .ToList();

    public IReadOnlyList<XamlXCustomAttribute> CustomAttributes =>
        _constructor.GetCustomAttributesData()
            .Select(a => new WpfCustomAttributeWrapper(a, _typeSystem))
            .Cast<XamlXCustomAttribute>()
            .ToList();

    public XamlXParameterInfo GetParameterInfo(int index)
    {
        var parameters = _constructor.GetParameters();
        if (index < 0 || index >= parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new WpfParameterInfo(parameters[index], _typeSystem);
    }

    public bool Equals(XamlXConstructor? other)
    {
        if (other is WpfConstructorWrapper wrapper)
        {
            return _constructor == wrapper._constructor;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlConstructor constructor)
        {
            return Equals(constructor);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _constructor.GetHashCode();
    }

    public ConstructorInfo UnderlyingConstructor => _constructor;
}

/// <summary>
/// Wraps a CLR EventInfo as an IXamlEventInfo.
/// </summary>
internal sealed class WpfEventInfoWrapper : XamlXEventInfo
{
    private readonly EventInfo _event;
    private readonly WpfTypeWrapper _declaringType;
    private readonly WpfTypeSystemProvider _typeSystem;

    public WpfEventInfoWrapper(
        EventInfo @event,
        WpfTypeWrapper declaringType,
        WpfTypeSystemProvider typeSystem)
    {
        _event = @event ?? throw new ArgumentNullException(nameof(@event));
        _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));
    }

    public string Name => _event.Name;
    public XamlXType DeclaringType => _declaringType;

    public XamlXMethod? Add => _event.AddMethod != null
        ? new WpfMethodWrapper(_event.AddMethod, _declaringType, _typeSystem)
        : null;

    public bool Equals(XamlXEventInfo? other)
    {
        if (other is WpfEventInfoWrapper wrapper)
        {
            return _event == wrapper._event;
        }
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is IXamlEventInfo eventInfo)
        {
            return Equals(eventInfo);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return _event.GetHashCode();
    }

    public EventInfo UnderlyingEvent => _event;
}
