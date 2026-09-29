using GitHub.Copilot;
using GitHub.Copilot.Rpc;

// Scenario 3: point the SDK at a folder and Copilot picks up that folder's
// .github/copilot-instructions.md by itself. Nothing about "pirates" is in this code:
// open Workspace/.github/copilot-instructions.md to see where the behavior comes from.

var workspace = FindWorkspace();

await using var client = new CopilotClient();
await client.StartAsync();

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5.6-luna",
    WorkingDirectory = workspace,

    // Custom instruction files (.github/copilot-instructions.md, AGENTS.md) are loaded from
    // WorkingDirectory. Explicit false = "do load them" (null lets the client mode decide).
    SkipCustomInstructions = false,

    // Read-only demo: allow reads, refuse everything else.
    OnPermissionRequest = (request, _) => Task.FromResult(request switch
    {
        PermissionRequestRead => PermissionDecision.ApproveOnce(),
        _ => PermissionDecision.Reject("Read-only demo"),
    }),
});

session.On<ToolExecutionStartEvent>(start =>
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  [event] tool started: {start.Data.ToolName}");
    Console.ResetColor();
});

Console.WriteLine($"Working directory: {workspace}\n");

const string prompt = "Look at the files in the current directory and tell me who is in the crew and what the treasure map says.";
Console.WriteLine($"> {prompt}\n");

var reply = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, TimeSpan.FromMinutes(3));
Console.WriteLine($"\nCopilot: {reply?.Data.Content}");

static string FindWorkspace()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, "Workspace");
        if (Directory.Exists(candidate)) return candidate;
    }
    throw new DirectoryNotFoundException("Workspace/ folder not found above " + AppContext.BaseDirectory);
}
