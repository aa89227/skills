---
name: agent-framework
description: |
  Use when building AI agents with Microsoft Agent Framework (.NET): creating agents, adding tools,
  multi-turn conversations, memory/context providers, middleware, MCP integration, workflows,
  and hosting (A2A, AG-UI). Trigger phrases: "agent framework", "AIAgent", "Microsoft.Agents.AI",
  "ChatClientAgent", "agent workflow", "agent tool", "MCP agent", "A2A agent".
license: MIT
metadata:
  author: aa89227
  version: "1.1"
  agent-framework-version: "1.0.0-rc4"
  tags: ["csharp", "dotnet", "agent-framework", "ai-agent", "workflow", "mcp", "a2a"]
  trigger_keywords: ["agent-framework", "AIAgent", "Microsoft.Agents", "ChatClientAgent", "workflow", "MCP", "A2A"]
---

## Auto-Trigger Scenarios

This skill activates when:
- User builds AI agents with Microsoft Agent Framework in .NET
- User asks about `AIAgent`, `ChatClientAgent`, or agent providers
- Code references `Microsoft.Agents.AI` namespaces
- User needs agent tools, MCP integration, workflows, or middleware patterns

# Microsoft Agent Framework (.NET) — 1.0.0-rc4

> **Version:** This skill is verified against `dotnet-1.0.0-rc4` tag of `microsoft/agent-framework`.

## Quick Reference

**NuGet:** `Microsoft.Agents.AI` (core), `Microsoft.Agents.AI.OpenAI` (Azure OpenAI/OpenAI provider)
**Namespace:** `Microsoft.Agents.AI`
**Base class:** `AIAgent` — all agents derive from this
**Requires:** .NET 10+ (recommended), .NET 8+ (supported)
**Repo:** `github.com/microsoft/agent-framework`
**Docs:** `learn.microsoft.com/en-us/agent-framework/`

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│                  AIAgent (base)                  │
├──────────┬──────────┬───────────┬───────────────┤
│ Providers│  Tools   │  Memory   │  Middleware    │
│ AzureOAI │ Function │ Context   │ Agent Run     │
│ OpenAI   │ MCP      │ Providers │ Function Call │
│ Anthropic│ Code Int.│ Session   │ IChatClient   │
│ Ollama   │ File Srch│ StateBag  │               │
│ Copilot  │ Web Srch │           │               │
└──────────┴──────────┴───────────┴───────────────┘
         │                    │
    ┌────▼────┐         ┌────▼────┐
    │Workflows│         │ Hosting │
    │Executors│         │ A2A     │
    │Edges    │         │ AG-UI   │
    │Checkpoint         │ Azure Fn│
    └─────────┘         └─────────┘
```

## NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.Agents.AI` | Core: `AIAgent`, builder, logging, OpenTelemetry |
| `Microsoft.Agents.AI.Abstractions` | Base abstractions: `AIAgent`, `AgentSession`, `AIContextProvider` |
| `Microsoft.Agents.AI.OpenAI` | Azure OpenAI / OpenAI provider (Chat, Responses, Assistants) |
| `Microsoft.Agents.AI.Anthropic` | Anthropic Claude provider |
| `Microsoft.Agents.AI.Workflows` | Graph-based workflow engine |
| `Microsoft.Agents.AI.Workflows.Generators` | Source generators for `[MessageHandler]` |
| `Microsoft.Agents.AI.A2A` | Agent-to-Agent protocol support |
| `Microsoft.Agents.AI.Hosting` | ASP.NET Core hosting |
| `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` | A2A protocol hosting |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | AG-UI protocol hosting |
| `Microsoft.Agents.AI.DevUI` | Interactive developer UI for debugging |
| `Microsoft.Agents.AI.Declarative` | Declarative agent definitions |
| `Microsoft.Agents.AI.AzureAI.Persistent` | Azure AI Foundry persistent agents |
| `Microsoft.Agents.AI.FoundryMemory` | Azure AI Foundry memory integration |
| `Microsoft.Agents.AI.CosmosNoSql` | Cosmos DB NoSQL context provider |

## 1. Hello Agent — Minimal Setup

```csharp
dotnet add package Azure.AI.OpenAI --prerelease
dotnet add package Azure.Identity
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
```

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;

AIAgent agent = new AzureOpenAIClient(
        new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!),
        new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");

// Non-streaming
Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

