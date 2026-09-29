using AdventureWorks.Abstractions;
using AdventureWorks.BlazorApp.Components;
using AdventureWorks.BlazorApp.Data;
using AdventureWorks.BlazorApp.Services;
using GitHub.Copilot;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<AdventureWorksDbContext>("AdventureWorks");
builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<ExternalComponentCompiler>();
builder.Services.AddSingleton<PageTestRunner>();
builder.Services.AddSingleton<CopilotClient>();
builder.Services.AddScoped<PageUpdateWorkspace>();
builder.Services.AddScoped<PageUpdateService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
