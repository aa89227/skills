// Microsoft Agent Framework 1.0.0-rc4 — Hello Agent & Providers
// Demonstrates: minimal setup (Azure OpenAI), streaming, OpenAI (non-Azure),
//   Chat Completion vs Responses client, function tools

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ComponentModel;

// --- Minimal Hello Agent (Azure OpenAI) ---

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;

AIAgent agent = new AzureOpenAIClient(
        new Uri(endpoint),
        new DefaultAzureCredential())   // production: use ManagedIdentityCredential
    .GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");

// Non-streaming
Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

// Streaming
await foreach (var update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
{
    Console.Write(update);
}

// --- Providers ---

var client = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

// Chat Completion client — broad model support, simple agents
AIAgent chatAgent = client.GetChatClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are a helpful assistant.", name: "ChatBot");

// Responses client — richer tools (code interpreter, file search, hosted MCP)
AIAgent responsesAgent = client.GetResponseClient("gpt-4o-mini")
    .AsAIAgent(instructions: "You are a helpful assistant.", name: "ResponseBot");

// OpenAI (non-Azure)
AIAgent openAiAgent = new OpenAIClient("<apikey>")
    .GetResponsesClient("gpt-4o-mini")
    .AsAIAgent(name: "HaikuBot", instructions: "You write beautifully.");

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
