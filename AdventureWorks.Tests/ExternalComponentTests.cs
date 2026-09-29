using System.Reflection;
using AdventureWorks.Abstractions;
using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Shouldly;
using AdventureWorks.BlazorApp.Components.Shared;
using AdventureWorks.BlazorApp.Services;

namespace AdventureWorks.Tests;

public sealed class ExternalComponentTests : IDisposable
{
    // Set by PageTestRunner: test the page Copilot just produced instead of the shipped ones.
    private const string CandidateDirectoryVariable = "ADVENTUREWORKS_CANDIDATE_PAGES_DIRECTORY";
    private const string CandidatePageVariable = "ADVENTUREWORKS_CANDIDATE_PAGE";

    private readonly string contentRoot;
    private readonly TestContext testContext = new();

    public ExternalComponentTests()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"AdventureWorks.ExternalComponents.{Guid.NewGuid():N}");
        contentRoot = Path.Combine(testRoot, "Web");
        Directory.CreateDirectory(Path.Combine(testRoot, "AdventureWorks.ExternalPages"));
        var pagesSource = Environment.GetEnvironmentVariable(CandidateDirectoryVariable) ?? FindExternalPagesSource();
        foreach (var sourceFile in Directory.GetFiles(pagesSource, "*.razor"))
        {
            File.Copy(
                sourceFile,
                Path.Combine(testRoot, "AdventureWorks.ExternalPages", Path.GetFileName(sourceFile)));
        }

        File.WriteAllText(
            Path.Combine(testRoot, "AdventureWorks.ExternalPages", "TestComponent.razor"),
            """
            @using Microsoft.AspNetCore.Components
            <p data-testid="external-component">Compiled external component</p>
            """);
        File.WriteAllText(
            Path.Combine(testRoot, "AdventureWorks.ExternalPages", "QuickGridComponent.razor"),
            """
            @using System.Linq
            @using Microsoft.AspNetCore.Components.QuickGrid
            <QuickGrid Items="items" Pagination="pagination" />
            <Paginator State="pagination" />
            @code {
                private IQueryable<string> items = Enumerable.Empty<string>().AsQueryable();
                private readonly PaginationState pagination = new();
            }
            """);

        testContext.Services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(contentRoot));
        testContext.Services.AddSingleton<ExternalComponentCompiler>();
        testContext.Services.AddSingleton<PageUpdateWorkspace>();
        testContext.Services.AddSingleton<ICustomerService, TestCustomerService>();
        testContext.Services.AddSingleton<IOrderService, TestOrderService>();
        testContext.JSInterop.SetupModule("./_content/Microsoft.AspNetCore.Components.QuickGrid/QuickGrid.razor.js")
            .Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ExternalComponent_CompilesLoadsAndRendersExternalRazor()
    {
        var rendered = testContext.RenderComponent<ExternalComponent>(parameters =>
            parameters.Add(component => component.PageName, "TestComponent.razor"));

        rendered.Find("[data-testid=external-component]").TextContent
            .ShouldBe("Compiled external component");
    }

    [Fact]
    public async Task ExternalComponentCompiler_ReusesCachedCompilation()
    {
        var compiler = testContext.Services.GetRequiredService<ExternalComponentCompiler>();

        var firstType = await compiler.CompileAsync("TestComponent.razor");
        var secondType = await compiler.CompileAsync("TestComponent.razor");

        secondType.ShouldBeSameAs(firstType);
    }

    [Fact]
    public async Task ExternalComponentCompiler_CompilesQuickGridComponents()
    {
        var compiler = testContext.Services.GetRequiredService<ExternalComponentCompiler>();

        var componentType = await compiler.CompileAsync("QuickGridComponent.razor");

        typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(componentType)
            .ShouldBeTrue();
    }

    [Fact]
    public void PageUpdateWorkspace_UsesWorkspaceRootForUserPages()
    {
        var workspace = testContext.Services.GetRequiredService<PageUpdateWorkspace>();
        var sourcePath = Path.Combine(
            Path.GetDirectoryName(FindExternalPagesSource())!,
            "CustomDashboardPage.razor");

        var candidatePath = workspace.PreparePage("CustomDashboardPage.razor", sourcePath);

        Path.GetFullPath(candidatePath)
            .ShouldStartWith(Path.Combine(Path.GetDirectoryName(contentRoot)!, "AdventureWorks.UserPages"));
    }

    [Theory]
    [InlineData("CustomDashboardPage.razor", "Recent orders")]
    [InlineData("CustomCustomersPage.razor", "Filter by company name")]
    [InlineData("CustomOrdersPage.razor", "All orders")]
    [InlineData("CustomCustomerDetailPage.razor", "Profile")]
    public void ExternalComponent_CompilesAndMountsApplicationPages(string pageName, string expectedText)
    {
        var rendered = testContext.RenderComponent<ExternalComponent>(parameters =>
        {
            parameters.Add(component => component.PageName, pageName);
            if (pageName == "CustomCustomerDetailPage.razor")
            {
                parameters.Add(
                    component => component.Parameters,
                    new Dictionary<string, object> { ["CustomerId"] = 1 });
            }
        });

        rendered.Markup.ShouldContain(expectedText);
    }

    public static IEnumerable<object[]> CandidatePages() =>
        (Environment.GetEnvironmentVariable(CandidatePageVariable) is { } candidate
            ? [candidate]
            : new[]
            {
                "CustomDashboardPage.razor",
                "CustomCustomersPage.razor",
                "CustomOrdersPage.razor",
                "CustomCustomerDetailPage.razor",
            })
        .Select(page => new object[] { page });

    // Post-compilation gate for Copilot page updates. Behavior-agnostic on purpose (the user may ask for
    // any change): the page must compile, mount and render with the test services without throwing.
    [Theory]
    [MemberData(nameof(CandidatePages))]
    public void CandidatePage_MountsAndRendersWithoutErrors(string pageName)
    {
        var rendered = testContext.RenderComponent<ExternalComponent>(parameters =>
        {
            parameters.Add(component => component.PageName, pageName);
            if (pageName == "CustomCustomerDetailPage.razor")
            {
                parameters.Add(
                    component => component.Parameters,
                    new Dictionary<string, object> { ["CustomerId"] = 1 });
            }
        });

        rendered.Markup.ShouldNotBeNullOrWhiteSpace();
    }

    public void Dispose()
    {
        testContext.Dispose();
        Directory.Delete(Directory.GetParent(contentRoot)!.FullName, recursive: true);
    }

    private sealed class TestWebHostEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name!;
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static string FindExternalPagesSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var externalPagesPath = Path.Combine(directory.FullName, "AdventureWorks.ExternalPages");
            if (Directory.Exists(externalPagesPath))
            {
                return externalPagesPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate AdventureWorks.ExternalPages.");
    }

    private sealed class TestCustomerService : ICustomerService
    {
        private static readonly CustomerProfile customer = new()
        {
            CustomerID = 1,
            CompanyName = "Test Company",
            FirstName = "Ada",
            LastName = "Lovelace",
            EmailAddress = "ada@example.com",
            Phone = "555-0100",
        };

        public IQueryable<CustomerProfile> GetCustomers(string? companyName = null) =>
            new[] { customer }
                .Where(item => string.IsNullOrWhiteSpace(companyName) ||
                               item.CompanyName!.Contains(companyName, StringComparison.OrdinalIgnoreCase))
                .AsQueryable();

        public Task<CustomerProfile?> GetCustomerAsync(
            int customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerProfile?>(customerId == customer.CustomerID ? customer : null);

        public Task<int> GetCustomerCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class TestOrderService : IOrderService
    {
        private static readonly IQueryable<OrderSummary> orders = new[]
        {
            new OrderSummary
            {
                SalesOrderID = 1001,
                OrderDate = new DateTime(2026, 1, 1),
                Status = 5,
                TotalDue = 125.50m,
                CustomerID = 1,
                CustomerName = "Test Company",
            },
        }.AsQueryable();

        public IQueryable<OrderSummary> GetOrders(int? customerId = null) =>
            customerId is null
                ? orders
                : orders.Where(order => order.CustomerID == customerId);

        public Task<int> GetOrderCountByCustomerAsync(
            int customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GetOrders(customerId).Count());

        public Task<decimal> GetTotalRevenueByCustomerAsync(
            int customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GetOrders(customerId).Sum(order => order.TotalDue));

        public Task<int> GetOrderCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(orders.Count());

        public Task<decimal> GetTotalRevenueAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(orders.Sum(order => order.TotalDue));
    }
}
