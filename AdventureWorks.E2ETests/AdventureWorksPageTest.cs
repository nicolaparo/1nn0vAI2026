using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace AdventureWorks.E2ETests;

/// <summary>
/// Base class for the browser tests. The application URL comes from the E2E_BASE_URL environment
/// variable so the same tests can run against a locally started app or, from inside the Playwright
/// container, against the app running on the Docker host.
/// </summary>
public abstract class AdventureWorksPageTest : PageTest
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5080";

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = BaseUrl,
        IgnoreHTTPSErrors = true,
    };
}
