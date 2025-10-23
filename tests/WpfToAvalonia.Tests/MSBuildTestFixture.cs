using WpfToAvalonia.Core.Project;

namespace WpfToAvalonia.Tests;

/// <summary>
/// Test fixture that ensures MSBuild is registered before running tests.
/// </summary>
public class MSBuildTestFixture
{
    public MSBuildTestFixture()
    {
        // Ensure MSBuild is registered for all tests
        ProjectFileParser.EnsureMSBuildRegistered();
    }
}
