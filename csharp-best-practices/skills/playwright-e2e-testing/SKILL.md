---
name: playwright-e2e-testing
description: |
  Playwright E2E testing with Aspire and xUnit. Use when writing or reviewing E2E tests that
  combine Playwright browser automation, Aspire DistributedApplicationTestingBuilder for full-stack
  orchestration, and xUnit with ICollectionFixture/IAsyncLifetime for test lifecycle.
  Trigger phrases: "Playwright test", "E2E test", "end to end test", "Aspire Playwright",
  "browser test", "Playwright Aspire xUnit", "visual regression", "screenshot test",
  "ICollectionFixture Playwright", "DistributedApplicationTestingBuilder E2E".
license: MIT
metadata:
  author: aa89227
  version: "2.0"
  tags: ["testing", "playwright", "aspire", "e2e", "xunit", "visual-regression"]
---

# Playwright E2E Testing with Aspire + xUnit

**Stack:** Microsoft.Playwright + Aspire.Hosting.Testing + xUnit v3 + Testcontainers

> **xUnit v2 note:** Replace `ValueTask` with `Task` in all `IAsyncLifetime` implementations, and `[Collection<T>]` with `[Collection("name")]`.

## Architecture Overview

```
xUnit Test Class ([Collection] + primary constructor)
  │
  └─ AspireFixture (shared across collection via ICollectionFixture)
       ├─ DistributedApplication (Aspire AppHost)
       │   └─ All resources: DB, API, Gateway, Web...
       ├─ IBrowser (Chromium via Docker run-server + WebSocket)
       ├─ WireMockServer (optional, external API stubs)
       └─ Database client (for test isolation reset)
```

## Project Setup

```xml
<PackageReference Include="Aspire.Hosting.Testing" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="Microsoft.Playwright" />
<PackageReference Include="Testcontainers" />
<PackageReference Include="xunit.v3" />
<PackageReference Include="xunit.runner.visualstudio" />

<!-- AppHost project reference required for DistributedApplicationTestingBuilder -->
<ProjectReference Include="..\..\MyApp.AppHost\MyApp.AppHost.csproj" />
```

## AspireFixture

Shared fixture that starts the entire Aspire stack and Playwright browser. Shared across all test classes in the collection via `ICollectionFixture`.

```csharp
public sealed class AspireFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IBrowser? _browser;
    private IPlaywright? _playwright;
    private IContainer? _browserContainer;

    public string WebBaseUrl { get; private set; } = string.Empty;
    public string ContainerWebBaseUrl { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // 1. Start Aspire AppHost
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>(
            [
                "--Frontend:Watch=false"
                // Override parameters as needed
            ]);

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("api")
            .WaitAsync(TimeSpan.FromMinutes(3));

        // 2. Resolve endpoints and expose host port to containers
        var baseUrl = _app.GetEndpoint("gateway", "http")!.ToString();
        WebBaseUrl = baseUrl;

        var uri = new Uri(baseUrl);
        await TestcontainersSettings.ExposeHostPortsAsync(uri.Port);
        ContainerWebBaseUrl = $"http://host.testcontainers.internal:{uri.Port}";

        // 3. Start Playwright browser in Docker container
        const int serverPort = 8080;
        _browserContainer = new ContainerBuilder("mcr.microsoft.com/playwright:v1.60.0-noble")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand($"npx -y playwright@1.60.0 run-server --port {serverPort} --host 0.0.0.0")
            .WithPortBinding(serverPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Listening on"))
            .Build();
        await _browserContainer.StartAsync();

        _playwright = await Playwright.CreateAsync();
        var wsEndpoint = $"ws://localhost:{_browserContainer.GetMappedPublicPort(serverPort)}/";
        _browser = await _playwright.Chromium.ConnectAsync(wsEndpoint);

        Assertions.SetDefaultExpectTimeout(15_000);
    }

    public async Task<IBrowserContext> NewContextAsync()
    {
        if (_browser is null) throw new InvalidOperationException("Browser not initialized");

        return await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ContainerWebBaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            ReducedMotion = ReducedMotion.Reduce,
            ColorScheme = ColorScheme.Light,
            DeviceScaleFactor = 1
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_browserContainer is not null) await _browserContainer.DisposeAsync();
        if (_app is not null) await _app.DisposeAsync();
    }
}
```

## Collection Definition

```csharp
[CollectionDefinition(E2ECollection.Name)]
public sealed class E2ECollection : ICollectionFixture<AspireFixture>
{
    public const string Name = "E2E";
}
```

Aspire AppHost takes minutes to start — `ICollectionFixture` shares it across all test classes in the collection.

## Key Design Decisions

### Playwright runs in Docker, not locally

The browser runs inside `mcr.microsoft.com/playwright` container via `run-server` mode. The C# test connects over WebSocket.

Why:
- Consistent rendering across Mac, Linux, and CI — eliminates font/OS differences.
- No `playwright install` step on the host — the container has everything.
- Screenshot baselines are portable across developer machines.

