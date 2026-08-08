// Microsoft Agent Framework 1.17.0 — Sessions & AIContextProvider
// Demonstrates: AgentSession multi-turn, session serialization,
//   background continuation, AIContextProvider implementation

using Microsoft.Agents.AI;

// --- Multi-turn Conversations ---

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate.", session));
Console.WriteLine(await agent.RunAsync("Now tell it in a parrot's voice.", session));
// Agent remembers the previous joke from the session context

// --- Session Serialization (persistence) ---

// Serialize to JSON (store to DB / cache)
System.Text.Json.JsonElement serialized = await agent.SerializeSessionAsync(session);

// Deserialize and resume
AgentSession resumed = await agent.DeserializeSessionAsync(serialized);
Console.WriteLine(await agent.RunAsync("What were we talking about?", resumed));

// --- Background response continuation ---

AgentResponse background = await agent.RunAsync(
    "Start the long-running report.",
    resumed,
    new AgentRunOptions { AllowBackgroundResponses = true });

if (background.ContinuationToken is not null)
{
    // Persist the token, then poll later. The token can be serialized with the framework's
    // AgentAbstractions JSON metadata when it crosses a process boundary.
    AgentResponse completed = await agent.RunAsync(
        resumed,
        new AgentRunOptions { ContinuationToken = background.ContinuationToken });
}

// --- AIContextProvider: custom memory that injects context ---
// Register via ChatClientAgentOptions.AIContextProviders

AIAgent agentWithMemory = chatClient.AsAIAgent(new ChatClientAgentOptions()
{
    ChatOptions = new() { Instructions = "You are a friendly assistant." },
    AIContextProviders = [new SimpleMemoryProvider()]
});

// For message-only enrichment, derive from MessageAIContextProvider and override
// ProvideMessagesAsync instead of implementing durable provider state.

// --- AIContextProvider implementation ---

internal sealed class SimpleMemoryProvider : AIContextProvider
{
    private readonly ProviderSessionState<MyState> _sessionState;
    private IReadOnlyList<string>? _stateKeys;

    public SimpleMemoryProvider()
    {
        _sessionState = new ProviderSessionState<MyState>(
            _ => new MyState(), this.GetType().Name);
    }

    public override IReadOnlyList<string> StateKeys => _stateKeys ??= [_sessionState.StateKey];

    // Called BEFORE LLM call — inject context into prompt
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = $"User's name is {state.UserName}."
        });
    }

    // Called AFTER LLM call — extract and persist state
    protected override async ValueTask StoreAIContextAsync(
        InvokedContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        // Extract info from context.RequestMessages and update state
        _sessionState.SaveState(context.Session, state);
    }

    // Advanced override points:
    // InvokingCoreAsync — full control over request messages, tools, instructions
    // InvokedCoreAsync  — full control over response processing
}

file sealed class MyState
{
    public string UserName { get; set; } = "Unknown";
}
