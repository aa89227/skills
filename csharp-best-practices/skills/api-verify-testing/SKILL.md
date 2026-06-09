---
name: api-verify-testing
description: |
  API integration testing with Verify snapshot testing patterns. Use when writing or reviewing
  API integration tests that use WebApplicationFactory, Testcontainers, typed HttpClient extensions,
  and Verify snapshot assertions. Trigger phrases: "API test", "integration test", "Verify snapshot",
  "snapshot test", "WebApplicationFactory test", "VerifyJson", "typed HttpClient",
  "external service test", "FakeHttpHandler", "DelegatingHandler", "fake HTTP", "mock external API",
  "xUnit test", "NUnit test".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["testing", "api", "verify", "snapshot", "integration-test", "nunit", "xunit"]
---

# API Integration Testing with Verify Snapshots

**Frameworks:** NUnit 4.x + Verify.NUnit **or** xUnit v3 + Verify.XunitV3, Microsoft.AspNetCore.Mvc.Testing, Testcontainers

> **xUnit v2 note:** Replace `ValueTask` with `Task` and `[Collection<T>]` with `[Collection("name")]`.

## General Rules

- All API tests **inherit from `ApiTestBase`** — provides shared `TestServer` and `HttpClient`.
- Per-test lifecycle automatically resets database and ID generator before each test — **do not manually clean up**.
- JWT authentication is auto-attached to every `HttpClient` — **do not manually add auth headers**.
- Use a **deterministic ID generator** (`FakeIdGenerator`) so snapshot output is reproducible across runs.
- All HTTP operations go through **typed HttpClient extensions** — never construct URLs manually in tests.
- Use **Verify snapshots** to assert API response shapes — avoid hand-written JSON assertions for complex responses.
- **Do not mock or replace internal services via DI** — API tests exercise the real application stack. Exception: external HTTP dependencies use `FakeHttpHandler` (see External Service Testing).
- **Prefer existing APIs for all data operations** — setup, action, and verification should all go through HTTP endpoints whenever possible. Only access the DI container as a last resort when no API exists for the operation.

## Infrastructure

### ApiTestBase

#### NUnit

```csharp
public abstract class ApiTestBase
{
    private static readonly Lazy<TestServer> Instance = new(() =>
        new TestServer(new InitialParameter { /* containers */ }));

    internal static TestServer Server => Instance.Value;
    protected static HttpClient Client => Server.CreateClient();

    [SetUp]
    public async Task BeforeEach()
    {
        await Server.DropDatabaseAsync();
        Server.ResetIdGenerator();
    }
}
```

Key points:
- `TestServer` is a **singleton** (`Lazy<T>`) — one container set for the entire test run.
- `Client` creates a **new `HttpClient`** per access — each test gets a fresh client.
- `[SetUp]` guarantees **full isolation**: clean database + reset deterministic IDs.

#### xUnit v3

```csharp
[CollectionDefinition]
public class ApiCollection : ICollectionFixture<TestServer>;

[Collection<ApiCollection>]
public abstract class ApiTestBase(TestServer server) : IAsyncLifetime
{
    internal TestServer Server { get; } = server;
    protected HttpClient Client => Server.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await Server.DropDatabaseAsync();
        Server.ResetIdGenerator();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

Key points:
- `ICollectionFixture<TestServer>` — singleton TestServer shared across all test classes in the collection.
- `[Collection<ApiCollection>]` — generic attribute (v3), type-safe, no magic strings. Inherited by derived classes.
- `IAsyncLifetime.InitializeAsync` — per-test reset, equivalent to NUnit's `[SetUp]`. Returns `ValueTask`.
- Primary constructor receives `TestServer` — xUnit auto-injects from collection fixture.

### TestServer (WebApplicationFactory)

```csharp
internal class TestServer(InitialParameter param) : WebApplicationFactory<Program>
{
    // Lazy-start Testcontainers in CreateHost
    // Override config via AddInMemoryCollection (connection strings, JWT, etc.)
    // Replace all ID generators with FakeIdGenerator via ConfigureServices
    // Auto-attach JWT bearer token via ConfigureClient
}
```

Key points:
- Testcontainers started lazily in `CreateHost` — no manual container management.
- Config overrides via `AddInMemoryCollection` — connection strings, JWT settings, external service URLs.
- All ID generators replaced with `Mock<IIdGenerator<T>>` backed by `FakeIdGenerator`.
- `GetRequiredService<T>()` helper to resolve services from DI for direct data manipulation.

> **xUnit note:** `ICollectionFixture<T>` requires a parameterless constructor. If TestServer needs constructor parameters, use default values or configure internally.

### FakeIdGenerator

Sequential hex IDs (`000000000000000000000001`, `000000000000000000000002`, ...) with thread-safe `Lock`. Reset before each test to ensure deterministic output.

## Typed HttpClient Extension Pattern

Every API endpoint group has a dedicated extension:

```csharp
// Extension entry point
internal static class ProductHttpClientExtension
{
    internal static ProductHttpClient Product(this HttpClient client)
        => new(client);
}

