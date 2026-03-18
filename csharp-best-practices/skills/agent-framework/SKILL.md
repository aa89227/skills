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
  version: "2.0"
  agent-framework-version: "1.0.0-rc4"
  tags: ["csharp", "dotnet", "agent-framework", "ai-agent", "workflow", "mcp", "a2a"]
  trigger_keywords: ["agent-framework", "AIAgent", "Microsoft.Agents", "ChatClientAgent", "workflow", "MCP", "A2A"]
---

# Microsoft Agent Framework (.NET) — 1.0.0-rc4

> Verified against `dotnet-1.0.0-rc4` tag of `microsoft/agent-framework`.

## Quick Reference

| Item | Value |
|---|---|
| Core package | `Microsoft.Agents.AI` |
| Provider package | `Microsoft.Agents.AI.OpenAI` (Azure OpenAI / OpenAI) |
| Namespace | `Microsoft.Agents.AI` |
| Base class | `AIAgent` — all agents derive from this |
| Requires | .NET 10+ recommended, .NET 8+ supported |
| Repo | `github.com/microsoft/agent-framework` |
| Docs | `learn.microsoft.com/en-us/agent-framework/` |

## Architecture

```
┌─────────────────────────────────────────────────┐
│                  AIAgent (base)                  │
├──────────┬──────────┬───────────┬───────────────┤
│ Providers│  Tools   │  Memory   │  Middleware    │
│ AzureOAI │ Function │ Context   │ Agent Run     │
│ OpenAI   │ MCP      │ Providers │ Function Call │
│ Anthropic│ Code Int.│ Session   │ IChatClient   │
│ Ollama   │ File Srch│ StateBag  │               │
└──────────┴──────────┴───────────┴───────────────┘
         │                    │
    ┌────▼────┐         ┌────▼────┐
    │Workflows│         │ Hosting │
    │Executors│         │ A2A     │
    │Edges    │         │ AG-UI   │
    │Checkpoint         │ Azure Fn│
    └─────────┘         └─────────┘
```

## Core Rules

- All agents derive from `AIAgent` — consistent interface for multi-agent scenarios.
- `IChatClient` from `Microsoft.Extensions.AI` is the core inference abstraction.
- Agent run middleware uses `innerAgent.RunAsync()` pattern — **not** a `next` delegate.
- `BuildAIAgent()` on `IChatClient` builder creates agent with chat client middleware pre-configured.
- Production: replace `DefaultAzureCredential` with `ManagedIdentityCredential` to avoid latency.
- Sessions are agent/service-specific — do not reuse across different agent configurations.
- `[MessageHandler]` on workflow executors uses compile-time source generation (Native AOT compatible).
- OpenTelemetry integration is built-in via `Microsoft.Agents.AI` package.

## Cheat Sheet

| Task | Pattern |
|---|---|
| Create agent | `chatClient.AsAIAgent(instructions: ..., tools: [...])` |
| Run (non-streaming) | `await agent.RunAsync("prompt")` |
| Run (streaming) | `await foreach (var u in agent.RunStreamingAsync("prompt"))` |
| Multi-turn | `var s = await agent.CreateSessionAsync(); agent.RunAsync("msg", s)` |
| Function tool | `AIFunctionFactory.Create(MyMethod)` → pass to `tools:` |
| Local MCP tools | `McpClient.CreateAsync(transport)` → `ListToolsAsync()` → cast to `AITool` |
| Hosted MCP | `new HostedMcpServerTool(serverName, serverAddress)` |
| MCP approval | Check `AgentResponse` for `McpServerToolApprovalRequestContent` |
| Memory/context | Implement `AIContextProvider` → pass to `AIContextProviders` in options |
| Agent middleware | `agent.AsBuilder().Use(runMiddleware, null).Build()` |
| Function middleware | `agent.AsBuilder().Use(funcMiddleware).Build()` |
| Chat middleware | `chatClient.AsBuilder().Use(getResponseFunc: ...).BuildAIAgent(...)` |
| Context middleware | `agent.AsBuilder().UseAIContextProviders(provider).Build()` |
| Serialize session | `agent.SerializeSessionAsync(session)` |
| Deserialize session | `agent.DeserializeSessionAsync(json)` |
| Workflow | `WorkflowBuilder` → `AddEdge()` → `Build()` → `InProcessExecution.RunAsync()` |
| Custom executor | `partial class MyExec : Executor` + `[MessageHandler]` |
| Azure Fn hosting | `FunctionsApplication.CreateBuilder(args).ConfigureDurableAgents(opts => opts.AddAIAgent(...))` |

## Additional Resources

### Example Files

Complete, runnable `.cs` examples in `examples/`:
- **`examples/hello-agent.cs`** — Minimal setup, Azure OpenAI, streaming, Chat vs Responses provider, function tools
- **`examples/sessions-memory.cs`** — AgentSession multi-turn, session serialization, AIContextProvider implementation
- **`examples/middleware.cs`** — Agent run, function calling, IChatClient middleware; AIContextProvider as middleware
- **`examples/mcp-tools.cs`** — Local MCP (stdio), hosted MCP (Azure AI Foundry), tool approval (human-in-the-loop)
- **`examples/workflows.cs`** — WorkflowBuilder, lambda executor, custom Executor class, `[MessageHandler]`, Azure Functions hosting

### Reference Files

Detailed tables in `references/`:
- **`references/packages-protocols.md`** — All NuGet packages, provider client types, tool support matrix, protocols, workflow concepts
