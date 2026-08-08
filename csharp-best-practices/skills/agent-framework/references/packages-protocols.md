# Microsoft Agent Framework — packages and protocols (`dotnet-1.17.0`)

This reference complements `SKILL.md`. It records the package ownership and protocol changes that
are easy to get wrong when upgrading from the repository's former 1.0.0 guidance. Package status
is version-sensitive; verify the application's exact package graph before shipping.

## Contents

- [Core and workflow packages](#core-and-workflow-packages)
- [Providers and integrations](#providers-and-integrations)
- [Hosting and protocol packages](#hosting-and-protocol-packages)
- [Tools and provider capability matrix](#tools-and-provider-capability-matrix)
- [Agent Skills runtime surface](#agent-skills-runtime-surface)
- [Context, memory, and session state](#context-memory-and-session-state)
- [Middleware and approval boundaries](#middleware-and-approval-boundaries)
- [Workflow concepts](#workflow-concepts)
- [Upgrade traps from 1.0.0](#upgrade-traps-from-100)

## Core and workflow packages

| Package | Role | 1.17.0 guidance |
|---|---|---|
| `Microsoft.Agents.AI` | `AIAgent`, sessions, tools, context, middleware | Core API |
| `Microsoft.Agents.AI.Abstractions` | Shared agent/session/response abstractions | Core dependency |
| `Microsoft.Agents.AI.Workflows` | Executors, edges, runs, checkpoints | Use for graph workflows |
| `Microsoft.Agents.AI.Workflows.Generators` | `[MessageHandler]` source generation | Use for generated executor handlers/AOT |
| `Microsoft.Agents.AI.Workflows.Declarative` | Declarative workflow runtime | Stable in the current line; old codegen is removed |
| `Microsoft.Agents.AI.Workflows.Declarative.Foundry` | Foundry declarative integration | Add only for Foundry declarative workflows |
| `Microsoft.Agents.AI.Workflows.Declarative.Mcp` | MCP declarative integration | Add only when declarative MCP is used |

Declarative workflow errors now fail the workflow when an agent returns an error. Checkpointed
workflows should use stable agent IDs and AOT-safe `DeclarativeWorkflowJsonOptions`.

## Providers and integrations

| Package | Role | Status/notes |
|---|---|---|
| `Microsoft.Agents.AI.OpenAI` | OpenAI/Azure OpenAI adapters | Chat Completions and Responses paths; inspect experimental warnings |
| `Microsoft.Agents.AI.Mcp` | MCP task adapter and MCP Agent Skills extensions | Alpha/experimental; supports long-running task wrapping |
| `Microsoft.Agents.AI.Foundry` | Azure AI Foundry project/agent integration | Prefer current `AIProjectClient.AsAIAgent` samples |
| `Microsoft.Agents.AI.Foundry.Hosting` | Foundry hosted-agent hosting | Version-sensitive hosting API |
| `Microsoft.Agents.AI.AzureAI.Persistent` | Azure AI persistent agents | Provider-specific session behavior |
| `Microsoft.Agents.AI.Anthropic` | Anthropic adapter | Provider-specific tool/content limits |
| `Microsoft.Agents.AI.CopilotStudio` | Copilot Studio integration | Treat remote actions as approval boundaries |
| `Microsoft.Agents.AI.GitHub.Copilot` | GitHub Copilot SDK agent | Stable in the 1.16 line; verify SDK compatibility |
| `Microsoft.Agents.AI.Harness` | Harness agent integration | FileAccess is opt-in in the current line |
| `Microsoft.Agents.AI.CosmosNoSql` | Cosmos DB context/history provider | Test service-backed persistence |
| `Microsoft.Agents.AI.Mem0` | Mem0 memory integration | External service and data policy required |
| `Microsoft.Agents.AI.Purview` | Purview/compliance tools | Configure tenant permissions and redaction |
| `Microsoft.Agents.AI.Valkey` | Valkey chat history | Use only when the deployment owns the store |
| `Microsoft.Agents.AI.Hyperlight` | Hyperlight execution integration | Preview/version-sensitive |
| `Microsoft.Agents.AI.LocalCodeAct` | Local CodeAct integration | Constrain process/filesystem/network access |
| `Microsoft.Agents.AI.Tools.Shell` | Shell tool | Treat commands and output as untrusted |
| `Microsoft.Agents.AI.DevUI` | Interactive development UI | Configure HTTPS/access control outside local-only use |

The 1.17.0 source dependency snapshot includes Microsoft.Extensions.AI 10.7.0, OpenAI 2.10.0,
ModelContextProtocol 1.2.0, OpenTelemetry 1.15.3, GitHub Copilot SDK 1.0.5, and A2A
1.0.0-preview2. These are a tag snapshot, not a promise that an application should override its
transitive versions.

## Hosting and protocol packages

| Package/protocol | Purpose | Current rule |
|---|---|---|
| `Microsoft.Agents.AI.Hosting` / `.AspNetCore` | Common ASP.NET Core hosting | Protect endpoints and configure identity |
| `Microsoft.Agents.AI.Hosting.A2A` | A2A hosting abstractions | Use current AgentCard/session options |
| `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` | A2A HTTP hosting | Supports input-required/background flows |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | MAF AG-UI ASP.NET Core adapter | Use `AddAGUIServer`/`MapAGUIServer` |
| `Microsoft.Agents.AI.Hosting.OpenAI` | OpenAI-compatible hosting | Alpha/version-sensitive; options mapping is explicit |
| A2A | Agent-to-Agent protocol | `A2ACardResolver` → `AgentCard.AsAIAgent()` |
| AG-UI | Agent-to-UI event protocol | External `AGUI.*` SDK plus MAF hosting adapter |

### AG-UI package migration

`Microsoft.Agents.AI.AGUI` was removed in 1.14.0. Install the external official packages as
needed: `AGUI.Abstractions`, `AGUI.Client`, `AGUI.Server`, and optionally `AGUI.Formatting` and
`AGUI.Protobuf`. The 1.17.0 MAF source pins the external package family at 0.0.3; verify the
compatible external release before changing it.

```csharp
builder.Services.AddAGUIServer();
app.MapAGUIServer("/", agent);
```

The old `AddAGUI`/`MapAGUI` names and old MAF AG-UI namespace are migration references only.

### Durable/Azure Functions ownership migration

Durable Task and Azure Functions projects moved out of the core repository in 1.17.0 to
[`microsoft/agent-framework-durable-extension`](https://github.com/microsoft/agent-framework-durable-extension).
The package names remain compatible, but source/build ownership and release instructions now live
there. Do not infer that the core 1.17.0 repository contains those projects.

## Tools and provider capability matrix

The matrix describes the intended capability boundary, not a guarantee for every model:

| Capability | Chat Completions | Responses | Foundry | Anthropic | Ollama/local |
|---|:---:|:---:|:---:|:---:|:---:|
| Function tools | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tool approval content | Provider-dependent | ✅ | ✅ | Provider-dependent | Provider-dependent |
| Code interpreter | — | ✅ | ✅ | — | — |
| File search | — | ✅ | ✅ | — | — |
| Web search/browsing | ✅ | ✅ | Provider-dependent | — | — |
| Hosted MCP | — | ✅ | ✅ | ✅ | — |
| Local MCP | ✅ | ✅ | ✅ | ✅ | ✅ |
| Agent Skills provider | ✅ | ✅ | ✅ | Provider-dependent | Provider-dependent |

Use provider-native client types and inspect the resulting `ChatOptions`/tool support. A provider
may accept a tool object while rejecting a content type, approval response, or hosted tool.

## Agent Skills runtime surface

| Surface | Current API/behavior |
|---|---|
| File/inline/class source | `AgentSkillsProvider`, `AgentInlineSkill`, `AgentClassSkill<T>` |
| Composition | `AgentSkillsProviderBuilder` with file, skill, MCP, filter, runner, source, and cache methods |
| MCP source | `UseMcpSkills(mcpClient, options)`; `skill://index.json`, `skill-md`, archive skills |
| Progressive tools | Discover/load skill, read resource, run script |
| Approval default | All three Agent Skills tools require approval by default since 1.11.1 |
| Resource/script lookup | Async; descriptions and available resource/script lists are exposed |
| File script arguments | JSON `string[]` CLI arguments |
| Source context | `AgentSkillsSourceContext` carries agent/session into `GetSkillsAsync` |
| Caching | `CachingAgentSkillsSource` or builder caching options; sources are disposable |
| Trust boundary | Validate path, symlink/traversal, extensions, file sizes, subprocess, and MCP server |

For class-defined skills, reflection discovers resources/scripts. Provide serializer metadata/options
for Native AOT and use custom argument marshaling when a script needs structured arguments.

## Context, memory, and session state

| Surface | Use |
|---|---|
| `AIContextProvider` | Stateful durable context; implement invoking/invoked hooks as needed |
| `MessageAIContextProvider` | Message-only enrichment/filtering |
| `ProviderSessionState<T>` | Provider-owned state serialized with the session |
| `IChatMessageInjector` | Provider-supported message injection |
| `TodoProvider` | Session-backed todo tools/context |
| `AgentModeProvider` | Session-backed plan/execute or custom modes |
| Chat history providers | Service/in-memory/Cosmos/Valkey-specific history; test persistence behavior |

Provider order matters when several providers add context. Do not put secrets into injected prompt
text, and do not use a second uncoordinated state store for provider session state.

## Middleware and approval boundaries

| Layer | Continuation model |
|---|---|
| Agent run | Receives `innerAgent`; call its `RunAsync` to continue |
| Function invocation | Receives `FunctionInvocationContext` and `next` |
| Chat client | Wraps `IChatClient`; continue through the wrapped client |
| Tool approval | Intercepts approval request/response; bind to the surfaced approval identity |

Since 1.10–1.14, auto-approval rules, FileAccess, Harness, Copilot enforcement, approval names,
and surfaced approval binding have changed. Keep approval policy explicit and covered by allow/deny
tests.

## Workflow concepts

| Concept | Meaning |
|---|---|
| `Executor` | Processing unit for an agent or deterministic operation |
| `Executor<TIn,TOut>` | Typed input/output executor |
| `WorkflowBuilder` | Directed graph construction, edges, and routes |
| `[MessageHandler]` | Generated handler method; use `partial` executor classes |
| `InProcessExecution` | In-process execution modes and event stream |
| Checkpoint | Serializable state for pause/resume; use stable agent IDs |
| Declarative workflow | Runtime-defined workflow; old codegen path removed |
| Magentic | Specialized multi-agent orchestration; cap rounds/stalls/resets/budget |

Use explicit error handling. A declarative workflow must observe an agent failure as a workflow
failure in the current release.

## Upgrade traps from 1.0.0

- Do not use the old `Microsoft.Agents.AI.AGUI`, `AddAGUI`, or `MapAGUI` surface.
- Do not use synchronous Agent Skill resource/script lookup, old script-body resource placeholders,
  or old file-skill depth/filter assumptions.
- Do not assume Agent Skills tools are safe to auto-approve; 1.11.1 made approval the default.
- Do not use old declarative workflow code generation.
- Do not assume Durable/Azure Functions projects are still in the core source tree.
- Do not swallow agent errors in a declarative workflow.
- Do not rely on a tool display name as an approval identity.
- Do not double-register OpenTelemetry after current agent setup auto-wires the chat client.
