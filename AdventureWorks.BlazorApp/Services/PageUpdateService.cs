using GitHub.Copilot;
using Microsoft.Extensions.AI;

namespace AdventureWorks.BlazorApp.Services;

public sealed class PageUpdateService(
    CopilotClient copilotClient,
    PageUpdateWorkspace workspace,
    ExternalComponentCompiler compiler,
    PageTestRunner testRunner)
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

        // Compile, then (automatically) test the page. Returns null on success, otherwise the failure report.
        async Task<string?> ValidatePageAsync(string who)
        {
            await ReportProgressAsync($"{who} is compiling the current page version.");
            try
            {
                await compiler.CompileAsync(pageName, candidatePath, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                await ReportProgressAsync("Compilation errors found.");
                return $"Compilation failed:{Environment.NewLine}{exception.Message}";
            }

            await ReportProgressAsync("Compilation succeeded. Running the tests.");
            var tests = await testRunner.RunAsync(pageName, candidatePath, cancellationToken);
            if (!tests.Passed)
            {
                await ReportProgressAsync("Tests failed.");
                return $"Compilation succeeded but the tests failed:{Environment.NewLine}{tests.Output}";
            }

            await ReportProgressAsync("Tests passed.");
            return null;
        }

        async Task<string> CompilePageAsync()
        {
            var failure = await ValidatePageAsync("Copilot");
            return failure is null
                ? "Compilation succeeded and the tests passed. The page is valid and ready to be reviewed."
                : $"{failure}{Environment.NewLine}Fix these problems and invoke compile_page again.";
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
                        Description = "Compile the current Razor page, then run the automated tests, and return actionable diagnostics.",
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

            You have a compile_page tool. Invoke it after every edit. It compiles the
            page and then runs the automated tests. If compilation or the tests fail,
            use the diagnostics to fix the page and invoke compile_page again.
            Continue this edit/compile/test loop until it succeeds.
            """;

        await ReportProgressAsync("Copilot is planning the requested page changes.");
        await session.SendAndWaitAsync(
            new MessageOptions { Prompt = prompt },
            TimeSpan.FromMinutes(5),
            cancellationToken);

        await ReportProgressAsync("Validating the updated page.");
        if (await ValidatePageAsync("The app") is { } failure)
        {
            throw new InvalidOperationException($"The updated page was not activated. {failure}");
        }

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
