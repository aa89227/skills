# API Verify Testing — Reviewer Checklist

When reviewing API integration test code, you **must** use the Todo tool or create a checklist file to track each item and ensure every check is completed.

## Checklist

### Infrastructure
- [ ] Test class inherits from `ApiTestBase`
- [ ] No manual auth header handling (auto-attached by TestServer)
- [ ] No manual Testcontainers lifecycle management

### HTTP Operations
- [ ] All HTTP operations go through typed HttpClient extensions (e.g., `Client.Xxx().MethodAsync()`)
- [ ] No manual URL string concatenation
- [ ] New API groups have a corresponding extension class
- [ ] Extension class follows pattern: static extension + `XxxHttpClient(HttpClient)` primary constructor
- [ ] Request DTOs defined as `sealed record` inside the typed client

### Verify Snapshots
- [ ] Snapshot name uses `"FeatureName.ScenarioDescription"` naming format
- [ ] Uses `UseStrictJson()` for strict JSON comparison
- [ ] Uses `UseFileName(snapshotName)` to explicitly specify snapshot file name

### Data Preparation
- [ ] Prefer creating test data via API calls
- [ ] When bypassing API, use `Server.GetRequiredService<T>()` to resolve services from DI container
- [ ] Shared setup helpers are placed in `TestHelper` as extension methods on `TestServer`
- [ ] No duplicated setup logic that could be extracted into shared helpers

### External Service Testing
- [ ] External HTTP dependencies use `FakeHttpHandler` — not mocked via DI service replacement
- [ ] Each external service has its own `FakeHttpHandler` instance on `TestServer`
- [ ] Handler registered via `AddHttpMessageHandler(() => handler)` — factory delegate, not DI-resolved
- [ ] Named client names match production registration
- [ ] `ResetFakeHandlers()` called in per-test lifecycle
- [ ] Responses queued **before** the test action (`EnqueueJsonResponse` / `EnqueueResponse`)
- [ ] Request assertions done **after** the test action via `handler.Requests`
- [ ] Error scenarios tested (e.g., external service returns 503)
- [ ] "No call" scenarios verified with empty collection assertion

### xUnit v3-specific (skip if using NUnit)
- [ ] Uses `xunit.v3` package (not legacy `xunit` v2)
- [ ] Uses `Verify.XunitV3` package (not deprecated `Verify.Xunit`)
- [ ] `[CollectionDefinition]` exists with `ICollectionFixture<TestServer>`
- [ ] Test class inherits `ApiTestBase(server)` with primary constructor
- [ ] `IAsyncLifetime` methods return `ValueTask` (not `Task`)
- [ ] `[Fact]` for individual tests, `[Theory]` for parameterized
- [ ] Assert uses xUnit API (`Assert.Equal`, `Assert.Contains`, `Assert.Empty`)
- [ ] No `[UsesVerify]` or `partial class` (not needed for xUnit)
