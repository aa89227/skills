// Microsoft Agent Framework 1.0.0-rc4 — Workflows
// Demonstrates: WorkflowBuilder, lambda executor, custom Executor class,
//   Executor<TIn,TOut>, [MessageHandler] source generator, InProcessExecution

using Microsoft.Agents.AI.Workflows;

// --- Simple Workflow (lambda executor + custom executor) ---

Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

var reverse = new ReverseTextExecutor();

// Build directed graph: uppercase → reverse
WorkflowBuilder builder = new(uppercase);               // entry executor
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
var workflow = builder.Build();

// Execute
await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent e)
        Console.WriteLine($"{e.ExecutorId}: {e.Data}");
}
// Output:
// UppercaseExecutor: HELLO, WORLD!
// ReverseTextExecutor: !DLROW ,OLLEH

// --- Custom Executor: [MessageHandler] source generator (AOT compatible) ---
// Requires: Microsoft.Agents.AI.Workflows.Generators package

internal sealed partial class ReverseTextExecutor() : Executor("ReverseTextExecutor")
{
    [MessageHandler]
    private ValueTask<string> HandleAsync(string message, IWorkflowContext context)
    {
        return ValueTask.FromResult(string.Concat(message.Reverse()));
    }
}

// --- Custom Executor: generic base class (explicit typing) ---

internal sealed class UppercaseExecutor() : Executor<string, string>("UppercaseExecutor")
{
    public override ValueTask<string> HandleAsync(
        string message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(message.ToUpperInvariant());
    }
}

// --- Azure Functions hosting ---

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

AIAgent hostedAgent = new AzureOpenAIClient(
        new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!),
        new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        instructions: "You are a helpful assistant hosted in Azure Functions.",
        name: "HostedAgent");

// Auto-generates HTTP endpoints: POST /api/agents/{name}/run
using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableAgents(options =>
        options.AddAIAgent(hostedAgent, timeToLive: TimeSpan.FromHours(1)))
    .Build();

app.Run();
