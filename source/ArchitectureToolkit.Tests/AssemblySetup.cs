namespace ArchitectureToolkit.Tests;

/// <summary>
/// Runs once before any test in this assembly. Sets ASPNETCORE_ENVIRONMENT to
/// "Testing" so that any WebApplicationFactory-based integration test (e.g.
/// CorrelationIdIntegrationTests) boots the real Program.cs with
/// IWebHostEnvironment.IsEnvironment("Testing") == true, which skips the
/// ADR-0015 auto-migration call. This must run before the first
/// WebApplicationFactory.CreateClient() call in the assembly, since that is
/// what triggers the lazy host build; a SetUpFixture at the assembly's root
/// namespace runs ahead of every fixture's tests, which satisfies that.
/// </summary>
[SetUpFixture]
public class AssemblySetup
{
    [OneTimeSetUp]
    public void SetTestingEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }
}
