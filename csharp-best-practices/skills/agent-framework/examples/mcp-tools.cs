// Microsoft Agent Framework 1.0.0-rc4 — MCP Tools Integration
// Demonstrates: local MCP (stdio), hosted MCP (Azure AI Foundry),
//   MCP tool approval (human-in-the-loop)

using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using ModelContextProtocol.Client;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;

// --- Local MCP Server (stdio transport) ---

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name    = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"],
}));

var mcpTools = await mcpClient.ListToolsAsync();

AIAgent agentWithMcp = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        instructions: "You answer questions about GitHub repos.",
        tools: [.. mcpTools.Cast<AITool>()]);

// --- Hosted MCP (Azure AI Foundry, Responses/Persistent Agents API) ---

var mcpTool = new HostedMcpServerTool(
    serverName:    "microsoft_learn",
    serverAddress: "https://learn.microsoft.com/api/mcp")
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire   // auto-approve
};

var persistentAgentsClient = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());

AIAgent persistentAgent = await persistentAgentsClient.CreateAIAgentAsync(
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

// --- MCP with Tool Approval (human-in-the-loop) ---

var mcpToolWithApproval = new HostedMcpServerTool(
    serverName:    "microsoft_learn",
    serverAddress: "https://learn.microsoft.com/api/mcp")
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.AlwaysRequire  // require human approval
};

// Run — may return approval requests instead of a final answer
AgentSession session = await persistentAgent.CreateSessionAsync();
AgentResponse response = await persistentAgent.RunAsync("Search for MCP docs.", session);

// Check for pending approval requests
var approvals = response.Messages
    .SelectMany(m => m.Contents)
    .OfType<McpServerToolApprovalRequestContent>()
    .ToList();

// Approve each request and continue
List<ChatMessage> userResponses = approvals.ConvertAll(req =>
    new ChatMessage(ChatRole.User, [req.CreateResponse(approved: true)]));

response = await persistentAgent.RunAsync(userResponses, session);
Console.WriteLine(response);
