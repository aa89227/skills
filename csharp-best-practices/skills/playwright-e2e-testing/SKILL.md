---
name: playwright-e2e-testing
description: |
  Playwright E2E testing for .NET with a standalone application host and Testcontainers
  (recommended), or Aspire when explicitly requested. Use when writing or reviewing E2E tests that
  combine Playwright browser automation, a real network-accessible application, containerized
  dependencies, and xUnit test lifecycle management. Trigger phrases: "Playwright test", "E2E test",
  "end to end test", "Playwright Testcontainers", "WebApplicationFactory Playwright",
  "standalone E2E", "Aspire Playwright", "browser test", "visual regression", "screenshot test",
  "IClassFixture Playwright", "ICollectionFixture Playwright", "DistributedApplicationTestingBuilder E2E".
license: MIT
metadata:
  author: aa89227
  version: "2.2"
  tags: ["testing", "playwright", "testcontainers", "webapplicationfactory", "aspire", "e2e", "xunit", "visual-regression"]
---

# Playwright E2E Testing with Standalone Host or Aspire + xUnit

## Instruction Retention

Read this file when this skill is first activated for the current task. Once read, retain and
follow its instructions without rereading the file on every turn or before every action.

Reread it only when the file may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

**Stack:** Microsoft.Playwright + xUnit v3 + Testcontainers

**Hosting policy:** Use a standalone application host plus Testcontainers by default. Use Aspire
only when the user explicitly requests Aspire or the requirement cannot be met by the standalone
host. Do not introduce `DistributedApplicationTestingBuilder` merely because the repository happens
to contain an Aspire AppHost.

> **xUnit v2 note:** Replace `ValueTask` with `Task` in all `IAsyncLifetime` implementations, and `[Collection<T>]` with `[Collection("name")]`.

## Hosting modes

| Mode | Selection | Application startup | Dependency isolation |
|---|---|---|---|
| **Standalone host + Testcontainers** | Default | `WebApplicationFactory<Program>` with real Kestrel on a dynamic port | Shared dependency container with isolated database/schema per worker; WireMock per worker |
| **Aspire orchestration** | Explicit opt-in | `DistributedApplicationTestingBuilder` and AppHost | Aspire-managed resources and lifecycle |

### Standalone host + Testcontainers (recommended)

```text
xUnit assembly fixture
  ├─ one PostgreSQL container
  ├─ one shared Playwright browser container
  └─ one worker per parallel test class
       ├─ isolated database
       ├─ WebApplicationFactory<Program> + real Kestrel
       └─ WireMock servers for external HTTP dependencies
```

The browser must connect to the application's real listening port. Do not use the in-memory
`TestServer` path when the browser runs in a container; expose the host port with Testcontainers
and use `host.testcontainers.internal` from the browser container. See
[standalone-host.md](references/standalone-host.md) for the complete fixture pattern.

### Aspire orchestration (opt-in)

Aspire remains supported for repositories that explicitly choose it. Keep its resource readiness,
configuration timing, and collection-fixture rules isolated to the Aspire path; do not let those
rules become requirements for standalone tests.

## Project Setup

### Standalone host + Testcontainers (recommended)

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="Microsoft.Playwright" />
<PackageReference Include="Testcontainers" />
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="xunit.v3.mtp-v2" />
<!-- Optional: external HTTP stubs, direct database seeding, and screenshot comparison. -->
<PackageReference Include="WireMock.Net" />
<PackageReference Include="Npgsql" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="SkiaSharp" />
<PackageReference Include="SkiaSharp.NativeAssets.Linux" />

<!-- Reference the application entry-point project, not an AppHost project. -->
<ProjectReference Include="..\..\src\Example.Web\Example.Web.csproj" />
```

The standalone project does not require `Aspire.Hosting.Testing` or an AppHost project. Add only
the optional packages required by the application's database, external services, and artifact
strategy.

### Aspire orchestration (opt-in)

```xml
<PackageReference Include="Aspire.Hosting.Testing" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="Microsoft.Playwright" />
<PackageReference Include="Testcontainers" />
<PackageReference Include="xunit.v3.mtp-v2" />

<!-- AppHost project reference required for DistributedApplicationTestingBuilder. -->
<ProjectReference Include="..\..\Example.AppHost\Example.AppHost.csproj" />
```

## Dependency Installation

The project setup block lists package IDs without versions intentionally. When introducing these
dependencies, resolve the latest stable releases from the online feeds at task time and use the
package CLI without `--version`:

```bash
# .NET 10+
dotnet package add Microsoft.AspNetCore.Mvc.Testing --project <path-to-test-project>
dotnet package add Microsoft.Playwright --project <path-to-test-project>
dotnet package add Testcontainers.PostgreSql --project <path-to-test-project>
dotnet package add xunit.v3.mtp-v2 --project <path-to-test-project>

