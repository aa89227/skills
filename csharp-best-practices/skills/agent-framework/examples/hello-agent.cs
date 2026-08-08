// Microsoft Agent Framework 1.17.0 — Hello Agent & Providers
// Demonstrates: Foundry-first setup, streaming, OpenAI/Azure OpenAI,
//   Chat Completion vs Responses client, function tools

using Azure.AI.Projects;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;

// --- Minimal Hello Agent (Azure AI Foundry project) ---

var projectEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")!;
var model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5.4-mini";

AIAgent agent = new AIProjectClient(
        new Uri(projectEndpoint),
        new DefaultAzureCredential())   // production: use ManagedIdentityCredential
    .AsAIAgent(
        model: model,
        instructions: "You are good at telling jokes.",
        name: "Joker");

// Non-streaming
Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

// Streaming
await foreach (var update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
{
    Console.Write(update.Text);
}

// --- Provider alternatives ---

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;

var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

// Chat Completion client — broad model support, simple agents
AIAgent chatAgent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are a helpful assistant.", name: "ChatBot");

// Responses client — richer tools (code interpreter, file search, hosted MCP)
// Note: GetResponsesClient() without model; pass model via AsAIAgent(model:)
AIAgent responsesAgent = client.GetResponsesClient()
    .AsAIAgent(model: "gpt-4o-mini", instructions: "You are a helpful assistant.", name: "ResponseBot");

// OpenAI (non-Azure) — model is required via AsAIAgent(model:)
AIAgent openAiAgent = new OpenAIClient("<apikey>")
    .GetResponsesClient()
    .AsAIAgent(model: "gpt-4o-mini", name: "HaikuBot", instructions: "You write beautifully.");

// --- Function Tools ---

[Description("Get the weather for a given location.")]
static string GetWeather(
    [Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";

AIAgent agentWithTools = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(
        instructions: "You are a helpful assistant",
        tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await agentWithTools.RunAsync("What is the weather like in Amsterdam?"));

// Multiple tools: tools: [AIFunctionFactory.Create(Func1), AIFunctionFactory.Create(Func2)]
