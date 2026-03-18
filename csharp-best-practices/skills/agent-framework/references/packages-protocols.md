# Microsoft Agent Framework — Packages & Protocols Reference (1.0.0-rc4)

## NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.Agents.AI` | Core: `AIAgent`, builder, logging, OpenTelemetry |
| `Microsoft.Agents.AI.Abstractions` | Base abstractions: `AIAgent`, `AgentSession`, `AIContextProvider` |
| `Microsoft.Agents.AI.OpenAI` | Azure OpenAI / OpenAI provider (Chat, Responses, Assistants) |
| `Microsoft.Agents.AI.Anthropic` | Anthropic Claude provider |
| `Microsoft.Agents.AI.Workflows` | Graph-based workflow engine |
| `Microsoft.Agents.AI.Workflows.Generators` | Source generators for `[MessageHandler]` (AOT) |
| `Microsoft.Agents.AI.A2A` | Agent-to-Agent protocol support |
| `Microsoft.Agents.AI.Hosting` | ASP.NET Core hosting |
| `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` | A2A protocol hosting |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` | AG-UI protocol hosting |
| `Microsoft.Agents.AI.DevUI` | Interactive developer UI for debugging |
| `Microsoft.Agents.AI.Declarative` | Declarative agent definitions |
| `Microsoft.Agents.AI.AzureAI.Persistent` | Azure AI Foundry persistent agents |
| `Microsoft.Agents.AI.FoundryMemory` | Azure AI Foundry memory integration |
| `Microsoft.Agents.AI.CosmosNoSql` | Cosmos DB NoSQL context provider |
| `Microsoft.Agents.AI.DurableTask` | Long-running workflows with Azure Durable Functions |

## Provider Client Types

| Client Type | API | Best For | Create Method |
|---|---|---|---|
| **Chat Completion** | Chat Completions | Simple agents, broad model support | `client.GetChatClient(model)` |
| **Responses** | Responses API | Full-featured: code interpreter, file search, hosted MCP | `client.GetResponseClient(model)` |
| **Assistants** | Assistants API | Server-managed agents with persistent state | `client.GetAssistantClient(model)` |

## Tool Support Matrix

| Tool Type | Chat Completion | Responses | Assistants | Foundry | Anthropic | Ollama |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Function Tools | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tool Approval | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Code Interpreter | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| File Search | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Web Search | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Hosted MCP | ❌ | ✅ | ❌ | ✅ | ✅ | ❌ |
| Local MCP | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## Protocols & Hosting

| Protocol | Package | Description |
|---|---|---|
| **A2A** | `Microsoft.Agents.AI.A2A` | Agent-to-Agent — invoke remote agents as local `AIAgent` |
| **AG-UI** | `Microsoft.Agents.AI.AGUI` | Agent-to-UI streaming protocol |
| **ASP.NET Core** | `Microsoft.Agents.AI.Hosting` | Host agents as web endpoints |
| **Azure Functions** | `Microsoft.Agents.AI.Hosting.AzureFunctions` | Serverless agent hosting (Durable Agents) |
| **Durable Tasks** | `Microsoft.Agents.AI.DurableTask` | Long-running workflows with Azure Durable Functions |

## Workflow Concepts

| Concept | Description |
|---|---|
| **Executor** | Processing unit — custom logic or AI agent |
| **Edge** | Connection between executors (data flow path) |
| **WorkflowBuilder** | Builds the directed graph of executors + edges |
| **InProcessExecution** | Runs the workflow in-process |
| **Run** | Represents a workflow execution with events |
| **Checkpointing** | Save/resume workflow state for long-running processes |
| **`[MessageHandler]`** | Source generator attribute for `partial` executor classes (AOT compatible) |
