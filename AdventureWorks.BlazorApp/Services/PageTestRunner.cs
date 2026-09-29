using System.Diagnostics;
using System.Text;

namespace AdventureWorks.BlazorApp.Services;

public sealed record PageTestResult(bool Passed, string Output);

/// <summary>
/// Runs the AdventureWorks.Tests candidate-page tests (through <c>dotnet test</c>) against a page
/// produced by Copilot, after it has compiled.
/// </summary>
public sealed class PageTestRunner(IWebHostEnvironment environment)
{
    // Keep the tests' bin/obj away from the running app's own output, which is locked while it runs.
    private const string ArtifactsDirectory = ".test-artifacts";
    private const int MaxOutputLength = 6000;
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    public async Task<PageTestResult> RunAsync(string pageName, string candidatePath, CancellationToken cancellationToken = default)
    {
        var repositoryRoot = Directory.GetParent(environment.ContentRootPath)?.FullName ?? environment.ContentRootPath;
        var testProject = Path.Combine(repositoryRoot, "AdventureWorks.Tests", "AdventureWorks.Tests.csproj");
        if (!File.Exists(testProject))
        {
            return new PageTestResult(false, $"Test project not found: {testProject}");
        }

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = repositoryRoot,
        };
        foreach (var argument in new[]
                 {
                     "test", testProject,
                     "--filter", "FullyQualifiedName~CandidatePage_",
                     "--artifacts-path", Path.Combine(repositoryRoot, "AdventureWorks.UserPages", ArtifactsDirectory),
                     "--nologo", "--verbosity", "minimal",
                 })
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Read by AdventureWorks.Tests: test this candidate page instead of the shipped ones.
        startInfo.Environment["ADVENTUREWORKS_CANDIDATE_PAGES_DIRECTORY"] = Path.GetDirectoryName(candidatePath)!;
        startInfo.Environment["ADVENTUREWORKS_CANDIDATE_PAGE"] = pageName;

        var output = new StringBuilder();
        var sync = new object();
        void Append(string? line)
        {
            if (line is not null)
            {
                lock (sync) { output.AppendLine(line); }
            }
        }

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => Append(e.Data);
        process.ErrorDataReceived += (_, e) => Append(e.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            process.WaitForExit(); // flush the redirected output handlers
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            return new PageTestResult(false, $"Tests timed out after {Timeout.TotalMinutes:0} minutes.");
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        string text;
        lock (sync) { text = output.ToString(); }
        if (text.Length > MaxOutputLength)
        {
            // The failure message is at the top of each failed test's block; the tail is only stack frames.
            text = text[..MaxOutputLength] + Environment.NewLine + "... (output truncated)";
        }

        return new PageTestResult(process.ExitCode == 0, text);
    }
}
