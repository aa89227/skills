// Microsoft Agent Framework 1.0.0-rc4 — Middleware
// Demonstrates: agent run middleware, function calling middleware,
//   IChatClient middleware, AIContextProvider as middleware

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// --- Register middleware via agent builder ---
//
// Agent run middleware (2nd arg null = non-streaming only):
//   .Use(MiddlewareFunc, streamingMiddlewareFunc)
//   pass null for streaming arg to skip streaming support

var middlewareAgent = originalAgent
    .AsBuilder()
    .Use(FunctionCallMiddleware)              // function calling middleware
    .Use(PIIMiddleware, null)                 // agent run middleware (non-streaming)
    .Use(GuardrailMiddleware, null)           // chained agent run middleware
    .Build();

// Chat client-level middleware (wraps LLM call itself)
var agentFromChatClient = azureOpenAIClient.AsIChatClient()
    .AsBuilder()
    .Use(getResponseFunc: ChatClientMiddleware, getStreamingResponseFunc: null)
    .BuildAIAgent(
        instructions: "You are a helpful assistant.",
        tools: [AIFunctionFactory.Create(GetDateTime)]);

// AIContextProvider as middleware (context injection pattern)
var contextAgent = originalAgent
    .AsBuilder()
    .UseAIContextProviders(new DateTimeContextProvider())
    .Build();

// Chat client-level context provider
var agentWithContextProvider = azureOpenAIClient.AsIChatClient()
    .AsBuilder()
    .UseAIContextProviders(new DateTimeContextProvider())
    .BuildAIAgent(instructions: "You are a helpful assistant.");

// --- Agent Run Middleware ---
// Receives innerAgent; calls innerAgent.RunAsync() to continue the chain.
// NOTE: uses innerAgent pattern (NOT a next delegate).

async Task<AgentResponse> PIIMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentSession? session,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
{
    // Inspect / modify input
    var filteredMessages = FilterPii(messages);

    // Proceed to next agent in the chain
    var response = await innerAgent.RunAsync(filteredMessages, session, options, cancellationToken);

    // Inspect / modify output
    response.Messages = FilterPii(response.Messages);
    return response;
}

// --- Function Calling Middleware ---
// Intercepts individual tool calls; set context.Terminate = true to stop the loop.

async ValueTask<object?> FunctionCallMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Calling function: {context.Function.Name}");
    var result = await next(context, cancellationToken);
    Console.WriteLine($"Function result: {result}");
    // context.Terminate = true;  // stop further function calls if needed
    return result;
}

// --- IChatClient Middleware ---
// Wraps the underlying LLM request/response.

async Task<ChatResponse> ChatClientMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerClient,
    CancellationToken cancellationToken)
{
    // Inspect / modify before LLM call
    var response = await innerClient.GetResponseAsync(messages, options, cancellationToken);
    // Inspect / modify after LLM call
    return response;
}

// Stubs
static IEnumerable<ChatMessage> FilterPii(IEnumerable<ChatMessage> msgs) => msgs;
static List<ChatMessage> FilterPii(List<ChatMessage> msgs) => msgs;
static string GetDateTime() => DateTime.UtcNow.ToString("o");
