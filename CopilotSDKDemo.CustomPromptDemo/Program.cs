using GitHub.Copilot;
using GitHub.Copilot.Rpc;

// Scenario 4: same Workspace/ folder, but the behavior now comes from a prompt defined
// IN THIS APP (SystemMessage) instead of from the folder's copilot-instructions.md.
//
//   dotnet run                    -> cat prompt only (the folder's pirate file is ignored)
//   dotnet run -- --with-folder   -> also load the folder's pirate instructions (watch them clash)

var loadFolderInstructions = args.Contains("--with-folder");
var workspace = FindWorkspace();

const string catPrompt = """
    You are a cat. In every reply meow and purr: sprinkle "Meow!", "Mrrrow", "Purrr..." and "*purrs*" throughout,
    occasionally mention napping in a sunbeam or knocking things off tables. Stay helpful and accurate:
    the facts must be right, only the tone is feline.
    """;

await using var client = new CopilotClient();
await client.StartAsync();

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5.6-luna",
    WorkingDirectory = workspace,

    // Our own prompt, added on top of Copilot's default system message.
    SystemMessage = new SystemMessageConfig { Mode = SystemMessageMode.Append, Content = catPrompt },

    // true (default here): ignore Workspace/.github/copilot-instructions.md so only the cat prompt applies.
    SkipCustomInstructions = !loadFolderInstructions,

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

Console.WriteLine($"Working directory: {workspace}");
Console.WriteLine($"Folder instructions: {(loadFolderInstructions ? "LOADED" : "skipped")}\n");

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
