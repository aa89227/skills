---
name: api-verify-testing
description: |
  API integration testing with Verify snapshot testing patterns. Use when writing or reviewing
  API integration tests that use WebApplicationFactory, Testcontainers, typed HttpClient extensions,
  and Verify snapshot assertions. Trigger phrases: "API test", "integration test", "Verify snapshot",
  "snapshot test", "WebApplicationFactory test", "VerifyJson", "typed HttpClient".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["testing", "api", "verify", "snapshot", "integration-test", "nunit"]
---

# API Integration Testing with Verify Snapshots

**Frameworks:** NUnit 4.x, Verify.NUnit, Microsoft.AspNetCore.Mvc.Testing, Testcontainers

## General Rules

- All API tests **inherit from `ApiTestBase`** — provides singleton `TestServer` and `HttpClient`.
- `[SetUp]` automatically resets database and ID generator before each test — **do not manually clean up**.
- JWT authentication is auto-attached to every `HttpClient` — **do not manually add auth headers**.
- Use a **deterministic ID generator** (`FakeIdGenerator`) so snapshot output is reproducible across runs.
- All HTTP operations go through **typed HttpClient extensions** — never construct URLs manually in tests.
- Use **Verify snapshots** to assert API response shapes — avoid hand-written JSON assertions for complex responses.
- **Do not mock or replace services via DI** — API tests should exercise the real application stack as-is.
- **Prefer existing APIs for all data operations** — setup, action, and verification should all go through HTTP endpoints whenever possible. Only access the DI container as a last resort when no API exists for the operation.

## Infrastructure

### ApiTestBase

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
- `BeforeEach` guarantees **full isolation**: clean database + reset deterministic IDs.

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

## Test Structure

### BDD-style with file-scoped static classes

```csharp
public sealed class FeatureNameAcceptanceTest : ApiTestBase
{
    [Test]
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
- Test methods: scenario description as method name — no suffix needed.
- `file static class Given / When / Then` — file-scoped, not visible outside the file.
- Simple scenarios can use `private` helper methods instead of Given/When/Then classes.

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

## Cheat Sheet

| Topic | Pattern |
|---|---|
| **Base class** | `class MyTest : ApiTestBase` |
| **HTTP call** | `Client.Xxx().MethodAsync(request)` |
| **Verify JSON** | `await VerifyJsonSnapshotAsync(json, "Feature.Scenario")` |
| **Verify object** | `await Verifier.Verify(object)` |
| **Snapshot name** | `"FeatureName.ScenarioDescription"` |
| **Test method name** | `ShouldYWhenX()` |
| **Shared setup** | `await Server.CreateSomeResource()` (TestHelper extension) |
| **DI access** | `Server.GetRequiredService<T>()` |
| **File upload** | `TestHelper.PrepareUploadFile("fileName")` |
| **Error assertion** | `await response.Content.GetProblemDetail()` |

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
- **`examples/complete-api-test.cs`** — End-to-end example showing typed HttpClient extension, TestHelper, and test class with Verify snapshots working together

### Reference Files

- **`references/reviewer-checklist.md`** — Reviewer checklist for verifying API integration test compliance