// Typed client with all API operations
internal class ProductHttpClient(HttpClient client)
{
    private const string BaseUrl = "/products";

    public Task<HttpResponseMessage> CreateProductAsync(CreateProductRequest request)
        => client.PostAsJsonAsync(BaseUrl, request);

    public Task<HttpResponseMessage> GetProductAsync(string productId)
        => client.GetAsync($"{BaseUrl}/{productId}");

    public Task<HttpResponseMessage> GetProductsByCategoryAsync(string categoryId)
        => client.GetAsync($"{BaseUrl}?categoryId={categoryId}");

    public Task<HttpResponseMessage> UpdateProductAsync(string productId, UpdateProductRequest request)
        => client.PutAsJsonAsync($"{BaseUrl}/{productId}", request);

    public Task<HttpResponseMessage> DeleteProductAsync(string productId)
        => client.DeleteAsync($"{BaseUrl}/{productId}");

    // Request DTOs as records inside the extension
    public sealed record CreateProductRequest(string CategoryId, string Name, decimal Price);
    public sealed record UpdateProductRequest(string Name, decimal Price);
}
```

Rules:
- One `XxxHttpClientExtension` static class per API group.
- One `XxxHttpClient(HttpClient)` class with primary constructor.
- `BaseUrl` as `private const string`.
- Request DTOs defined as `sealed record` inside the typed client.
- Usage: `Client.Product().CreateProductAsync(request)`.

## Verify Snapshot Testing

### Configuration

```csharp
// VerifyConfig.cs — runs once via [ModuleInitializer]
public static class VerifyConfig
{
    [ModuleInitializer]
    public static void Initialize()
    {
        DiffRunner.Disabled = true;
        Verifier.DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, "Snapshots"),
                typeName: type.Name,
                methodName: method.Name));
    }
}
```

- Snapshots stored in `Snapshots/` directory at project root.
- `DiffRunner.Disabled = true` — no interactive diff tool popup during CI.
- `[ModuleInitializer]` is a .NET attribute — works identically with NUnit and xUnit.

### Snapshot Assertion Helper

Define this private helper in each test class that uses Verify:

```csharp
private async Task VerifyJsonSnapshotAsync(string json, string snapshotName)
{
    var settings = new VerifySettings();
    settings.UseFileName(snapshotName);
    settings.UseStrictJson();
    await VerifyJson(json, settings);
}
```

- `UseFileName(snapshotName)` — explicit snapshot name, not auto-generated.
- `UseStrictJson()` — exact JSON match, no property ordering tolerance.
- Naming convention: `"FeatureName.ScenarioDescription"` (e.g., `"Product.QueryByCategory"`).
- NUnit uses `Verify.NUnit`; xUnit v3 uses `Verify.XunitV3` — the API is identical.
- xUnit does **not** need `[UsesVerify]` or `partial class`.

## Test Structure

### NUnit

```csharp
public sealed class FeatureNameAcceptanceTest : ApiTestBase
{
    [Test]
    [Description("""
    Given: Some precondition
    When: Some action
    Then: Expected result
    """)]
    public async Task ShouldReturnExpectedResultWhenConditionMet()
    {
        // Arrange
        await Given.SystemHasSomeData();

        // Act
        var response = await When.PerformSomeAction();

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        await VerifyJsonSnapshotAsync(json, "FeatureName.ScenarioDescription");
    }

    private async Task VerifyJsonSnapshotAsync(string json, string snapshotName) { /* ... */ }
}

file static class Given
{
    public static async Task SystemHasSomeData() { /* setup via API */ }
}

file static class When
{
    public static async Task<HttpResponseMessage> PerformSomeAction() { /* HTTP call */ }
}
```

Rules:
- Test class: `sealed`, inherits `ApiTestBase`.
- `[Test]` marks test methods.
- `[Description]` carries BDD scenario in Given/When/Then format.
- `file static class Given / When / Then` — file-scoped, not visible outside the file.
- Simple scenarios can use `private` helper methods instead of Given/When/Then classes.

### xUnit v3

```csharp
public sealed class FeatureNameAcceptanceTest(TestServer server) : ApiTestBase(server)
{
    [Fact]
    public async Task ShouldReturnExpectedResultWhenConditionMet()
    {
        // Arrange
        await Given.SystemHasSomeData();

        // Act
        var response = await When.PerformSomeAction();

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        await VerifyJsonSnapshotAsync(json, "FeatureName.ScenarioDescription");
    }

    private async Task VerifyJsonSnapshotAsync(string json, string snapshotName) { /* ... */ }
}

