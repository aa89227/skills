// Microsoft Agent Framework 1.0.0 — MCP Tools Integration
// Demonstrates: local MCP (stdio), hosted MCP (Responses API),
//   MCP tool approval (human-in-the-loop)

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI.Responses;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;
var deploymentName = "gpt-4o-mini";

// --- Local MCP Server (stdio transport) ---

await using var mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name    = "MCPServer",
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-github"],
}));

var mcpTools = await mcpClient.ListToolsAsync();

AIAgent agentWithMcp = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(
        instructions: "You answer questions about GitHub repos.",
        tools: [.. mcpTools.Cast<AITool>()]);

// --- Hosted MCP (Responses API — server-side MCP execution) ---
// Requires: GetResponsesClient() (not GetChatClient)

var mcpTool = new HostedMcpServerTool(
    serverName:    "microsoft_learn",
    serverAddress: "https://learn.microsoft.com/api/mcp")
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire   // auto-approve
};

AIAgent responsesAgent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetResponsesClient()
    .AsAIAgent(
        model: deploymentName,
        instructions: "Search Microsoft Learn only.",
        name: "LearnAgent",
        tools: [mcpTool]);

// --- MCP with Tool Approval (human-in-the-loop) ---

var mcpToolWithApproval = new HostedMcpServerTool(
    serverName:    "microsoft_learn",
    serverAddress: "https://learn.microsoft.com/api/mcp")
{
    AllowedTools = ["microsoft_docs_search"],
    ApprovalMode = HostedMcpServerToolApprovalMode.AlwaysRequire  // require human approval
};

// Run — may return approval requests instead of a final answer
AgentSession session = await responsesAgent.CreateSessionAsync();
AgentResponse response = await responsesAgent.RunAsync("Search for MCP docs.", session);

// Check for pending approval requests (ToolApprovalRequestContent, not McpServerToolApprovalRequestContent)
var approvals = response.Messages
    .SelectMany(m => m.Contents)
    .OfType<ToolApprovalRequestContent>()
    .ToList();

// Approve each request — cast ToolCall to McpServerToolCallContent for MCP details
List<ChatMessage> userResponses = approvals.ConvertAll(req =>
{
    McpServerToolCallContent mcpToolCall = (McpServerToolCallContent)req.ToolCall!;
    Console.WriteLine($"Approving: {mcpToolCall.ServerName}/{mcpToolCall.Name}");
    return new ChatMessage(ChatRole.User, [req.CreateResponse(approved: true)]);
});

response = await responsesAgent.RunAsync(userResponses, session);
Console.WriteLine(response);
