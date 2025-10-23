namespace WpfToAvalonia.XamlParser.TypeSystem;

/// <summary>
/// Abstraction for XAML type system.
/// This interface is compatible with XamlX.TypeSystem.IXamlType but can also work
/// with other type systems (Roslyn, System.Reflection, etc.).
/// </summary>
public interface IXamlType
{
    /// <summary>
    /// Gets the unique identifier for this type.
    /// </summary>
    object Id { get; }

    /// <summary>
    /// Gets the simple name of the type.
    /// Example: "Button" for System.Windows.Controls.Button
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the namespace of the type.
    /// Example: "System.Windows.Controls" for Button
    /// </summary>
    string? Namespace { get; }

    /// <summary>
    /// Gets the full name of the type.
    /// Example: "System.Windows.Controls.Button"
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Gets a value indicating whether this type is public.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets the assembly containing this type.
    /// </summary>
    IXamlAssembly? Assembly { get; }

    /// <summary>
    /// Gets the properties of this type.
    /// </summary>
    IReadOnlyList<IXamlProperty> Properties { get; }

    /// <summary>
    /// Gets the base type of this type.
    /// </summary>
    IXamlType? BaseType { get; }

    /// <summary>
    /// Gets a value indicating whether this is a value type.
    /// </summary>
    bool IsValueType { get; }

    /// <summary>
    /// Gets a value indicating whether this is an enum.
    /// </summary>
    bool IsEnum { get; }

    /// <summary>
    /// Gets the interfaces implemented by this type.
    /// </summary>
    IReadOnlyList<IXamlType> Interfaces { get; }

    /// <summary>
    /// Determines if this type is assignable from another type.
    /// </summary>
    bool IsAssignableFrom(IXamlType type);
}

/// <summary>
/// Represents a property in the XAML type system.
/// </summary>
public interface IXamlProperty
{
    /// <summary>
    /// Gets the name of the property.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    IXamlType PropertyType { get; }

    /// <summary>
    /// Gets the type that declares this property.
    /// </summary>
    IXamlType? DeclaringType { get; }

    /// <summary>
    /// Gets a value indicating whether this is an attached property.
    /// </summary>
    bool IsAttached { get; }

    /// <summary>
    /// Gets a value indicating whether this property has a public getter.
    /// </summary>
    bool CanRead { get; }

    /// <summary>
    /// Gets a value indicating whether this property has a public setter.
    /// </summary>
    bool CanWrite { get; }
}

/// <summary>
/// Represents an assembly in the XAML type system.
/// </summary>
public interface IXamlAssembly
{
    /// <summary>
    /// Gets the name of the assembly.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Finds a type by its full name.
    /// </summary>
    IXamlType? FindType(string fullName);
}

/// <summary>
/// Represents a method in the XAML type system.
/// </summary>
public interface IXamlMethod
{
    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the return type of the method.
    /// </summary>
    IXamlType ReturnType { get; }

    /// <summary>
    /// Gets the parameter types of the method.
    /// </summary>
    IReadOnlyList<IXamlType> Parameters { get; }

    /// <summary>
    /// Gets a value indicating whether this method is public.
    /// </summary>
    bool IsPublic { get; }

    /// <summary>
    /// Gets a value indicating whether this method is static.
    /// </summary>
    bool IsStatic { get; }
}

/// <summary>
/// Provides type resolution services for XAML parsing.
/// </summary>
public interface IXamlTypeResolver
{
    /// <summary>
    /// Resolves a type by its XML namespace and local name.
    /// </summary>
    IXamlType? ResolveType(string xmlNamespace, string typeName);

    /// <summary>
    /// Resolves a type by its full .NET name.
    /// </summary>
    IXamlType? ResolveType(string fullTypeName);

    /// <summary>
    /// Gets a type from a CLR type object (System.Type or Roslyn INamedTypeSymbol).
    /// </summary>
    IXamlType? GetType(object clrType);
}
