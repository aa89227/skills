# Microsoft Agent Framework .NET release differences

This is the temporary maintenance checklist used to update the repository's `agent-framework`
skill. It covers every published .NET release note between the project's previous baseline and
the current release. It is intentionally a condensed, actionable paraphrase rather than a copy
of the upstream notes.

## Scope and baseline

- Previous project baseline: `dotnet-1.0.0` (`Microsoft Agent Framework` 1.0.0).
- Current upstream .NET release checked: `dotnet-1.17.0`, published 2026-08-04.
- Review date: 2026-08-07.
- Upstream release index: <https://github.com/microsoft/agent-framework/releases>.
- The upstream repository has no separate GitHub release tag for `dotnet-1.6.0`; NuGet history
  includes 1.6.0, so the gap is recorded rather than silently skipped.
- Python-only release-note entries are not translated into this .NET skill. Package/API claims
  below are additionally checked against the `dotnet-1.17.0` source tree.

## Contents

- [Scope and baseline](#scope-and-baseline)
- [Published .NET release sequence](#published-net-release-sequence)
- [Release-by-release change list](#release-by-release-change-list)
- [Consolidated update checklist](#consolidated-update-checklist)
- [Post-update verification](#post-update-verification)
- [Sources](#sources)

## Published .NET release sequence

| Release | Published | Note |
|---|---|---|
| `dotnet-1.0.0` | 2026-04-02 | Project baseline |
| `dotnet-1.1.0` | 2026-04-10 | Published release note |
| `dotnet-1.2.0` | 2026-04-21 | Published release note |
| `dotnet-1.3.0` | 2026-04-24 | Published release note |
| `dotnet-1.4.0` | 2026-05-05 | Published release note |
| `dotnet-1.5.0` | 2026-05-08 | Published release note |
| `dotnet-1.6.0` | — | No standalone upstream GitHub release note |
| `dotnet-1.6.1` | 2026-05-14 | Published release note |
| `dotnet-1.6.2` | 2026-06-02 | Published release note |
| `dotnet-1.7.0` | 2026-06-02 | Published release note |
| `dotnet-1.8.0` | 2026-06-02 | Published release note |
| `dotnet-1.9.0` | 2026-06-03 | Published release note |
| `dotnet-1.10.0` | 2026-06-10 | Published release note |
| `dotnet-1.11.0` | 2026-06-23 | Published release note |
| `dotnet-1.11.1` | 2026-06-25 | Published release note |
| `dotnet-1.12.0` | 2026-07-02 | Published release note |
| `dotnet-1.13.0` | 2026-07-03 | Published release note |
| `dotnet-1.14.0` | 2026-07-21 | Published release note |
| `dotnet-1.15.0` | 2026-07-22 | Published release note |
| `dotnet-1.16.0` | 2026-07-30 | Published release note |
| `dotnet-1.17.0` | 2026-08-04 | Current release at review time |

## Release-by-release change list

### 1.0.0 — baseline

- Establishes the `AIAgent`/`AgentSession` model, `IChatClient` integration, function tools,
  MCP, AI context providers, middleware, workflows, A2A, AG-UI, and Durable/Azure Functions
  integrations used by the original skill.

### 1.1.0

- Adds class-based skill resource/script reflection discovery and custom argument types.
- Aligns skill-directory discovery with the skill specification.
- Adds message-delivery callback overloads to workflow `Executor` APIs.
- Adds `CreateSessionAsync(conversationId)` support for Foundry-backed agents and fixes
  compaction/session edge cases.
- Skill impact: document class-defined skills and keep resource/script access asynchronous.

### 1.2.0

- Adds Foundry Hosted Agents, Handoff-hosted-agent sessions, Foundry evaluations, and Aspire
  DevUI integration.
- Adds Handoff and code-interpreter-container file-download samples.
- Improves declarative workflow no-response handling/checkpoint edge cases.
- Updates the underlying Microsoft.Extensions.AI/OpenAI dependencies.
- Skill impact: include Foundry-first construction, Handoff session behavior, DevUI, and hosted
  Responses capabilities as supported variants.

### 1.3.0

- Adds dynamic tool-expansion guidance/sample, A2A streaming handlers, and server-side Foundry
  Toolbox support.
- Includes package updates and a workflow race-condition fix.
- Skill impact: mention A2A streaming and dynamic/hosted tool surfaces without presenting them as
  ordinary local function tools.

### 1.4.0

- Updates OpenTelemetry to 1.15.3.
- Adds durable workflow HTTP results and declarative `HttpRequestAction`.
- **Breaking:** file-based skill scripts receive arguments as `string[]`.
- Adds the `Microsoft.Agents.AI.Hyperlight` integration and hosted-agent User-Agent support.
- Skill impact: document the script-argument contract and the Hyperlight/declarative variants.

### 1.5.0

- Filters non-portable message content and fixes `MultiPartyConversation` serialization.
- Adds `WebBrowsingTool` allowlisting and improves Todo-provider concurrency/message injection.
- Adds AG-UI reasoning events and GitHub Copilot SDK beta.2 support.
- Adds experimental .NET Magentic orchestration and updates function-call output handling.
- Skill impact: add allowlists, message portability, Todo injection, AG-UI reasoning, and clearly
  label Magentic as version-sensitive/experimental at this stage.

### 1.6.0

- No standalone upstream GitHub release note was published for this tag. Do not invent a release
  entry; use 1.6.1 as the next documented release and treat package 1.6.0 as part of the version
  history only.

### 1.6.1

- Releases Hyperlight and adds `IChatMessageInjector`.
- Adds hosted Files/`AgentSessionFiles` and hosted RAG support.
- Removes server-side Foundry Toolbox tools and adds DevUI access control.
- Adds A2A input-request content for human-in-the-loop flows, shell tools, and the Harness agent.
- Adds file-store improvements.
- **Breaking:** OpenTelemetry agent setup auto-wires `OpenTelemetryChatClient`.
- Skill impact: document message injection, hosted files/RAG, access control, HITL, shell/Harness,
  and the telemetry wiring change.

### 1.6.2

- Delegates MCP content conversion to the MCP SDK.
- Aligns A2A constructors/options with `ChatClientAgent` and adds `A2AAgentOptions`.
- Adds public `FoundryChatClient`, file/vector helpers, and `ToPromptAgentAsync`.
- Adds Hosted MemoryAgent/session-state samples, Harness background/default tools/shell, and
  session export/import.
- Adds workflow edge/route tests and fixes.
- Skill impact: update MCP/A2A construction guidance and include session persistence/export.

### 1.7.0

- Adds MCP long-running-task support.
- Fixes A2A `MessageId` handling.
- **Breaking:** `AgentSkill` resource/script lookup APIs become asynchronous.
- Adds hosted Agent Skills/Foundry skills samples, Magentic samples, and a shell project.
- Skill impact: use `ListAgentToolsWithTaskSupportAsync`/`McpTaskOptions` for MCP tasks and never
  show synchronous skill resource/script lookup.

### 1.8.0

- Persists `ForeachExecutor` checkpoint state and removes the Responses experimental flag from
  `FoundryAgent`.
- Adds MCP-based `skill-md` skills.
- **Breaking:** removes declarative workflow code generation.
- Adds A2A reference task IDs/input-required handling, ClaimsIdentity session scoping, and Handoff
  parity fixes.
- **Breaking:** refactors `AgentFileSkillsSource` depth/filter configuration.
- Skill impact: remove codegen guidance, document MCP skills and task/input-required state, and
  use current file-skill options rather than old source configuration.

### 1.9.0

- Makes `Microsoft.Agents.AI.Workflows.Declarative` packages stable and removes Experimental from
  .NET orchestrations.
- Adds hosted `ToolboxMcpSkills`, AG-UI hosting/workflow fixes, and `LocalCodeAct`.
- Adds custom argument marshaling for skill scripts.
- **Breaking:** aligns FileAccess tools around directory discovery and recursive search.
- Adds workflow output tagging/filtering and fixes MCP/A2A/hosted-agent behavior.
- Skill impact: distinguish stable declarative/orchestration packages from experimental APIs,
  document LocalCodeAct, script marshaling, and the FileAccess search contract.

### 1.10.0

- Updates GitHub Copilot SDK to 1.0.0 with breaking migration changes, Microsoft.Extensions.AI to
  10.6, and MCP to 1.2.0.
- Removes required Harness tokens and makes compaction opt-in.
- Adds Reasoning handling in `ChatClientAgent` `ChatOptions` merging and improves AG-UI history/
  approval matching.
- **Breaking:** hosting fixes and `ToolApprovalAgent` auto-approval rules change behavior.
- Adds storage for auto-approved functions, `LoopAgent`, Valkey chat history, and the new skill
  script schema (resources no longer live in the script body).
- Skill impact: require explicit auto-approval rules, describe compaction as opt-in, update the
  skill-script schema, and include `LoopAgent`/Valkey only as supported variants.

### 1.11.0

- Refreshes MCP authentication headers per run.
- Skill content now exposes `available_resources` and `available_scripts`.
- Adds the A2A default `NoopAgentSessionStore` and hosted `url_citation` streaming.
- Adds GitHub Copilot tool execution events as function-call/result events.
- **Breaking:** FileAccessProvider approval follows auto-approval rules.
- Adds durable route scoping, sequential-orchestration conversation options, and hosted-tool fixes.
- Skill impact: describe progressive skill disclosure and per-run MCP auth; do not assume all
  provider tools are auto-approved.

### 1.11.1

- **Breaking/security:** all `AgentSkillsProvider` tools require approval by default.
- Adds AOT-safe `DeclarativeWorkflowJsonOptions` and fixes checkpoint upgrades.
- Removes `{resource_instructions}`/`{script_instructions}` placeholders.
- **Breaking:** archive-type skills in `AgentMcpSkillsSource` and stops skill search recursion after
  `SKILL.md`.
- Adds `IncludeDetailedErrors` for scripts and a hosted `CreateMcpTool` overload.
- Skill impact: make approval the default in the guidance, remove obsolete placeholders, and
  document archive-type MCP skills and AOT JSON options.

### 1.12.0

- Adds `BackgroundTaskCompletionLoopEvaluator` and Toolbox OAuth consent.
- **Breaking:** makes MAAI001 experimental flags explicit.
- Adds HTTPS Aspire DevUI backend and resource/script descriptions.
- **Breaking:** extracts skill-source caching into `CachingAgentSkillsSource`.
- Adds `ApprovalRequiredAIFunction` enforcement for Copilot.
- **Breaking:** Azure.AI.AgentServer 2.0 migration and Foundry.Hosting direction.
- Skill impact: use `CachingAgentSkillsSource`/builder caching options, describe OAuth/HTTPS, and
  avoid implying that all experimental APIs compile without explicit opt-in.

### 1.13.0

- Adds `AgentSkillsSourceContext` to `GetSkillsAsync` and makes skill-source classes public.
- Consolidates source caching and makes sources disposable.
- Adds skill approval options.
- **Breaking:** aligns file-editing tools and FileAccess/FileMemory store APIs.
- **Breaking:** OpenAI Hosting options mapping no longer passes options by default.
- Removes the Experimental marker from the Skills API in `Microsoft.Agents.AI`.
- Adds Foundry local identity/token credential validation.
- Skill impact: pass agent/session source context, dispose sources, use explicit skill approval
  options, and call out OpenAI Hosting options-mapping behavior.

### 1.14.0

- Stabilizes message injection, Todo/Agent Mode providers, `ToolApprovalAgent`, FileMemoryProvider,
  and HarnessAgent; adds `ToolAutoApprovalRuleContext`.
- **Breaking:** Harness FileAccess is opt-in; approval responses are bound to surfaced approvals.
- Adds approval-name collision warnings, message-order/merger fixes, and the current
  `ChatClientAgentSession` constructor.
- **Breaking/migration:** the old `Microsoft.Agents.AI.AGUI` package is removed. Use external
  `AGUI.Client`, `AGUI.Server`, `AGUI.Abstractions` (and optional Formatting/Protobuf) together
  with `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`; rename `AddAGUI` to `AddAGUIServer` and
  `MapAGUI` to `MapAGUIServer`.
- Skill impact: add Agent Mode/Todo and stable provider guidance, require explicit Harness
  FileAccess, and replace every old AG-UI package/API example.

### 1.15.0

- **Breaking:** OpenAI Responses hosting protocol helpers and optional execution state change.
- Fixes declarative auto-send, Dapr samples, and logging.
- Hardens workflow credential handling against accidental exposure.
- Skill impact: mark OpenAI Hosting as version-sensitive and include the workflow security rule.

### 1.16.0

- Fixes `InMemoryChatHistoryProvider` persistence with service-stored history.
- Adds Todo/Agent Mode and FileMemory samples.
- Makes the GitHub Copilot agent stable and lets `ToolApprovalAgent` create a session when absent.
- Adds LocalCodeAct, forwards A2A configuration, and fixes declarative table state.
- Adds stable agent IDs in checkpointed workflows and hosted-agent/source-zip samples.
- Skill impact: document session creation by approval agents, A2A option forwarding, and stable
  checkpoint IDs.

### 1.17.0

- **Packaging/ownership migration:** Durable Task and Azure Functions integrations move to the
  external <https://github.com/microsoft/agent-framework-durable-extension> repository. Package
  names remain the same, but the core repository no longer builds/publishes those projects.
- Fixes the Handoff sample's user-input path.
- Declarative workflows now fail when an agent returns an error instead of silently continuing.
- Skill impact: link to the external Durable extension repository, remove in-tree hosting claims,
  and state declarative workflow error propagation explicitly.

## Consolidated update checklist

The updated skill must reflect all of the following, with detailed package/API tables in
`packages-protocols.md`:

- Version header and package inventory target `dotnet-1.17.0`.
- Foundry-first agent creation plus `IChatClient`/OpenAI alternatives; .NET 10 recommended and
  .NET 8/9 supported.
- Session serialization/import/export, service-specific sessions, background responses, and
  continuation tokens.
- Agent/function/chat middleware signatures, including the `innerAgent` run delegate distinction.
- Tool approval as a security boundary, including the 1.11.1 default approval for Agent Skills
  tools and explicit auto-approval rules.
- Progressive Agent Skills: file, inline/class, MCP, async resource/script access, descriptions,
  caching sources, source context, script argument arrays, archive skills, and trust boundaries.
- MCP long-running tasks, per-run auth refresh, local/hosted tools, and MCP-based skills.
- `AIContextProvider`, `MessageAIContextProvider`, `ProviderSessionState`, message injection,
  Todo/Agent Mode, and chat-history persistence.
- Workflow source generators, stable declarative/orchestration status, no declarative codegen,
  checkpoint/error behavior, Magentic version sensitivity, and stable agent IDs.
- A2A sessions/input-required/background responses and AG-UI's external package/API migration.
- Durable/Azure Functions extraction, OpenTelemetry auto-wiring, HTTPS DevUI, credential safety,
  and production identity guidance.

## Post-update verification

Checked against the checklist after editing on 2026-08-07:

- PASS — `SKILL.md` targets `dotnet-1.17.0`, links this checklist, and stays under the 500-line
  skill-body guidance.
- PASS — package/protocol reference covers the current MCP, Agent Skills, approval, context,
  workflow, A2A, AG-UI, DevUI, and Durable-extension surfaces.
- PASS — examples no longer present the removed AG-UI/Azure Functions APIs as current guidance;
  legacy names remain only in explicit migration/trap notes and this historical checklist.
- PASS — `quick_validate.py csharp-best-practices/skills/agent-framework` reports `Skill is valid!`.
- PASS — both plugin manifests and both marketplace indexes parse as valid JSON.
- PASS — `git diff --check` reports no whitespace errors.

## Sources

Each release note was checked at its corresponding tag:

- [1.0.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.0.0)
- [1.1.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.1.0)
- [1.2.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.2.0)
- [1.3.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.3.0)
- [1.4.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.4.0)
- [1.5.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.5.0)
- [1.6.1](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.6.1)
- [1.6.2](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.6.2)
- [1.7.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.7.0)
- [1.8.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.8.0)
- [1.9.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.9.0)
- [1.10.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.10.0)
- [1.11.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.11.0)
- [1.11.1](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.11.1)
- [1.12.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.12.0)
- [1.13.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.13.0)
- [1.14.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.14.0)
- [1.15.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.15.0)
- [1.16.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.16.0)
- [1.17.0](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.17.0)

The upstream [Durable/Azure Functions extraction decision](https://github.com/microsoft/agent-framework/blob/dotnet-1.17.0/docs/decisions/0032-durable-azure-functions-extraction.md)
and the `dotnet-1.17.0` source tree were used to resolve package ownership and API details.