Rules:
- **Version alignment is mandatory**: NuGet `Microsoft.Playwright` version must match the Docker image tag and `npx playwright@` version exactly.
- `run-server` binds to `[::1]` by default — must add `--host 0.0.0.0` for host-to-container access.

### Aspire parameter injection timing

All configuration overrides must be passed as args to `DistributedApplicationTestingBuilder.CreateAsync<T>()`. The AppHost top-level code runs during `CreateAsync` — changing `Configuration` afterward has no effect on `AddParameter` values.

### Browser context settings for stability

Every `NewContextAsync` call must set these options to ensure deterministic rendering:

| Setting | Value | Why |
|---|---|---|
| `ViewportSize` | Fixed (e.g., 1280x720) | Consistent layout |
| `ReducedMotion` | `Reduce` | No CSS transitions/animations |
| `ColorScheme` | `Light` | Fixed appearance |
| `DeviceScaleFactor` | `1` | No HiDPI variance |

### Container-to-host connectivity

The browser runs in a container but needs to reach the Aspire app on the host. Use Testcontainers' cross-engine API (works with both Docker and Podman):

```csharp
var uri = new Uri(baseUrl);
await TestcontainersSettings.ExposeHostPortsAsync(uri.Port);
ContainerWebBaseUrl = $"http://host.testcontainers.internal:{uri.Port}";
```

- `ExposeHostPortsAsync` — makes the host port reachable from any Testcontainers-managed container.
- `host.testcontainers.internal` — portable hostname that resolves to the host across Docker and Podman.
- Do **not** use `host.docker.internal` — it is Docker-specific and fails on Podman.

### Podman compatibility

When running on rootless Podman, set these environment variables:

```bash
DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
TESTCONTAINERS_RYUK_DISABLED=true
```

## Test Class Pattern

### Primary: ICollectionFixture + per-method context

```csharp
[Collection(E2ECollection.Name)]
public sealed class MyFeatureE2ETests(AspireFixture fixture)
{
    [Fact]
    public async Task ShouldDisplayEmptyStateWhenNoData()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        // Act
        await page.GotoAsync("/items");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        await Expect(page.GetByText("No items found")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ShouldCreateItemSuccessfully()
    {
        // Arrange
        await fixture.ResetDatabaseAsync();
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/items");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await page.GetByRole(AriaRole.Link, new() { Name = "Add Item" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync("Test Item");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Assert
        await Expect(page.GetByText("Test Item")).ToBeVisibleAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator)
        => Assertions.Expect(locator);
}
```

Key points:
- `[Collection(E2ECollection.Name)]` — shares the fixture across all test classes in the collection.
- Primary constructor `(AspireFixture fixture)` — xUnit injects the shared fixture.
- `await using var context` — each test manages its own browser context lifecycle. Context (and its pages) is disposed at the end of the test.
- No `IAsyncLifetime` on the test class — per-test setup (DB reset, context creation) is explicit in each method.

### Alternative: IClassFixture + IAsyncLifetime

Use when fixture-per-class isolation is acceptable (e.g., lightweight apps or a single test class):

```csharp
public sealed class MyFeatureE2ETests : IClassFixture<AspireFixture>, IAsyncLifetime
{
    private readonly AspireFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public MyFeatureE2ETests(AspireFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _context = await _fixture.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task ShouldDisplayEmptyStateWhenNoData()
    {
        await _page.GotoAsync("/items");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(_page.GetByText("No items found")).ToBeVisibleAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator)
        => Assertions.Expect(locator);
}
```

### Lifecycle Comparison

| Scope | ICollectionFixture (primary) | IClassFixture (alternative) |
|---|---|---|
| **Fixture init** | Once per collection | Once per class |
| **Per-test setup** | Explicit in test method | `IAsyncLifetime.InitializeAsync` |
| **Per-test cleanup** | `await using` on context | `IAsyncLifetime.DisposeAsync` |
| **Fixture dispose** | After all classes in collection | After all tests in class |

### When to use which

- **ICollectionFixture** (default) — Aspire AppHost takes minutes to start. Share it across multiple test classes.
- **IClassFixture** — only when you need complete isolation between test classes, or when you have a single test class.

## WireMock Integration (External API Stubs)

When the app calls external services, use WireMock to stub them:

```csharp
// In AspireFixture
public WireMockServer WireMock { get; private set; } = null!;

// In InitializeAsync (before Aspire CreateAsync)
WireMock = WireMockServer.Start();
var wireMockDomain = WireMock.Url!.Replace("http://", "").Replace("https://", "");

// Pass to Aspire as parameter
var appHost = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.MyApp_AppHost>(
    [
        $"--Parameters:external-api-host={wireMockDomain}"
    ]);
```

Per-test stub setup (in each test method):

```csharp
[Fact]
public async Task ShouldHandleExternalApiResponse()
{
    fixture.WireMock.Reset();
    fixture.WireMock.Given(
        Request.Create().WithPath("/api/external/resource").UsingGet())
        .RespondWith(
            Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": []}"""));

    await using var context = await fixture.NewContextAsync();
    var page = await context.NewPageAsync();
    // ...
}
```

## Auth Mocking

