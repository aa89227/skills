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
