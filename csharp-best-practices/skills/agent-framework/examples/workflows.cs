// Microsoft Agent Framework 1.17.0 — Workflows
// Demonstrates: WorkflowBuilder, lambda executor, custom Executor class,
//   Executor<TIn,TOut>, [MessageHandler] source generator, InProcessExecution,
//   current orchestration/hosting boundary

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

// --- Magentic orchestration (specialized, version-sensitive API) ---
// Cap rounds/stalls/resets and tool permissions for production workloads.
// var magentic = new MagenticWorkflowBuilder(managerAgent)
//     .AddParticipants(researchAgent, writerAgent)
//     .WithName("ResearchAndWrite")
//     .WithDescription("Research, then produce a bounded report.")
//     .RequirePlanSignoff()
//     .WithMaxRounds(10)
//     .WithMaxStalls(3)
//     .WithMaxResets(2)
//     .Build();

// Durable Task/Azure Functions hosting is no longer in this core source tree. Use the maintained
// extension repository: https://github.com/microsoft/agent-framework-durable-extension