file static class Given
{
    public static async Task SystemHasSomeData() { /* setup via API */ }
}

file static class When
{
    public static async Task<HttpResponseMessage> PerformSomeAction() { /* HTTP call */ }
}
```

Rules:
- Test class: `sealed`, inherits `ApiTestBase(server)` with primary constructor.
- `[Fact]` marks test methods; use `[Theory]` for parameterized tests.
- No `[Description]` equivalent — use descriptive method names for BDD intent.
- `file static class Given / When / Then` — identical to NUnit.
- `[Collection<ApiCollection>]` inherited from `ApiTestBase` — no need to repeat.

### TestHelper (shared helpers)

```csharp
internal static class TestHelper
{
    // Extension methods on TestServer for cross-test reuse
    public static async Task<string> CreateSomeResource(this TestServer server, string name = "default")
    {
        var response = await server.CreateClient().Xxx().CreateXxxAsync(new(...));
        return (await response.Content.ReadFromJsonAsync<string>())!;
    }
}
```

- Defined as extension methods on `TestServer`.
- Used for common setup operations shared across multiple test files.

## Data Preparation Priority

1. **Via API** (strongly preferred): `Client.Xxx().CreateXxxAsync(request)` — tests the full stack, no mocking, no DI manipulation.
2. **Via test files**: `TestHelper.PrepareUploadFile(fileName)` for file uploads from `TestFiles/` directory.
3. **Via DI container** (last resort only — when absolutely no API exists for the operation):
   ```csharp
   var service = Server.GetRequiredService<IXxxService>();
   // or access database context directly
   ```
   Mark such usage with a `// FIXME: no API available` comment so it can be replaced when an API is added.

## External Service Testing

When the API under test calls external services via `IHttpClientFactory`, use `DelegatingHandler` to intercept outgoing requests — verify request body/headers and return fake responses without hitting real services.

### FakeHttpHandler

Reusable handler that captures every outgoing request and returns queued responses:

```csharp
internal sealed class FakeHttpHandler : DelegatingHandler
{
    public List<CapturedRequest> Requests { get; } = [];
    private readonly Queue<HttpResponseMessage> _responses = new();

    public void EnqueueResponse(HttpStatusCode status = HttpStatusCode.OK)
        => _responses.Enqueue(new HttpResponseMessage(status));

    public void EnqueueJsonResponse<T>(T body, HttpStatusCode status = HttpStatusCode.OK)
        => _responses.Enqueue(new HttpResponseMessage(status) { Content = JsonContent.Create(body) });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is not null
            ? await request.Content.ReadAsStringAsync(ct)
            : null;

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri!,
            body,
            request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))));

        return _responses.TryDequeue(out var response)
            ? response
            : new HttpResponseMessage(HttpStatusCode.OK);
    }

    public void Reset()
    {
        Requests.Clear();
        _responses.Clear();
    }

    public sealed record CapturedRequest(
        HttpMethod Method, Uri RequestUri, string? Body, Dictionary<string, string> Headers);
}
```

Key points:
- One `FakeHttpHandler` instance per external service — each service gets its own handler.
- `EnqueueJsonResponse<T>` / `EnqueueResponse` — queue responses **before** the test action.
- `Requests` — inspect captured requests **after** the test action.
- `Reset()` — called in per-test lifecycle to clear state between tests.
- Default response is `200 OK` when no response is queued.

### Registration in TestServer

```csharp
internal class TestServer(InitialParameter param) : WebApplicationFactory<Program>
{
    internal FakeHttpHandler SmsHandler { get; } = new();
    internal FakeHttpHandler PaymentHandler { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient("SmsService")
                .AddHttpMessageHandler(() => SmsHandler);
            services.AddHttpClient("PaymentService")
                .AddHttpMessageHandler(() => PaymentHandler);
        });
    }

    public void ResetFakeHandlers()
    {
        SmsHandler.Reset();
        PaymentHandler.Reset();
    }
}
```

Key points:
- Handler instances are **properties on TestServer** — tests access via `Server.SmsHandler`.
- `AddHttpMessageHandler(() => handler)` — factory delegate, not DI-resolved, so handler lifetime is managed by TestServer.
- Named client names must match production registration (e.g., `"SmsService"`).
- `ResetFakeHandlers()` — called from per-test lifecycle.

### Per-test Reset

#### NUnit

```csharp
[SetUp]
public async Task BeforeEach()
{
    await Server.DropDatabaseAsync();
    Server.ResetIdGenerator();
    Server.ResetFakeHandlers();
}
```

#### xUnit v3

```csharp
public async ValueTask InitializeAsync()
{
    await Server.DropDatabaseAsync();
    Server.ResetIdGenerator();
    Server.ResetFakeHandlers();
}
```

### Asserting Captured Requests

**Verify request body with snapshot:**