For apps with browser-side authentication SDKs, inject mock auth via `AddInitScriptAsync` in the fixture's `NewContextAsync`:

```csharp
public async Task<IBrowserContext> NewContextAsync()
{
    var context = await _browser.NewContextAsync(new BrowserNewContextOptions { /* ... */ });

    // Block real auth SDK
    await context.RouteAsync("**/auth-sdk.js", route => route.AbortAsync());

    // Inject mock that returns test credentials
    await context.AddInitScriptAsync("""
        window.authSdk = {
            getToken: function() { return 'test-jwt-token'; },
            signout: function(redirect) { window.location.href = redirect; },
            ready: function() { return Promise.resolve(); }
        };
        """);

    return context;
}
```

## Playwright Assertion Patterns

```csharp
// Element visibility
await Expect(page.GetByText("Success")).ToBeVisibleAsync();
await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Submit" })).ToBeEnabledAsync();

// Element count
await Expect(page.GetByRole(AriaRole.Row)).ToHaveCountAsync(3);

// Text content
await Expect(page.GetByTestId("status")).ToHaveTextAsync("Active");

// Navigation
await Expect(page).ToHaveURLAsync(new Regex(@"/items/\w+"));

// Custom timeout for slow operations
await Expect(page.GetByText("Processing complete"))
    .ToBeVisibleAsync(new() { Timeout = 30_000 });
```

Use `Assertions.Expect()` (Playwright static method), not xUnit `Assert` — Playwright assertions auto-retry until timeout.

## Cheat Sheet

| Topic | Pattern |
|---|---|
| **Collection fixture** | `ICollectionFixture<AspireFixture>` + `[Collection(Name)]` |
| **Start Aspire** | `DistributedApplicationTestingBuilder.CreateAsync<Projects.MyApp_AppHost>([args])` |
| **Wait for resource** | `_app.ResourceNotifications.WaitForResourceHealthyAsync("name")` |
| **Get endpoint** | `_app.GetEndpoint("resource", "http")` |
| **Get conn string** | `await _app.GetConnectionStringAsync("db")` |
| **Expose host port** | `await TestcontainersSettings.ExposeHostPortsAsync(port)` |
| **Container base URL** | `http://host.testcontainers.internal:{port}` |
| **Playwright image** | `mcr.microsoft.com/playwright:v{version}-noble` |
| **Browser connect** | `_playwright.Chromium.ConnectAsync($"ws://localhost:{port}/")` |
| **New context** | `await using var ctx = await fixture.NewContextAsync()` |
| **New page** | `var page = await context.NewPageAsync()` |
| **Navigate** | `await page.GotoAsync("/path")` |
| **Wait for idle** | `await page.WaitForLoadStateAsync(LoadState.NetworkIdle)` |
| **Find by role** | `page.GetByRole(AriaRole.Button, new() { Name = "..." })` |
| **Find by label** | `page.GetByLabel("Field Name")` |
| **Find by text** | `page.GetByText("content")` |
| **Find by test id** | `page.GetByTestId("id")` |
| **Click** | `await locator.ClickAsync()` |
| **Fill** | `await locator.FillAsync("value")` |
| **Assert visible** | `await Expect(locator).ToBeVisibleAsync()` |
| **Block request** | `await context.RouteAsync("**/path", route => route.AbortAsync())` |
| **Inject script** | `await context.AddInitScriptAsync("...")` |
| **Mock external API** | `fixture.WireMock.Given(...).RespondWith(...)` |
| **Per-test reset** | `fixture.ResetDatabaseAsync()` + `fixture.WireMock.Reset()` |

## Rules

1. **Version alignment** — `Microsoft.Playwright` NuGet, Docker image tag, and `npx playwright@` must be the same version.
2. **Docker run-server** — always run browser in container for rendering consistency; never use local browser for E2E.
3. **`--host 0.0.0.0`** — required for `run-server`; without it the WebSocket binds to loopback only.
4. **`ExposeHostPortsAsync` + `host.testcontainers.internal`** — use Testcontainers' cross-engine API for container→host connectivity; do not use Docker-specific `host.docker.internal`.
5. **Aspire params at CreateAsync** — all overrides must be in the args array; post-build config changes are ignored.
6. **ICollectionFixture by default** — Aspire startup is expensive; share the fixture across test classes.
7. **`await using` on context** — each test method manages its own `IBrowserContext` lifecycle.
8. **Playwright assertions over xUnit assertions** — use `Expect()` for DOM checks (auto-retry); reserve xUnit `Assert` for non-DOM values.
9. **Fixed rendering** — always set viewport, ReducedMotion, ColorScheme, DeviceScaleFactor for deterministic rendering.

## Additional Resources

### Reference Files

- **`references/visual-regression.md`** — Screenshot stability settings, SkiaSharp pixel comparison implementation, baseline management, and comparison with Playwright JS `toHaveScreenshot`. Load when adding visual regression testing.
- **`references/video-recording.md`** — Environment variable-controlled video recording, fixture/test class code changes, `SaveAsAsync` usage with Docker run-server, output structure. Load when adding test execution recording.