# Add only when the application under test needs them.
dotnet package add WireMock.Net --project <path-to-test-project>
dotnet package add Npgsql --project <path-to-test-project>
dotnet package add Npgsql.EntityFrameworkCore.PostgreSQL --project <path-to-test-project>
dotnet package add SkiaSharp --project <path-to-test-project>
dotnet package add SkiaSharp.NativeAssets.Linux --project <path-to-test-project>

# .NET 9 and earlier: use `dotnet add <project> package <PackageId>` instead
```

Do not hand-edit a `.csproj`, add `--no-restore` to the initial package command, or copy a package
version from this skill, a sample, or memory. Let
the CLI/package manager write the resolved versions to the project or central package file. The
Playwright package, Docker image, and `npx playwright` client must use one exact compatible version:
resolve the current `Microsoft.Playwright` version first, then apply that same resolved version to
the image/client rather than copying the version in an old example.

Do not install or reference `Aspire.Hosting.Testing` for the standalone mode. The mode selection is
an architectural decision, not a package-discovery step.

For the explicitly selected Aspire mode only:

```bash
dotnet package add Aspire.Hosting.Testing --project <path-to-test-project>
```

## Standalone mode fixture contract (recommended)

Use the standalone pattern when the test should exercise the real application over HTTP without
starting an Aspire AppHost. The detailed, copy-paste-ready template is in
[standalone-host.md](references/standalone-host.md). Its required boundaries are:

- Start one shared Testcontainers database container for the suite, then create an isolated database
  or schema for each parallel worker.
- Run the application with `WebApplicationFactory<Program>` and real Kestrel on a dynamic port;
  the browser cannot reach the in-memory `TestServer` transport from a separate container.
- Inject test connection strings and external-service endpoints through host configuration before
  the application builds. Use one WireMock server per worker when external HTTP calls need stubs.
- Probe a bounded health endpoint before exposing ports and starting browser work. For interactive
  server-rendered applications, wait for the first application WebSocket/circuit signal and a stable
  DOM condition; `NetworkIdle` alone is not sufficient.
- Call `TestcontainersSettings.ExposeHostPortsAsync` for all application ports before creating the
  Playwright browser container, then use `host.testcontainers.internal` in the browser's `BaseURL`.
- Share the browser and database container at assembly scope, but create a fresh browser context per
  test and reset mutable data and stubs before each test.
- If frontend and backend are separate processes, start and health-check each one explicitly; keep
  the browser's frontend URL and the backend's test configuration explicit instead of introducing an
  AppHost solely to launch them.

## Aspire mode (opt-in): AspireFixture

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
        // Set this from the version resolved for Microsoft.Playwright by the package CLI.
        const string playwrightVersion = "<resolved-Microsoft.Playwright-version>";
        _browserContainer = new ContainerBuilder($"mcr.microsoft.com/playwright:v{playwrightVersion}-noble")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand($"npx -y playwright@{playwrightVersion} run-server --port {serverPort} --host 0.0.0.0")
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

## Aspire collection definition (opt-in)

```csharp
[CollectionDefinition(E2ECollection.Name)]
public sealed class E2ECollection : ICollectionFixture<AspireFixture>
{
    public const string Name = "E2E";
}
```

Aspire AppHost takes minutes to start — in Aspire mode, `ICollectionFixture` shares it across all
test classes in the collection. This collection pattern is not required by standalone hosting.

## Key Design Decisions

### Playwright runs in Docker, not locally

The browser runs inside `mcr.microsoft.com/playwright` container via `run-server` mode. The C# test connects over WebSocket.

Why:
- Consistent rendering across Mac, Linux, and CI — eliminates font/OS differences.
- No `playwright install` step on the host — the container has everything.
- Screenshot baselines are portable across developer machines.

Rules:
- **Resolve first, align exactly**: resolve the latest stable NuGet `Microsoft.Playwright` release
  online before adding it, then use that same resolved version for the Docker image tag and
  `npx playwright@` client. Never copy a version from this document.
- `run-server` binds to `[::1]` by default — must add `--host 0.0.0.0` for host-to-container access.

### Aspire parameter injection timing (Aspire mode only)

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

The browser runs in a container but needs to reach the application on the host. In standalone mode,
use the Kestrel address from `WebApplicationFactory`; in Aspire mode, use the address returned by
the AppHost. Then use Testcontainers' cross-engine API (works with both Docker and Podman):

```csharp
var uri = new Uri(applicationBaseUrl);
await TestcontainersSettings.ExposeHostPortsAsync(uri.Port);
var browserBaseUrl = $"http://host.testcontainers.internal:{uri.Port}";
```

- `ExposeHostPortsAsync` — makes the host port reachable from any Testcontainers-managed container.
- `host.testcontainers.internal` — portable hostname that resolves to the host across Docker and Podman.
- Do **not** use `host.docker.internal` — it is Docker-specific and fails on Podman.
- With parallel standalone workers, expose all worker ports before creating the shared browser
  container, and serialize this process-wide setup.

### Podman compatibility

When running on rootless Podman, set these environment variables:

```bash
DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
TESTCONTAINERS_RYUK_DISABLED=true
```

## Test class patterns by hosting mode

### Standalone mode: assembly infrastructure + class fixture

Use the standalone fixture from [standalone-host.md](references/standalone-host.md). The usual
shape is one xUnit assembly fixture for shared containers, one isolated worker per test class, and
`IClassFixture<E2EAppFixture>` for test classes. This avoids repeatedly starting the application and
database while keeping parallel test classes isolated.

### Aspire mode: ICollectionFixture + per-method context

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

### Aspire mode alternative: IClassFixture + IAsyncLifetime

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

- **Standalone assembly fixture + per-class worker** (default) — share the database and browser
  containers, but isolate each worker's database, application host, and external-service stubs.
- **Standalone `IClassFixture`** — use one worker per test class when the class needs its own
  application configuration or database isolation.
- **Aspire `ICollectionFixture`** — use only in Aspire mode when the AppHost is intentionally shared
  across the collection.
- **Aspire `IClassFixture`** — use only when an Aspire test class genuinely needs fixture-per-class
  isolation.

## WireMock integration (external API stubs)

For standalone mode, start one WireMock server per worker and inject its endpoint through host
configuration before the application is built. Reset the server before each test. The complete
standalone pattern is in [standalone-host.md](references/standalone-host.md).

### Aspire mode: parameter-based stubs

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
| **Standalone fixture** | Assembly fixture + `IClassFixture<E2EAppFixture>` |
| **Aspire collection fixture** | `ICollectionFixture<AspireFixture>` + `[Collection(Name)]` |
| **Start Aspire** | `DistributedApplicationTestingBuilder.CreateAsync<Projects.MyApp_AppHost>([args])` |
| **Start standalone app** | `WebApplicationFactory<Program>` + real Kestrel on a dynamic port |
| **Database isolation** | Shared PostgreSQL container + database/schema per worker |
| **External HTTP stubs** | One WireMock server per worker; inject before app build |
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

1. **Latest version alignment** — resolve the latest stable `Microsoft.Playwright` version online
   when introducing the dependency; the NuGet package, Docker image tag, and `npx playwright@`
   client must then use that same exact version.
2. **Standalone by default** — use `WebApplicationFactory<Program>` plus Testcontainers unless
   Aspire is explicitly selected.
3. **Docker browser** — run the browser in a container for rendering consistency; never use a local
   browser for E2E when the project follows this skill's default architecture.
4. **`--host 0.0.0.0`** — required for `run-server`; without it the WebSocket binds to loopback only.
5. **`ExposeHostPortsAsync` + `host.testcontainers.internal`** — use Testcontainers' cross-engine
   API for container-to-host connectivity; do not use Docker-specific `host.docker.internal`.
6. **Standalone network host** — use real Kestrel on a dynamic port when a remote browser must reach
   the application; the in-memory `TestServer` transport is not reachable from the browser container.
7. **Worker isolation** — share expensive containers, but isolate mutable database state and external
   HTTP stubs per parallel worker.
8. **Aspire params at `CreateAsync`** — in Aspire mode, all overrides must be in the args array;
   post-build configuration changes are ignored.
9. **`await using` on context** — each test method manages its own `IBrowserContext` lifecycle.
10. **Playwright assertions over xUnit assertions** — use `Expect()` for DOM checks (auto-retry);
    reserve xUnit `Assert` for non-DOM values.
11. **Fixed rendering** — always set viewport, ReducedMotion, ColorScheme, and DeviceScaleFactor
    for deterministic rendering.

## Additional Resources

### Reference Files

- **`references/visual-regression.md`** — Screenshot stability settings, SkiaSharp pixel comparison implementation, baseline management, and comparison with Playwright JS `toHaveScreenshot`. Load when adding visual regression testing.
- **`references/video-recording.md`** — Environment variable-controlled video recording, fixture/test class code changes, `SaveAsAsync` usage with Docker run-server, output structure. Load when adding test execution recording.
- **`references/standalone-host.md`** — Recommended non-Aspire fixture with WebApplicationFactory,
  Kestrel, shared Testcontainers, per-worker database isolation, WireMock, browser containers,
  readiness, authentication, and screenshot artifacts. Load for standalone E2E setup.
