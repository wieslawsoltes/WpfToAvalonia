using Microsoft.CodeAnalysis;

namespace WpfToAvalonia.Core.Workspace;

/// <summary>
/// Manages loading and working with MSBuild workspaces.
/// </summary>
public interface IWorkspaceManager : IDisposable
{
    /// <summary>
    /// Gets the current workspace.
    /// </summary>
    Microsoft.CodeAnalysis.Workspace? CurrentWorkspace { get; }

    /// <summary>
    /// Loads a solution from a .sln file.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded solution.</returns>
    Task<Solution> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a project from a .csproj file.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded project.</returns>
    Task<Microsoft.CodeAnalysis.Project> LoadProjectAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the compilation for a project.
    /// </summary>
    /// <param name="project">The project to compile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compilation.</returns>
    Task<Compilation?> GetCompilationAsync(Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the semantic model for a document.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The semantic model.</returns>
    Task<SemanticModel?> GetSemanticModelAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies changes to the workspace.
    /// </summary>
    /// <param name="solution">The new solution with changes.</param>
    /// <returns>True if changes were applied successfully, otherwise false.</returns>
    bool TryApplyChanges(Solution solution);
}