```csharp
var captured = Server.SmsHandler.Requests[0];
await VerifyJsonSnapshotAsync(captured.Body!, "Sms.SendNotificationRequest");
```

**Check specific headers:**

#### NUnit

```csharp
var captured = Server.PaymentHandler.Requests[0];
Assert.That(captured.Headers, Does.ContainKey("X-Api-Key"));
Assert.That(captured.Headers["X-Api-Key"], Is.EqualTo("expected-key"));
```

#### xUnit

```csharp
var captured = Server.PaymentHandler.Requests[0];
Assert.Contains("X-Api-Key", captured.Headers);
Assert.Equal("expected-key", captured.Headers["X-Api-Key"]);
```

**Snapshot the entire captured request:**

```csharp
var captured = Server.SmsHandler.Requests[0];
var settings = new VerifySettings();
settings.UseFileName("Sms.CapturedRequest");
await Verify(new
{
    captured.Method,
    PathAndQuery = captured.RequestUri.PathAndQuery,
    captured.Body,
    captured.Headers
}, settings);
```

**Verify no request was sent:**

#### NUnit

```csharp
Assert.That(Server.SmsHandler.Requests, Is.Empty);
```

#### xUnit

```csharp
Assert.Empty(Server.SmsHandler.Requests);
```

## Cheat Sheet

### Common (Framework-agnostic)

| Topic | Pattern |
|---|---|
| **HTTP call** | `Client.Xxx().MethodAsync(request)` |
| **Verify JSON** | `await VerifyJsonSnapshotAsync(json, "Feature.Scenario")` |
| **Verify object** | `await Verify(object)` |
| **Snapshot name** | `"FeatureName.ScenarioDescription"` |
| **Test method name** | `ShouldYWhenX()` |
| **Shared setup** | `await Server.CreateSomeResource()` (TestHelper extension) |
| **DI access** | `Server.GetRequiredService<T>()` |
| **File upload** | `TestHelper.PrepareUploadFile("fileName")` |
| **Error assertion** | `await response.Content.GetProblemDetail()` |
| **Fake external svc** | `Server.SmsHandler.EnqueueJsonResponse(body)` |
| **Capture request** | `Server.SmsHandler.Requests[0].Body` |

### NUnit-specific

| Topic | Pattern |
|---|---|
| **Base class** | `class MyTest : ApiTestBase` |
| **Test attribute** | `[Test]` |
| **BDD description** | `[Description("""Given: … When: … Then: …""")]` |
| **Per-test reset** | `[SetUp] public async Task BeforeEach()` |
| **Assert equal** | `Assert.That(actual, Is.EqualTo(expected))` |
| **Assert contains key** | `Assert.That(dict, Does.ContainKey(key))` |
| **Assert empty** | `Assert.That(list, Is.Empty)` |
| **Verify package** | `Verify.NUnit` |

### xUnit v3-specific

| Topic | Pattern |
|---|---|
| **Base class** | `class MyTest(TestServer s) : ApiTestBase(s)` |
| **Test attribute** | `[Fact]` / `[Theory]` |
| **BDD description** | Descriptive method naming (no attribute) |
| **Per-test reset** | `IAsyncLifetime.InitializeAsync()` → `ValueTask` |
| **Collection fixture** | `[CollectionDefinition]` + `ICollectionFixture<TestServer>` |
| **Collection ref** | `[Collection<ApiCollection>]` (generic, type-safe) |
| **Assert equal** | `Assert.Equal(expected, actual)` |
| **Assert contains key** | `Assert.Contains(key, dict)` |
| **Assert empty** | `Assert.Empty(list)` |
| **Verify package** | `Verify.XunitV3` |

## Best Practices

1. **One test = one scenario** — each test method tests one specific behavior.
2. **Prefer API for data setup** — only bypass when the API doesn't support the operation.
3. **Descriptive snapshot names** — use `"FeatureName.ScenarioDescription"` format for easy identification.
4. **Don't assert intermediate steps** — focus on the final response shape via Verify.
5. **Keep Given/When/Then helpers focused** — each helper does one thing.
6. **Use deterministic data** — rely on `FakeIdGenerator` and fixed test values for reproducible snapshots.
7. **Use domain language for naming** — test methods and helpers should clearly describe the scenario.

## Additional Resources

### Example Files

Complete `.cs` examples in `examples/`:
- **`examples/complete-api-test.cs`** — NUnit: typed HttpClient extension, TestHelper, and test class with Verify snapshots
- **`examples/complete-api-test-xunit.cs`** — xUnit v3: same example adapted with `ICollectionFixture` and `IAsyncLifetime`
- **`examples/external-service-test.cs`** — NUnit: FakeHttpHandler setup, TestServer registration, and request body/header assertion
- **`examples/external-service-test-xunit.cs`** — xUnit v3: same external service tests adapted for xUnit

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for verifying API integration test compliance
