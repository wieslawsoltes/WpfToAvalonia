using System.Reflection;
using XamlX.TypeSystem;
using WpfToAvalonia.Core.Diagnostics;

// Alias to disambiguate between XamlX types and our internal types
using XamlXType = XamlX.TypeSystem.IXamlType;
using XamlXAssembly = XamlX.TypeSystem.IXamlAssembly;

namespace WpfToAvalonia.XamlParser.TypeSystem;

/// <summary>
/// Provides a bridge between WPF's type system and XamlX's IXamlTypeSystem.
/// This allows XamlX to parse WPF XAML by understanding WPF's type model.
/// </summary>
public sealed class WpfTypeSystemProvider : IXamlTypeSystem
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly Dictionary<Assembly, WpfAssemblyWrapper> _assemblyCache = new();
    private readonly Dictionary<Type, WpfTypeWrapper> _typeCache = new();

    // WPF core assemblies
    private const string PresentationFramework = "PresentationFramework";
    private const string PresentationCore = "PresentationCore";
    private const string WindowsBase = "WindowsBase";

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfTypeSystemProvider"/> class.
    /// </summary>
    public WpfTypeSystemProvider(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        _diagnostics.AddInfo(
            "WPF_TYPE_SYSTEM_INIT",
            "Initializing WPF type system provider",
            null);
    }

    /// <summary>
    /// Gets all loaded assemblies in the type system.
    /// </summary>
    public IEnumerable<XamlXAssembly> Assemblies =>
        _assemblyCache.Values.Cast<XamlXAssembly>();

    /// <summary>
    /// Finds a type by its full name across all assemblies.
    /// </summary>
    public XamlXType? FindType(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return null;
        }

        // Try to find in already loaded assemblies
        foreach (var assembly in _assemblyCache.Values)
        {
            var type = assembly.FindType(fullName);
            if (type != null)
            {
                return type;
            }
        }

        // Try to load the type using reflection
        var clrType = Type.GetType(fullName);
        if (clrType != null)
        {
            return GetOrCreateType(clrType);
        }

        _diagnostics.AddWarning(
            "WPF_TYPE_NOT_FOUND",
            $"Type '{fullName}' not found in WPF type system",
            null);

        return null;
    }

    /// <summary>
    /// Finds a type by its full name in a specific assembly.
    /// </summary>
    public XamlXType? FindType(string fullName, string assemblyName)
    {
        var assembly = GetOrLoadAssembly(assemblyName);
        if (assembly == null)
        {
            return null;
        }

        return assembly.FindType(fullName);
    }

    /// <summary>
    /// Finds an assembly by a substring of its name.
    /// </summary>
    public XamlXAssembly? FindAssembly(string substring)
    {
        if (string.IsNullOrEmpty(substring))
        {
            return null;
        }

        // Check loaded assemblies first
        var assembly = _assemblyCache.Values.FirstOrDefault(a => a.Name.Contains(substring, StringComparison.OrdinalIgnoreCase));
        if (assembly != null)
        {
            return assembly;
        }

        // Try to load by full name
        return GetOrLoadAssembly(substring);
    }

    /// <summary>
    /// Gets an assembly by name, loading it if necessary.
    /// </summary>
    public XamlXAssembly? GetAssembly(string name)
    {
        return GetOrLoadAssembly(name);
    }

    /// <summary>
    /// Loads a WPF assembly by name.
    /// </summary>
    private WpfAssemblyWrapper? GetOrLoadAssembly(string assemblyName)
    {
        // Check cache first
        var cachedAssembly = _assemblyCache.Values.FirstOrDefault(a => a.Name == assemblyName);
        if (cachedAssembly != null)
        {
            return cachedAssembly;
        }

        try
        {
            // Try to load the assembly
            Assembly? assembly = null;

            // Special handling for WPF assemblies
            if (IsWpfAssembly(assemblyName))
            {
                assembly = Assembly.Load(assemblyName);
            }
            else
            {
                // Try standard assembly loading
                assembly = Assembly.Load(assemblyName);
            }

            if (assembly != null)
            {
                return GetOrCreateAssembly(assembly);
            }
        }
        catch (Exception ex)
        {
            _diagnostics.AddWarning(
                "WPF_ASSEMBLY_LOAD_FAILED",
                $"Failed to load assembly '{assemblyName}': {ex.Message}",
                null);
        }

        return null;
    }

    /// <summary>
    /// Gets or creates a wrapper for a CLR assembly.
    /// </summary>
    private WpfAssemblyWrapper GetOrCreateAssembly(Assembly assembly)
    {
        if (_assemblyCache.TryGetValue(assembly, out var cached))
        {
            return cached;
        }

        var wrapper = new WpfAssemblyWrapper(assembly, this, _diagnostics);
        _assemblyCache[assembly] = wrapper;
        return wrapper;
    }

    /// <summary>
    /// Gets or creates a wrapper for a CLR type.
    /// </summary>
    internal WpfTypeWrapper GetOrCreateType(Type type)
    {
        if (_typeCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var assembly = GetOrCreateAssembly(type.Assembly);
        var wrapper = new WpfTypeWrapper(type, assembly, this, _diagnostics);
        _typeCache[type] = wrapper;
        return wrapper;
    }

    /// <summary>
    /// Determines if an assembly name refers to a WPF assembly.
    /// </summary>
    private bool IsWpfAssembly(string assemblyName)
    {
        return assemblyName == PresentationFramework ||
               assemblyName == PresentationCore ||
               assemblyName == WindowsBase ||
               assemblyName.StartsWith("PresentationFramework,") ||
               assemblyName.StartsWith("PresentationCore,") ||
               assemblyName.StartsWith("WindowsBase,");
    }

    /// <summary>
    /// Preloads common WPF assemblies into the cache.
    /// </summary>
    public void PreloadWpfAssemblies()
    {
        _diagnostics.AddInfo(
            "WPF_PRELOAD_ASSEMBLIES",
            "Preloading WPF assemblies",
            null);

        var wpfAssemblies = new[]
        {
            PresentationFramework,
            PresentationCore,
            WindowsBase
        };

        foreach (var assemblyName in wpfAssemblies)
        {
            try
            {
                GetOrLoadAssembly(assemblyName);
            }
            catch (Exception ex)
            {
                _diagnostics.AddWarning(
                    "WPF_PRELOAD_FAILED",
                    $"Failed to preload assembly '{assemblyName}': {ex.Message}",
                    null);
            }
        }
    }

    /// <summary>
    /// Gets statistics about the cached type system.
    /// </summary>
    public TypeSystemStatistics GetStatistics()
    {
        return new TypeSystemStatistics
        {
            AssembliesLoaded = _assemblyCache.Count,
            TypesCached = _typeCache.Count,
            WpfAssembliesLoaded = _assemblyCache.Values.Count(a => IsWpfAssembly(a.Name))
        };
    }
}

/// <summary>
/// Statistics about the WPF type system provider.
/// </summary>
public sealed class TypeSystemStatistics
{
    public int AssembliesLoaded { get; init; }
    public int TypesCached { get; init; }
    public int WpfAssembliesLoaded { get; init; }
}
