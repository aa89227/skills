---
name: agent-framework
description: |
  Use when building or reviewing AI agents with Microsoft Agent Framework (.NET): AIAgent and
  ChatClientAgent construction, Foundry/OpenAI providers, sessions and background responses,
  function/MCP/hosted tools, tool approval, Agent Skills, AI context providers, middleware,
  workflows, A2A, AG-UI, or Durable/Azure Functions hosting. Trigger phrases: "agent framework",
  "AIAgent", "Microsoft.Agents.AI", "ChatClientAgent", "AgentSkillsProvider", "agent workflow",
  "agent tool", "MCP agent", "A2A agent", "AG-UI", "Durable agent".
license: MIT
metadata:
  author: aa89227
  version: "4.0"
  agent-framework-version: "1.17.0"
  release-note-baseline: "1.0.0"
  tags: ["csharp", "dotnet", "agent-framework", "ai-agent", "workflow", "mcp", "a2a", "agent-skills"]
  trigger_keywords: ["agent-framework", "AIAgent", "Microsoft.Agents", "ChatClientAgent", "AgentSkillsProvider", "workflow", "MCP", "A2A", "AG-UI"]
---

# Microsoft Agent Framework (.NET) — 1.17.0

> Verified against the `dotnet-1.17.0` tag of `microsoft/agent-framework` on 2026-08-07.
> Read [the release-difference checklist](references/release-differences-1.0.0-to-1.17.0.md)
> when a version-sensitive detail matters.

## Version and package rules

- Target the 1.17.0 package family unless the application explicitly pins another version.
- Use .NET 10 for new work; .NET 8 and .NET 9 remain supported by the framework guidance.
- Treat `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Abstractions` as the core API surface.
- Select a provider package deliberately: `Microsoft.Agents.AI.OpenAI`, Foundry packages,
  `Microsoft.Agents.AI.Anthropic`, Copilot, or another provider listed in
  [packages-protocols.md](references/packages-protocols.md).
- Add only the packages needed by the chosen feature. MCP, hosting, AG-UI, declarative workflows,
  and provider integrations have independent preview/alpha/stable status.
- Hyperlight, LocalCodeAct, Harness, Shell, hosted Files/RAG, and ToolboxMcpSkills are
  provider/integration-specific surfaces; isolate them behind a capability boundary.
- Treat every `Experimental`/preview/alpha API warning as a versioned contract; do not suppress it
  blindly. Check the 1.17.0 package and release notes before copying a sample.
- The old `Microsoft.Agents.AI.AGUI` package/API is gone. Use the external `AGUI.*` packages and
  `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` with `AddAGUIServer`/`MapAGUIServer`.
