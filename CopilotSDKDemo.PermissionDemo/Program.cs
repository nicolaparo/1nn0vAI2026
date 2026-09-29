using System.ComponentModel;
using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using Microsoft.Extensions.AI;

// Scenario 1: intercept a DEFAULT Copilot tool with our own C# handler.
// Three interception points, from "earliest" to "replace the tool entirely".

var workspace = Path.Combine(Path.GetTempPath(), "CopilotSDKDemo.PermissionDemo");
Directory.CreateDirectory(workspace);
File.WriteAllText(Path.Combine(workspace, "notes.txt"), "Secret launch date: 2026-12-01");

await using var client = new CopilotClient();
await client.StartAsync();

await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5.6-luna",
    WorkingDirectory = workspace,

    // 1) Custom tool with the SAME NAME as a built-in ("view") replaces the built-in.
    Tools = [CopilotTool.DefineTool(
        ViewFileHandler,
        toolOptions: new CopilotToolOptions { OverridesBuiltInTool = true, SkipPermission = true },
        factoryOptions: new AIFunctionFactoryOptions
        {
            Name = "view",
            Description = "Reads a file or lists a directory.",
        })],

    // 2) Runs before ANY tool (built-in or custom): can allow, deny or rewrite the arguments.
    Hooks = new SessionHooks { OnPreToolUse = OnPreToolUse },

    // 3) Asked for every permission-gated operation (shell, write, read, url...).
    OnPermissionRequest = OnPermissionRequest,
});

session.On<ToolExecutionStartEvent>(start =>
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"  [event] tool started: {start.Data.ToolName}");
    Console.ResetColor();
});

const string prompt = "Read the file notes.txt in the current directory and tell me what it says.";
Console.WriteLine($"> {prompt}\n");

var reply = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, TimeSpan.FromMinutes(3));
Console.WriteLine($"\nCopilot: {reply?.Data.Content}");

// ---------------------------------------------------------------------------

static string ViewFileHandler([Description("Path of the file or directory")] string path)
{
    // >>> BREAKPOINT #3: the built-in "view" never runs, this code does.
    Log("3 custom 'view' handler", path);
    return "[intercepted by ViewFileHandler] The file is empty.";
}

static Task<PreToolUseHookOutput?> OnPreToolUse(PreToolUseHookInput input, HookInvocation invocation)
{
    // >>> BREAKPOINT #1: sees every tool call before it executes.
    Log("1 OnPreToolUse", $"{input.ToolName} {input.ToolArgs}");
    return Task.FromResult<PreToolUseHookOutput?>(new PreToolUseHookOutput { PermissionDecision = "allow" });
}

static Task<PermissionDecision> OnPermissionRequest(PermissionRequest request, PermissionInvocation invocation)
{
    // >>> BREAKPOINT #2: decide per request kind. Here: no shell, everything else once.
    Log("2 OnPermissionRequest", request.Kind.ToString());
    PermissionDecision decision = request switch
    {
        PermissionRequestShell shell => PermissionDecision.Reject($"Shell not allowed: {shell.FullCommandText}"),
        _ => PermissionDecision.ApproveOnce(),
    };
    return Task.FromResult(decision);
}

static void Log(string where, string what)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"  [{where}] {what}");
    Console.ResetColor();
}
