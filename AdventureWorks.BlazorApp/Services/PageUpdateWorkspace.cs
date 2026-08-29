using System.Collections.Concurrent;

namespace AdventureWorks.BlazorApp.Services;

public sealed class PageUpdateWorkspace(IWebHostEnvironment environment)
{
    private const string ExternalPagesDirectory = "AdventureWorks.ExternalPages";
    private readonly string workspacePath = Path.Combine(
        Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath,
        "AdventureWorks.UserPages",
        Guid.NewGuid().ToString("N"));
    private readonly ConcurrentDictionary<string, string> activePages =
        new(StringComparer.OrdinalIgnoreCase);

    public event Func<Task>? Changed;

    public string PreparePage(string pageName, string sourcePath)
    {
        ValidatePageName(pageName);
        Directory.CreateDirectory(workspacePath);

        foreach (var sourceFile in Directory.GetFiles(Path.GetDirectoryName(sourcePath)!, "*.razor"))
        {
            var destinationPath = Path.Combine(workspacePath, Path.GetFileName(sourceFile));
            if (!File.Exists(destinationPath))
            {
                File.Copy(sourceFile, destinationPath);
            }
        }

        return Path.Combine(workspacePath, pageName);
    }

    public string? GetActivePagePath(string pageName) =>
        activePages.TryGetValue(pageName, out var path) ? path : null;

    public async Task ActivatePageAsync(string pageName, string pagePath)
    {
        ValidatePageName(pageName);
        if (!File.Exists(pagePath) ||
            !Path.GetFullPath(pagePath).StartsWith(
                Path.GetFullPath(workspacePath) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The page must be an existing file in the user workspace.", nameof(pagePath));
        }

        activePages[pageName] = pagePath;
        if (Changed is not null)
        {
            await Changed.Invoke().ConfigureAwait(false);
        }
    }

    public static string? ResolvePageName(string uri)
    {
        var path = new Uri(uri).AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(path))
        {
            return "CustomDashboardPage.razor";
        }

        if (path.Equals("customers", StringComparison.OrdinalIgnoreCase))
        {
            return "CustomCustomersPage.razor";
        }

        if (path.StartsWith("customers/", StringComparison.OrdinalIgnoreCase))
        {
            return "CustomCustomerDetailPage.razor";
        }

        if (path.Equals("orders", StringComparison.OrdinalIgnoreCase))
        {
            return "CustomOrdersPage.razor";
        }

        return null;
    }

    private static void ValidatePageName(string pageName)
    {
        if (!pageName.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
            pageName.Contains(Path.DirectorySeparatorChar) ||
            pageName.Contains(Path.AltDirectorySeparatorChar) ||
            pageName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Page names must be a file name ending in .razor.", nameof(pageName));
        }
    }
}
