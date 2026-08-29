using GitHub.Copilot;
using Microsoft.Extensions.AI;

namespace AdventureWorks.BlazorApp.Services;

public sealed class PageUpdateService(
    CopilotClient copilotClient,
    PageUpdateWorkspace workspace,
    ExternalComponentCompiler compiler)
{
    public event Func<string, Task>? ProgressChanged;

    public async Task UpdatePageAsync(
        string pageName,
        string request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);

        await ReportProgressAsync("Preparing a private copy of this page.");
        var sourcePath = compiler.GetPagePath(pageName);
        var candidatePath = workspace.PreparePage(pageName, sourcePath);
        await ReportProgressAsync("Starting Copilot.");
        await copilotClient.StartAsync(cancellationToken);

        async Task<string> ReportCopilotProgressAsync(string update)
        {
            await ReportProgressAsync(update);
            return "Progress update sent to the user.";
        }

        async Task<string> CompilePageAsync()
        {
            await ReportProgressAsync("Copilot is compiling the current page version.");
            try
            {
                await compiler.CompileAsync(pageName, candidatePath, cancellationToken);
                await ReportProgressAsync("Copilot compilation succeeded.");
                return "Compilation succeeded. The page is valid and ready to be reviewed.";
            }
            catch (InvalidOperationException exception)
            {
                await ReportProgressAsync("Copilot found compilation errors and is revising the page.");
                return $"Compilation failed. Fix these errors and invoke compile_page again:{Environment.NewLine}{exception.Message}";
            }
        }

        await using var session = await copilotClient.CreateSessionAsync(new SessionConfig
        {
            Model = "gpt-5.6-luna",
            WorkingDirectory = Path.GetDirectoryName(candidatePath),
            OnPermissionRequest = PermissionHandler.ApproveAll,
            Tools =
            [
                CopilotTool.DefineTool(
                    ReportCopilotProgressAsync,
                    factoryOptions: new AIFunctionFactoryOptions
                    {
                        Name = "report_progress",
                        Description = "Send a brief progress update to the user after each meaningful operation.",
                    }),
                CopilotTool.DefineTool(
                    CompilePageAsync,
                    factoryOptions: new AIFunctionFactoryOptions
                    {
                        Name = "compile_page",
                        Description = "Compile the current Razor page and return actionable diagnostics.",
                    }),
            ],
        }, cancellationToken);

        var prompt = $"""
            You are editing the Blazor page in {Path.GetFileName(candidatePath)}.
            Implement this user request: <UserRequest>{request}</UserRequest>

            Work only in the current workspace and edit the target Razor page.
            Preserve the existing page's default functionality, service contracts,
            routing assumptions, and accessibility. Do not edit files outside the
            workspace. Review your changes when finished.

            You have a report_progress tool. Call it with a short, user-friendly
            update before and after each meaningful operation so the user can see
            what is happening.

            You have a compile_page tool. Invoke it after every edit. If compilation
            fails, use the diagnostics to fix the page and invoke compile_page again.
            Continue this edit/compile loop until compilation succeeds.
            """;

        await ReportProgressAsync("Copilot is planning the requested page changes.");
        await session.SendAndWaitAsync(
            new MessageOptions { Prompt = prompt },
            TimeSpan.FromMinutes(5),
            cancellationToken);

        await ReportProgressAsync("Validating the updated page.");
        await compiler.CompileAsync(pageName, candidatePath, cancellationToken);
        await ReportProgressAsync("Loading the validated page.");
        await workspace.ActivatePageAsync(pageName, candidatePath);
        await ReportProgressAsync("Page update complete.");
    }

    private async Task ReportProgressAsync(string message)
    {
        if (ProgressChanged is not null)
        {
            await ProgressChanged.Invoke(message).ConfigureAwait(false);
        }
    }
}
