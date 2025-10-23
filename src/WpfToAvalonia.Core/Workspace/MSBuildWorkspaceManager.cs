using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace WpfToAvalonia.Core.Workspace;

/// <summary>
/// MSBuild-based implementation of the workspace manager.
/// </summary>
public sealed class MSBuildWorkspaceManager : IWorkspaceManager
{
    private MSBuildWorkspace? _workspace;
    private bool _disposed;
    private static bool _msbuildRegistered;
    private static readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MSBuildWorkspaceManager"/> class.
    /// </summary>
    public MSBuildWorkspaceManager()
    {
        EnsureMSBuildRegistered();
    }

    /// <inheritdoc />
    public Microsoft.CodeAnalysis.Workspace? CurrentWorkspace => _workspace;

    /// <inheritdoc />
    public async Task<Solution> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(solutionPath))
            throw new ArgumentNullException(nameof(solutionPath));

        if (!File.Exists(solutionPath))
            throw new FileNotFoundException($"Solution file not found: {solutionPath}");

        EnsureWorkspaceCreated();

        var solution = await _workspace!.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
        return solution;
    }

    /// <inheritdoc />
    public async Task<Microsoft.CodeAnalysis.Project> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(projectPath))
            throw new ArgumentNullException(nameof(projectPath));

        if (!File.Exists(projectPath))
            throw new FileNotFoundException($"Project file not found: {projectPath}");

        EnsureWorkspaceCreated();

        var project = await _workspace!.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
        return project;
    }

    /// <inheritdoc />
    public async Task<Compilation?> GetCompilationAsync(Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken = default)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        return await project.GetCompilationAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SemanticModel?> GetSemanticModelAsync(Document document, CancellationToken cancellationToken = default)
    {
        if (document == null)
            throw new ArgumentNullException(nameof(document));

        return await document.GetSemanticModelAsync(cancellationToken);
    }

    /// <inheritdoc />
    public bool TryApplyChanges(Solution solution)
    {
        if (solution == null)
            throw new ArgumentNullException(nameof(solution));

        EnsureWorkspaceCreated();

        return _workspace!.TryApplyChanges(solution);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _workspace?.Dispose();
        _workspace = null;
        _disposed = true;
    }

    private void EnsureWorkspaceCreated()
    {
        if (_workspace != null)
            return;

        _workspace = MSBuildWorkspace.Create();

        // Subscribe to diagnostic events for better error reporting
        _workspace.WorkspaceFailed += (sender, args) =>
        {
            // Log workspace failures if needed
            System.Diagnostics.Debug.WriteLine($"Workspace failed: {args.Diagnostic.Message}");
        };
    }

    private static void EnsureMSBuildRegistered()
    {
        lock (_lockObject)
        {
            if (_msbuildRegistered)
                return;

            try
            {
                // Register the default MSBuild instance
                if (!MSBuildLocator.IsRegistered)
                {
                    var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                    var instance = instances.OrderByDescending(x => x.Version).FirstOrDefault();

                    if (instance != null)
                    {
                        MSBuildLocator.RegisterInstance(instance);
                    }
                    else
                    {
                        // Fall back to registering defaults
                        MSBuildLocator.RegisterDefaults();
                    }
                }

                _msbuildRegistered = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to register MSBuild instance.", ex);
            }
        }
    }
}