- Durable Task/Azure Functions integrations moved to
  [microsoft/agent-framework-durable-extension](https://github.com/microsoft/agent-framework-durable-extension).
  Package names remain the same, but those projects are no longer built in the core repository.

## Quick reference

| Task | Current pattern |
|---|---|
| Create Foundry agent | `AIProjectClient(...).AsAIAgent(model: ..., instructions: ..., name: ...)` |
| Create from chat client | `IChatClient.AsAIAgent(...)` or `ChatClientAgent` |
| Run | `await agent.RunAsync("prompt")` |
| Stream | `await foreach (var update in agent.RunStreamingAsync("prompt"))` |
| Multi-turn | `var session = await agent.CreateSessionAsync(); await agent.RunAsync("...", session)` |
| Function tool | `AIFunctionFactory.Create(method)` in `ChatOptions.Tools` or agent options |
| Per-run options/tools | `ChatClientAgentRunOptions(new ChatOptions { Tools = [...] })` |
| Local MCP | MCP SDK `McpClient` → `ListToolsAsync()` → pass returned `AITool`s |
| MCP tasks | `ListAgentToolsWithTaskSupportAsync(new McpTaskOptions { ... })` |
| Hosted MCP | Provider/Responses hosted MCP tool; require approval for untrusted calls |
| Context | `AIContextProvider`; use `MessageAIContextProvider` for message-only enrichment |
| Agent Skills | `AgentSkillsProvider` or `AgentSkillsProviderBuilder` |
| Agent middleware | Run middleware calls its supplied `innerAgent`; it is not a `next` delegate |
| Function middleware | `FunctionInvocationContext` plus the `next` delegate |
| Agent as tool | `agent.AsAIFunction()` |
| Session persistence | Framework serialize/deserialize or provider-specific session export/import |
| Workflow | `WorkflowBuilder` + generated `Executor` handlers + `InProcessExecution` |
| A2A client | `A2ACardResolver` → `AgentCard.AsAIAgent()` |

## Agent construction and execution

Prefer the provider's current adapter and keep provider-specific setup outside business logic:

```csharp
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

var project = new AIProjectClient(
    new Uri(Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")!),
    new DefaultAzureCredential());

AIAgent agent = project.AsAIAgent(
    model: Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5.4-mini",
    instructions: "You are a concise assistant.",
    name: "Assistant");

AgentResponse response = await agent.RunAsync("Summarize the current task.");
await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Give one next step."))
{
    Console.Write(update.Text);
}
```

- Use `ManagedIdentityCredential` in production where applicable; `DefaultAzureCredential` is
  convenient for local development but can add credential-probing latency.
- For OpenAI/Azure OpenAI, obtain the appropriate `IChatClient` or Responses client and call
  `AsAIAgent`; use Responses when code interpreter, file search, hosted MCP, or other Responses
  features are required.
- Keep `AIAgent` as the application-facing abstraction. Do not leak a provider client into every
  tool or workflow executor.
- Use `ChatClientAgentOptions`/`ChatOptions` for instructions, model, tools, and context providers;
  use `ChatClientAgentRunOptions` for per-run overrides.

## Sessions, persistence, and background responses

```csharp
using System.Text.Json;

AgentSession session = await agent.CreateSessionAsync();
await agent.RunAsync("Remember that the project is called Northwind.", session);
AgentResponse answer = await agent.RunAsync("What is the project called?", session);

JsonElement serialized = await agent.SerializeSessionAsync(session);
AgentSession restored = await agent.DeserializeSessionAsync(serialized);
```

- Treat sessions as agent/service-specific. Do not reuse one session with a different agent
  configuration or provider unless that provider explicitly supports it.
- Use provider-supported conversation IDs or session export/import when the service owns history;
  verify the provider's serialization contract.
- For long-running work, run with `AgentRunOptions { AllowBackgroundResponses = true }`. If the
  response contains a `ContinuationToken`, persist it and poll by passing it back in a subsequent
  run. Serialize the token with `AgentAbstractionsJsonUtilities.DefaultOptions` when it crosses a
  process boundary.
- Tool-approval agents can create a session when one is absent in current versions; still create
  and persist sessions explicitly when conversation continuity matters.
- Use the provider's chat-history implementation when service-stored history is required; test
  persistence with the actual provider because in-memory/service-backed behavior has changed.

## Tools and approvals

Create narrowly scoped tools and treat every model-supplied argument as untrusted input:

```csharp
using Microsoft.Extensions.AI;

AITool weather = AIFunctionFactory.Create(
    (string city) => GetWeather(city),
    name: "get_weather",
    description: "Get the current weather for a city.");

var runOptions = new ChatClientAgentRunOptions(
    new ChatOptions { Tools = [weather] });
AgentResponse result = await agent.RunAsync("Check Seattle.", runOptions);
```

- Validate arguments, constrain filesystem/network access, set timeouts, and return bounded
  results. A tool description is not an authorization policy.
- Check `AgentResponse` messages for `ToolApprovalRequestContent` and surface approval requests to
  the user or an appropriate policy engine before executing side effects.
- Use `ToolApprovalAgent`/`UseToolApproval` for approval interception. Bind an approval response to
  the exact surfaced approval; do not approve by a display name alone when names can collide.
- Since 1.11.1, all `AgentSkillsProvider` tools require approval by default. Preserve that default
  for untrusted skills. If a trusted, read-only source is intentionally auto-approved, configure a
  narrow rule such as `AgentSkillsProvider.ReadOnlyToolsAutoApprovalRule`; use
  `AllToolsAutoApprovalRule` only for a deliberately trusted boundary.
- Re-check approval behavior when upgrading `ToolApprovalAgent`, FileAccess, Copilot, or hosted
  tools; auto-approval rules and approval content changed across 1.10–1.14.
- Use provider-native tools such as code interpreter, file search, web search, and hosted MCP only
  with the provider/client combination that supports them. See the support matrix in the reference.

## MCP integration

For local MCP, create an MCP SDK client with the desired transport, list tools, and pass the SDK
tools to the agent. Keep the transport process, environment, working directory, and server trust
policy explicit. Hosted MCP is provider-specific and generally uses Responses/hosted tool APIs.

For MCP long-running tasks, add `Microsoft.Agents.AI.Mcp` and use the task-aware adapter:

```csharp
var tools = await mcpClient.ListAgentToolsWithTaskSupportAsync(
    new McpTaskOptions { DefaultTimeToLive = TimeSpan.FromMinutes(10) });
```

The adapter handles `tasks/call`, polling, and `tasks/result`; application code should still apply
timeouts, cancellation, approval, and result-size limits. MCP authentication headers can be
refreshed per run in current versions.

## Agent Skills and progressive disclosure

Use `AgentSkillsProvider` when the model should discover a skill, load its instructions, read
resources, and run scripts only as needed. Keep the initial tool surface small:

1. Discover the skill.
2. Load `SKILL.md` instructions.
3. Read only the requested resource.
4. Run a script only after validating its arguments and approval.

Supported sources include file skills, inline/class-defined skills, MCP `skill-md`/archive skills,
and composed sources through `AgentSkillsProviderBuilder`. Prefer the builder when combining
sources, filters, caching, runners, or options.

- A file skill's `SKILL.md` name must use lowercase letters, numbers, and single hyphens, be at
  most 64 characters, and have no leading/trailing/consecutive hyphens. Keep its description at
  most 1024 characters; use optional compatibility/license/metadata fields deliberately.
- Resource and script lookup is asynchronous. Current skill bodies expose resource/script
  descriptions and `available_resources`/`available_scripts`; do not use the removed instruction
  placeholders.
- File skills enforce bounded discovery/search, supported extensions, path-traversal/symlink
  checks, and file-size limits. Treat a skill directory and MCP server as a trust boundary.
- FileAccess/file-editing tools now use explicit directory discovery, recursive-search, and store
  APIs. Keep roots, recursion, edits, and approval scope explicit; do not assume the 1.0 behavior.
- File scripts receive CLI arguments as a JSON `string[]`; use a safe subprocess runner, validate
  the working directory/environment, and set `IncludeDetailedErrors` only when disclosure is safe.
- Class skills use frontmatter/description attributes and async resource/script methods. Supply
  serializer metadata/options when Native AOT requires it; custom argument marshaling is supported.
- `AgentSkillsSource` instances are public/disposable. `GetSkillsAsync` receives an
  `AgentSkillsSourceContext` containing the agent/session; dispose sources and configure caching
  with `CachingAgentSkillsSource` or the builder's caching options.
- MCP skill archives, `skill://index.json`, and server-provided resources are remote input. Verify
  server identity, allowed paths, extensions, size limits, and approval rules before enabling them.

## Memory and context providers

Use `AIContextProvider` for durable/contextual enrichment and stateful memory. Implement the
invoking and invoked hooks as appropriate, persist provider state in the session, and avoid
embedding secrets in prompt text. Use `ProviderSessionState<T>`/provider state keys rather than
inventing a second session store.

Use `MessageAIContextProvider` when the provider only needs to add or filter messages. Use
`IChatMessageInjector`/message injection for provider-supported message insertion and preserve
message order and portability. `TodoProvider` and `AgentModeProvider` are current context
providers: their state belongs to the session and their tools need the same approval policy as
other tools.

Register providers through `ChatClientAgentOptions.AIContextProviders` or the builder's
`UseAIContextProviders`. Keep provider order intentional when multiple providers add context.
Test compaction, service-backed chat history, session restore, and prompt-injection boundaries.

## Middleware

Keep the middleware layer matched to the intercepted abstraction:

- Agent-run middleware receives messages/session/options plus an `innerAgent`; invoke that agent to
  continue. It does not use the function-middleware `next` delegate pattern.
- Function middleware receives `FunctionInvocationContext` and a `next` delegate; validate,
  authorize, time-limit, log, or redact the function invocation there.
- Chat middleware wraps `IChatClient`; use it for provider-independent request/response policies.
- Compose with the agent builder and preserve cancellation, session, run options, tool approvals,
  and exceptions. Do not swallow an agent error that a declarative workflow must observe.

## Workflows and orchestration

For deterministic graphs, use `WorkflowBuilder`, bind agents/functions as executors, connect them
with explicit edges/routes, then run with `InProcessExecution`. For custom executors, use a partial
`Executor` with generated `[MessageHandler]` methods and add
`Microsoft.Agents.AI.Workflows.Generators`; keep handlers small and serializable for checkpointing.

- Declarative workflow packages are stable in the current line; declarative code generation was
  removed, so do not generate source using the old workflow-codegen path.
- Use AOT-safe `DeclarativeWorkflowJsonOptions` when serializing declarative workflow state.
- Checkpointed workflows need stable agent IDs and explicit state schemas. Current declarative
  workflows propagate an agent error as a workflow failure; do not silently continue.
- Use `LoopAgent` only with explicit stop conditions and budgets; multi-agent Magentic rounds are
  not an unbounded retry mechanism.
- Magentic orchestration is available through specialized workflow APIs. Treat its builder/events
  as version-sensitive and cap rounds, stalls, resets, tool permissions, and budget.
- Durable workflow orchestration is maintained in the external Durable extension repository; do
  not add new in-tree Azure Functions imports based on old 1.0 samples.

## A2A, AG-UI, and hosting

For A2A clients, resolve the remote card and adapt it to `AIAgent`:

```csharp
A2ACardResolver resolver = new(new Uri("https://remote-agent.example"));
AgentCard card = await resolver.GetAgentCardAsync();
AIAgent remoteAgent = card.AsAIAgent();
```

Use current A2A session/options support for streaming handlers, reference task IDs,
`input-required` human-in-the-loop content, background responses, and forwarded configuration. For ASP.NET Core hosting, use the
current `Microsoft.Agents.AI.Hosting.A2A.AspNetCore`/hosting packages and protect endpoints.

For AG-UI, install the external official packages as needed: `AGUI.Abstractions`, `AGUI.Client`,
`AGUI.Server`, and optionally `AGUI.Formatting`/`AGUI.Protobuf`. The MAF ASP.NET Core integration
is `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`:

```csharp
builder.Services.AddAGUIServer();
app.MapAGUIServer("/", agent);
```

Use the split namespaces and options-based client APIs; `AddAGUI`/`MapAGUI` and the old
`Microsoft.Agents.AI.AGUI` package are migration-only references. Current AG-UI integrations may
also emit reasoning events; preserve event history and approval-response matching.

## Observability, identity, and security

- Configure OpenTelemetry deliberately. Since 1.6.1, agent setup can auto-wire
  `OpenTelemetryChatClient`; avoid double instrumentation and verify exporter/privacy settings.
- Run DevUI behind HTTPS/access control outside local development, and do not expose prompts,
  tool arguments, or credentials through diagnostic endpoints.
- Prefer managed identity in deployed Azure workloads; never log credentials, bearer tokens,
  prompt secrets, tool arguments, or raw provider responses without a data policy.
- Treat remote MCP servers, Agent Skills, hosted tools, file access, code execution, and A2A
  endpoints as untrusted until authenticated and constrained.
- Set cancellation, timeouts, retry limits, response-size limits, and cost/budget limits on every
  external call. Redact non-portable content before forwarding messages across providers.
- Keep approval, file, shell, web-browsing, and network allowlists explicit and test both approval
  and denial paths.

## Bundled references and examples

- [packages-protocols.md](references/packages-protocols.md): package status, provider/tool matrix,
  protocol migrations, and workflow concepts.
- [release-differences-1.0.0-to-1.17.0.md](references/release-differences-1.0.0-to-1.17.0.md):
  the version-by-version maintenance checklist used for this update.
- [examples/hello-agent.cs](examples/hello-agent.cs): Foundry-first creation and basic runs.
- [examples/sessions-memory.cs](examples/sessions-memory.cs): sessions, continuation, and context.
- [examples/middleware.cs](examples/middleware.cs): agent/function/chat middleware.
- [examples/mcp-tools.cs](examples/mcp-tools.cs): local/hosted MCP, tasks, and approval.
- [examples/workflows.cs](examples/workflows.cs): graph workflows, generated handlers, and current
  orchestration/hosting boundaries.