// Streaming
await foreach (var update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
{
    Console.Write(update);
}
```

> **Production tip:** Replace `DefaultAzureCredential` with a specific credential (e.g., `ManagedIdentityCredential`) to avoid latency and security risks from fallback mechanisms.

## 2. Agent Providers

Three Azure OpenAI client types with different tool capabilities:

| Client Type | API | Best For | Create Method |
|---|---|---|---|
| **Chat Completion** | Chat Completions | Simple agents, broad model support | `client.GetChatClient(model)` |
| **Responses** | Responses API | Full-featured: code interpreter, file search, hosted MCP | `client.GetResponseClient(model)` |
| **Assistants** | Assistants API | Server-managed agents with persistent state | `client.GetAssistantClient(model)` |

### Provider examples

```csharp
// Chat Completion client
AIAgent chatAgent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are a helpful assistant.", name: "ChatBot");

// Responses client (richer tool support)
AIAgent responsesAgent = client.GetResponseClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are a helpful assistant.", name: "ResponseBot");
```

### OpenAI (non-Azure)

```csharp
using OpenAI;
using OpenAI.Responses;
using Microsoft.Agents.AI;

AIAgent agent = new OpenAIClient("<apikey>")
    .GetResponsesClient("gpt-4o-mini")
    .AsAIAgent(name: "HaikuBot", instructions: "You write beautifully.");
```

## 3. Function Tools

Turn any C# method into an agent tool with `AIFunctionFactory.Create`.

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI;

[Description("Get the weather for a given location.")]
static string GetWeather(
    [Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        instructions: "You are a helpful assistant",
        tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await agent.RunAsync("What is the weather like in Amsterdam?"));
```

**Key points:**
- Use `[Description]` on methods and parameters so the model knows when/how to call them
- Pass tools via `tools:` parameter in `AsAIAgent()`
- Multiple tools: `tools: [AIFunctionFactory.Create(Func1), AIFunctionFactory.Create(Func2)]`

## 4. Multi-turn Conversations (AgentSession)

```csharp
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate.", session));
Console.WriteLine(await agent.RunAsync("Now tell it in a parrot's voice.", session));
// Agent remembers the previous joke from session context
```

### Session serialization (persistence)

```csharp
// Serialize
JsonElement serialized = await agent.SerializeSessionAsync(session);

// Deserialize (resume later)
AgentSession resumed = await agent.DeserializeSessionAsync(serialized);
Console.WriteLine(await agent.RunAsync("What were we talking about?", resumed));
```

## 5. Memory — AIContextProvider

Custom memory components that inject context before each LLM call and extract state after.

```csharp
using Microsoft.Agents.AI;

AIAgent agent = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new() { Instructions = "You are a friendly assistant." },
    AIContextProviders = [new UserInfoMemory(chatClient.AsIChatClient())]
});
```

### Implementing AIContextProvider

Override two key methods:

```csharp
internal sealed class SimpleMemoryProvider : AIContextProvider
{
    private readonly ProviderSessionState<MyState> _sessionState;

    public SimpleMemoryProvider()
        : base(null, null)
    {
        _sessionState = new ProviderSessionState<MyState>(
            _ => new MyState(), this.GetType().Name);
    }

    public override string StateKey => _sessionState.StateKey;

    // Called BEFORE agent invokes LLM — inject context
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = $"User's name is {state.UserName}."
        });
    }

    // Called AFTER agent invokes LLM — extract and store state
    protected override async ValueTask StoreAIContextAsync(
        InvokedContext context, CancellationToken cancellationToken = default)
    {
        // Extract info from conversation and persist to state
        var state = _sessionState.GetOrInitializeState(context.Session);
        // ... update state based on context.RequestMessages
        _sessionState.SaveState(context.Session, state);
    }
}
```

**Advanced override points:**
- `InvokingCoreAsync` — full control over request messages, tools, instructions before LLM call
- `InvokedCoreAsync` — full control over response processing after LLM call

## 6. MCP Tools Integration

### Local MCP server (stdio)

```csharp
using ModelContextProtocol.Client;

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"],
}));

var mcpTools = await mcpClient.ListToolsAsync();

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        instructions: "You answer questions about GitHub repos.",
        tools: [.. mcpTools.Cast<AITool>()]);
```

### Hosted MCP (Azure AI Foundry)

```csharp
using Azure.AI.Agents.Persistent;

var mcpTool = new HostedMcpServerTool(
    serverName: "microsoft_learn",
    serverAddress: "https://learn.microsoft.com/api/mcp")
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire
};

AIAgent agent = await persistentAgentsClient.CreateAIAgentAsync(
    model: "gpt-4o-mini",
    options: new()
    {
        Name = "LearnAgent",
        ChatOptions = new()
        {
            Instructions = "Search Microsoft Learn only.",
            Tools = [mcpTool]
        },
    });
```

### MCP with tool approval (human-in-the-loop)

```csharp
var mcpTool = new HostedMcpServerTool(
    serverName: "microsoft_learn",
    serverAddress: "https://learn.microsoft.com/api/mcp")
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.AlwaysRequire
};

// After RunAsync, check for approval requests:
AgentResponse response = await agent.RunAsync("Search for MCP docs", session);
var approvals = response.Messages
    .SelectMany(m => m.Contents)
    .OfType<McpServerToolApprovalRequestContent>()
    .ToList();

// Approve each request:
List<ChatMessage> userResponses = approvals.ConvertAll(req =>
    new ChatMessage(ChatRole.User, [req.CreateResponse(approved: true)]));
response = await agent.RunAsync(userResponses, session);
```

## 7. Middleware

Three types of middleware, registered via agent builder:

```csharp
// Agent-level middleware (function calling + agent run)
var middlewareAgent = originalAgent
    .AsBuilder()
    .Use(FunctionCallMiddleware)          // function calling
    .Use(PIIMiddleware, null)             // agent run (non-streaming only, pass null for streaming)
    .Use(GuardrailMiddleware, null)       // agent run (chained)
    .Build();

// Chat client-level middleware (applied before building the agent)
var agent = azureOpenAIClient.AsIChatClient()
    .AsBuilder()
    .Use(getResponseFunc: ChatClientMiddleware, getStreamingResponseFunc: null)
    .BuildAIAgent(
        instructions: "You are a helpful assistant.",
        tools: [AIFunctionFactory.Create(GetDateTime)]);
```

### Agent run middleware

The agent run middleware receives the `innerAgent` and calls `innerAgent.RunAsync()` to proceed:

```csharp
async Task<AgentResponse> PIIMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
{
    // Inspect/modify input
    var filteredMessages = FilterPii(messages);
    // Call the next agent in the chain
    var response = await innerAgent.RunAsync(filteredMessages, session, options, cancellationToken);
    // Inspect/modify output
    response.Messages = FilterPii(response.Messages);
    return response;
}
```

### Function calling middleware

```csharp
async ValueTask<object?> FunctionCallMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Function: {context.Function.Name}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"Result: {result}");
    // Set context.Terminate = true to stop function call loop
    return result;
}
```

### IChatClient middleware

```csharp
async Task<ChatResponse> ChatClientMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerClient,
    CancellationToken cancellationToken)
{
    // Inspect/modify before LLM call
    var response = await innerClient.GetResponseAsync(messages, options, cancellationToken);
    // Inspect/modify after LLM call
    return response;
}
```

### AIContextProvider as middleware

```csharp
// Agent-level context provider middleware
var contextAgent = originalAgent
    .AsBuilder()
    .UseAIContextProviders(new DateTimeContextProvider())
    .Build();

// Chat client-level context provider
var agent = azureOpenAIClient.AsIChatClient()
    .AsBuilder()
    .UseAIContextProviders(new DateTimeContextProvider())
    .BuildAIAgent(instructions: "You are a helpful assistant.");
```

## 8. Workflows — Graph-based Orchestration

Workflows connect executors (processing units) via edges (data flow).

### Simple workflow

```csharp
using Microsoft.Agents.AI.Workflows;

// Bind a lambda as an executor
Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

// Custom executor class
var reverse = new ReverseTextExecutor();

// Build the workflow graph
WorkflowBuilder builder = new(uppercase);       // entry executor
builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
var workflow = builder.Build();

// Execute
await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent e)
        Console.WriteLine($"{e.ExecutorId}: {e.Data}");
}
// Output: UppercaseExecutor: HELLO, WORLD!
//         ReverseTextExecutor: !DLROW ,OLLEH
```

### Custom Executor class

```csharp
// Strongly-typed executor with source generator support
internal sealed partial class ReverseTextExecutor() : Executor("ReverseTextExecutor")
{
    [MessageHandler]
    private ValueTask<string> HandleAsync(string message, IWorkflowContext context)
    {
        return ValueTask.FromResult(string.Concat(message.Reverse()));
    }
}
```

Or using the generic base class:

```csharp
internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
    public override ValueTask<string> HandleAsync(
        string message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(string.Concat(message.Reverse()));
    }
}
```

### Key workflow concepts

| Concept | Description |
|---|---|
| **Executor** | Processing unit — custom logic or AI agent |
| **Edge** | Connection between executors (data flow path) |
| **WorkflowBuilder** | Builds the directed graph of executors + edges |
| **InProcessExecution** | Runs the workflow in-process |
| **Run** | Represents a workflow execution with events |
| **Checkpointing** | Save/resume workflow state for long-running processes |
| **`[MessageHandler]`** | Source generator attribute for `partial` executor classes (AOT compatible) |

## 9. Tool Support Matrix

| Tool Type | Chat Completion | Responses | Assistants | Foundry | Anthropic | Ollama |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Function Tools | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tool Approval | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Code Interpreter | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| File Search | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Web Search | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Hosted MCP | ❌ | ✅ | ❌ | ✅ | ✅ | ❌ |
| Local MCP | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## 10. Protocols & Hosting

| Protocol | Package | Description |
|---|---|---|
| **A2A** | `Microsoft.Agents.AI.A2A` | Agent-to-Agent — invoke remote agents as local `AIAgent` |
| **AG-UI** | `Microsoft.Agents.AI.AGUI` | Agent-to-UI streaming protocol |
| **ASP.NET Core** | `Microsoft.Agents.AI.Hosting` | Host agents as web endpoints |
| **Azure Functions** | `Microsoft.Agents.AI.Hosting.AzureFunctions` | Serverless agent hosting (Durable Agents) |
| **Durable Tasks** | `Microsoft.Agents.AI.DurableTask` | Long-running workflows with Azure Durable Functions |

### Azure Functions hosting example

```csharp
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        instructions: "You are a helpful assistant hosted in Azure Functions.",
        name: "HostedAgent");

// ConfigureDurableAgents auto-generates HTTP endpoints: POST /api/agents/{name}/run
using IHost app = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureDurableAgents(options => options.AddAIAgent(agent, timeToLive: TimeSpan.FromHours(1)))
    .Build();
app.Run();
```

## Cheat Sheet

| Task | Pattern |
|---|---|
| Create agent | `chatClient.AsAIAgent(instructions: ..., tools: [...])` |
| Run (non-streaming) | `await agent.RunAsync("prompt")` |
| Run (streaming) | `await foreach (var u in agent.RunStreamingAsync("prompt"))` |
| Multi-turn | `var session = await agent.CreateSessionAsync(); agent.RunAsync("msg", session)` |
| Add function tool | `AIFunctionFactory.Create(MyMethod)` → pass to `tools:` |
| Add MCP tools | `McpClient.CreateAsync(transport)` → `ListToolsAsync()` → cast to `AITool` |
| Memory/context | Implement `AIContextProvider` → pass to `AIContextProviders` in options |
| Middleware (agent) | `agent.AsBuilder().Use(funcMiddleware).Use(runMiddleware, null).Build()` |
| Middleware (chat) | `chatClient.AsBuilder().Use(getResponseFunc: ...).BuildAIAgent(...)` |
| Context middleware | `agent.AsBuilder().UseAIContextProviders(provider).Build()` |
| Serialize session | `agent.SerializeSessionAsync(session)` / `DeserializeSessionAsync(json)` |
| Workflow | `WorkflowBuilder` → `AddEdge()` → `Build()` → `InProcessExecution.RunAsync()` |
| Custom executor | `partial class MyExec : Executor` + `[MessageHandler]` |

## Notes

- **Version:** All examples verified against `dotnet-1.0.0-rc4` (`microsoft/agent-framework`).
- All agents derive from `AIAgent` base class — consistent interface for multi-agent scenarios.
- `IChatClient` from `Microsoft.Extensions.AI` is the core inference abstraction.
- `BuildAIAgent()` on `IChatClient` builder creates agent with chat client middleware pre-configured.
- Agent run middleware uses `innerAgent.RunAsync()` pattern (not a `next` delegate).
- OpenTelemetry integration is built-in via `Microsoft.Agents.AI` package.
- `[MessageHandler]` on workflow executors uses compile-time source generation (Native AOT compatible).
- Sessions are agent/service-specific — do not reuse across different agent configurations.
- Azure Functions hosting uses `ConfigureDurableAgents` with `AddAIAgent()` to auto-generate HTTP endpoints.
