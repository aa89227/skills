---
name: playwright-e2e-testing
description: |
  Playwright E2E testing with Aspire and xUnit. Use when writing or reviewing E2E tests that
  combine Playwright browser automation, Aspire DistributedApplicationTestingBuilder for full-stack
  orchestration, and xUnit with IClassFixture/IAsyncLifetime for test lifecycle.
  Trigger phrases: "Playwright test", "E2E test", "end to end test", "Aspire Playwright",
  "browser test", "Playwright Aspire xUnit", "visual regression", "screenshot test",
  "IClassFixture Playwright", "DistributedApplicationTestingBuilder E2E".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["testing", "playwright", "aspire", "e2e", "xunit", "visual-regression"]
---

# Playwright E2E Testing with Aspire + xUnit

**Stack:** Microsoft.Playwright + Aspire.Hosting.Testing + xUnit + Testcontainers

## Architecture Overview

```
xUnit Test Class (IClassFixture<AspireFixture> + IAsyncLifetime)
  │
  └─ AspireFixture (shared, once per test class)
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
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />

<!-- AppHost project reference required for DistributedApplicationTestingBuilder -->
<ProjectReference Include="..\..\MyApp.AppHost\MyApp.AppHost.csproj" />
```

## AspireFixture

Shared fixture that starts the entire Aspire stack and Playwright browser once per test class.

```csharp
public sealed class AspireFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IBrowser? _browser;
    private IPlaywright? _playwright;
    private IContainer? _browserContainer;

    public string WebBaseUrl { get; private set; } = string.Empty;
    public string ContainerWebBaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
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

        // 2. Resolve endpoints
        var baseUrl = _app.GetEndpoint("gateway", "http")!.ToString();
        WebBaseUrl = baseUrl;
        ContainerWebBaseUrl = baseUrl
            .Replace("localhost", "host.docker.internal")
            .Replace("127.0.0.1", "host.docker.internal");

        // 3. Start Playwright browser in Docker container
        const int serverPort = 8080;
        _browserContainer = new ContainerBuilder("mcr.microsoft.com/playwright:v1.60.0-noble")
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand($"npx -y playwright@1.60.0 run-server --port {serverPort} --host 0.0.0.0")
            .WithPortBinding(serverPort, true)
            .WithExtraHost("host.docker.internal", "host-gateway")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Listening on"))
            .Build();
        await _browserContainer.StartAsync();

        _playwright = await Playwright.CreateAsync();
        var wsEndpoint = $"ws://localhost:{_browserContainer.GetMappedPublicPort(serverPort)}/";
        _browser = await _playwright.Chromium.ConnectAsync(wsEndpoint);

        Assertions.SetDefaultExpectTimeout(15_000);
    }

    public static bool IsVideoEnabled =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_RECORD_VIDEO") is "true" or "1";

    public async Task<IPage> NewPageAsync()
    {
        if (_browser is null) throw new InvalidOperationException("Browser not initialized");

        var options = new BrowserNewContextOptions
        {
            BaseURL = ContainerWebBaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            ReducedMotion = ReducedMotion.Reduce,
            ColorScheme = ColorScheme.Light,
            DeviceScaleFactor = 1
        };

        if (IsVideoEnabled)
        {
            options.RecordVideoDir = "/tmp/videos";
            options.RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 };
        }

        var context = await _browser.NewContextAsync(options);
        return await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        if (_browserContainer is not null) await _browserContainer.DisposeAsync();
        if (_app is not null) await _app.DisposeAsync();
    }
}
```

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
- The container reaches host services via `host.docker.internal` (set via `WithExtraHost`).

### Aspire parameter injection timing

All configuration overrides must be passed as args to `DistributedApplicationTestingBuilder.CreateAsync<T>()`. The AppHost top-level code runs during `CreateAsync` — changing `Configuration` afterward has no effect on `AddParameter` values.

### Browser context settings for stability

Every `NewPageAsync` call must set these options to ensure deterministic screenshots:

| Setting | Value | Why |
|---|---|---|
| `ViewportSize` | Fixed (e.g., 1280x720) | Consistent layout |
| `ReducedMotion` | `Reduce` | No CSS transitions/animations |
| `ColorScheme` | `Light` | Fixed appearance |
| `DeviceScaleFactor` | `1` | No HiDPI variance |

### Container-aware BaseURL

The browser runs inside Docker but the Aspire gateway listens on the host. The `BaseURL` passed to the browser context must use `host.docker.internal` instead of `localhost`:

```csharp
ContainerWebBaseUrl = gatewayUrl
    .Replace("localhost", "host.docker.internal")
    .Replace("127.0.0.1", "host.docker.internal");
```

## Test Class Pattern

```csharp
public sealed class MyFeatureE2ETests : IClassFixture<AspireFixture>, IAsyncLifetime
{
    private readonly AspireFixture _fixture;
    private IPage _page = null!;
    private string _testMethodName = string.Empty;

    public MyFeatureE2ETests(AspireFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _testMethodName = TestContext.Current.TestMethod?.MethodName ?? "unknown";
        await ResetDatabaseAsync();
        _page = await _fixture.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (AspireFixture.IsVideoEnabled && _page.Video is not null)
        {
            var videoDir = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "videos", _testMethodName);
            Directory.CreateDirectory(videoDir);
            await _page.Video.SaveAsAsync(Path.Combine(videoDir, "test.webm"));
        }

        await _page.Context.DisposeAsync();
    }

    [Fact]
    public async Task ShouldDisplayEmptyStateWhenNoData()
    {
        // Act
        await _page.GotoAsync("/items");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        await Expect(_page.GetByText("No items found")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ShouldCreateItemSuccessfully()
    {
        // Arrange
        await _page.GotoAsync("/items");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await _page.GetByRole(AriaRole.Link, new() { Name = "Add Item" }).ClickAsync();
        await _page.GetByLabel("Name").FillAsync("Test Item");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        // Assert
        await Expect(_page.GetByText("Test Item")).ToBeVisibleAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator)
        => Assertions.Expect(locator);

    private async Task ResetDatabaseAsync() { /* drop/reset via fixture */ }
}
```

### Lifecycle Summary

| Scope | What | How |
|---|---|---|
| **Once per class** | Start Aspire + browser container | `AspireFixture.InitializeAsync()` via `IClassFixture<T>` |
| **Per test** | Reset DB, mocks; new page | `IAsyncLifetime.InitializeAsync()` on test class |
| **Per test cleanup** | Dispose browser context | `IAsyncLifetime.DisposeAsync()` on test class |
| **Once per class** | Stop everything | `AspireFixture.DisposeAsync()` |

### Why `IClassFixture` + `IAsyncLifetime` (not `ICollectionFixture`)

- `IClassFixture<AspireFixture>` — fixture lives for the lifetime of the test class. xUnit creates it once, shares across all `[Fact]` methods in the class.
- `IAsyncLifetime` on the **test class** — provides per-test `InitializeAsync`/`DisposeAsync` for isolation (drop DB, new page).
- `ICollectionFixture` would share across **multiple** test classes. Use only when multiple test classes must share the same Aspire instance (trades isolation for speed).

## Visual Regression

### Screenshot Stability Settings

Before capturing, stabilize rendering:

```csharp
// Wait for web fonts
await _page.EvaluateAsync("async () => { await document.fonts.ready; }");

// Mask dynamic content that changes every run
var masks = new ILocator[]
{
    _page.Locator("time"),
    _page.Locator("[data-testid='dynamic-id']")
};

// Capture
var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
{
    FullPage = true,
    Animations = ScreenshotAnimations.Disabled,
    Caret = ScreenshotCaret.Hide,
    Mask = masks
});
```

Key points:
- `document.fonts.ready` — prevents font-loading flicker.
- `Mask` — hides timestamps, generated IDs, and other non-deterministic content.
- `Animations = Disabled` + `Caret = Hide` — eliminates animation frames and blinking cursors.
- Element screenshot (`Locator.ScreenshotAsync`) is preferred for modals/dialogs to avoid scroll and positioning variance.

### Baseline Management

- Store baselines in source control (e.g., `Snapshots/{TestName}/{name}.png`).
- First run without a baseline auto-creates it.
- On mismatch, save `{name}.actual.png` next to the baseline for visual diff.

### Why SkiaSharp, Not Playwright Built-in

Playwright .NET **does not have `ToHaveScreenshotAsync`**. This API only exists in the JavaScript/TypeScript version. The tracking issue ([microsoft/playwright-dotnet#1854](https://github.com/microsoft/playwright-dotnet/issues/1854)) is still open with `P3-collecting-feedback` — the maintainers have stated the internal implementation is not ready to be made public.

Use SkiaSharp for pixel-level comparison:

```csharp
internal static class ScreenshotComparer
{
    private const int ChannelThreshold = 30;

    public static double DiffRatio(byte[] expectedPng, byte[] actualPng)
    {
        using var expected = SKBitmap.Decode(expectedPng);
        using var actual = SKBitmap.Decode(actualPng);

        if (expected is null || actual is null) return 1.0;
        if (expected.Width != actual.Width || expected.Height != actual.Height) return 1.0;

        var expectedBytes = expected.Bytes;
        var actualBytes = actual.Bytes;
        if (expectedBytes.Length != actualBytes.Length) return 1.0;

        long differingPixels = 0;
        var totalPixels = (long)expected.Width * expected.Height;

        for (var i = 0; i + 3 < expectedBytes.Length; i += 4)
        {
            if (Math.Abs(expectedBytes[i] - actualBytes[i]) > ChannelThreshold
                || Math.Abs(expectedBytes[i + 1] - actualBytes[i + 1]) > ChannelThreshold
                || Math.Abs(expectedBytes[i + 2] - actualBytes[i + 2]) > ChannelThreshold)
            {
                differingPixels++;
            }
        }

        return totalPixels == 0 ? 0 : (double)differingPixels / totalPixels;
    }
}
```

Requires NuGet packages: `SkiaSharp` + `SkiaSharp.NativeAssets.Linux` (for CI).

SkiaSharp approach vs Playwright JS `toHaveScreenshot` (for reference if .NET adds parity later):

| | SkiaSharp (current .NET approach) | Playwright `toHaveScreenshot` (JS only) |
|---|---|---|
| **Algorithm** | Per-channel RGBA threshold | YIQ perceptual color distance |
| **Tolerance** | `ChannelThreshold` (e.g., 30 per channel) | `threshold` (0-1 perceptual) + `maxDiffPixelRatio` |
| **Diff output** | Manual: save `actual.png` on mismatch | Auto: expected + actual + diff images |
| **Auto-retry** | None (single capture) | Retries until timeout (absorbs render lag) |
| **Baseline update** | Manual: delete baseline to regenerate | `--update-snapshots` CLI flag |
| **Extra deps** | SkiaSharp + NativeAssets (~3 MB) | None |

## Video Recording

Record test execution videos for review. Controlled by environment variable — off by default for performance.

### Enable

```bash
PLAYWRIGHT_RECORD_VIDEO=true dotnet test
```

### How It Works

1. `AspireFixture.NewPageAsync()` checks `PLAYWRIGHT_RECORD_VIDEO` env var.
2. When enabled, sets `RecordVideoDir` on `BrowserNewContextOptions` to start recording.
3. In test `DisposeAsync`, `SaveAsAsync` downloads the video from the Docker container to `videos/{testMethodName}/`.
4. Videos are saved regardless of test pass or fail.

### Output Structure

```
videos/
├── ShouldDisplayEmptyStateWhenNoData/
│   └── test.webm
├── ShouldCreateItemSuccessfully/
│   └── test.webm
```

### Key Implementation Details

**`RecordVideoDir` is a server-side path** — since the browser runs in Docker, this path (`/tmp/videos`) is inside the container. The actual video retrieval happens via `SaveAsAsync`, which downloads from the container to the host.

**`PathAsync()` throws on remote connections** — never use `page.Video.PathAsync()` with Docker run-server. Always use `SaveAsAsync(localPath)`.

**Capture test method name in `InitializeAsync`** — use `TestContext.Current.TestMethod?.MethodName` (xUnit v3) to get the method name. Store the **string value** in a field for use in `DisposeAsync`. The `TestContext` itself is a moment-in-time snapshot that must not be cached, but extracting a string from it is safe. `TestMethod` availability during `DisposeAsync` (cleanup stage) is not officially guaranteed.

**`SaveAsAsync` before context close** — call `SaveAsAsync` before `_page.Context.DisposeAsync()`. It is safe to call while the video is still in progress; it waits for completion internally.

### .gitignore

Add to the E2E test project's `.gitignore`:

```
videos/
```

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

Per-test stub setup:

```csharp
public async Task InitializeAsync()
{
    _fixture.WireMock.Reset();

    _fixture.WireMock.Given(
        Request.Create().WithPath("/api/external/resource").UsingGet())
        .RespondWith(
            Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"items": []}"""));

    _page = await _fixture.NewPageAsync();
}
```

## Auth Mocking

For apps with browser-side authentication SDKs, inject mock auth via `AddInitScriptAsync`:

```csharp
public async Task<IPage> NewPageAsync()
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

    return await context.NewPageAsync();
}
```

## Playwright Assertion Patterns

```csharp
// Element visibility
await Expect(_page.GetByText("Success")).ToBeVisibleAsync();
await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Submit" })).ToBeEnabledAsync();

// Element count
await Expect(_page.GetByRole(AriaRole.Row)).ToHaveCountAsync(3);

// Text content
await Expect(_page.GetByTestId("status")).ToHaveTextAsync("Active");

// Navigation
await Expect(_page).ToHaveURLAsync(new Regex(@"/items/\w+"));

// Custom timeout for slow operations
await Expect(_page.GetByText("Processing complete"))
    .ToBeVisibleAsync(new() { Timeout = 30_000 });
```

Use `Assertions.Expect()` (Playwright static method), not xUnit `Assert` — Playwright assertions auto-retry until timeout.

## Cheat Sheet

| Topic | Pattern |
|---|---|
| **Fixture** | `IClassFixture<AspireFixture>, IAsyncLifetime` |
| **Start Aspire** | `DistributedApplicationTestingBuilder.CreateAsync<Projects.MyApp_AppHost>([args])` |
| **Wait for resource** | `_app.ResourceNotifications.WaitForResourceHealthyAsync("name")` |
| **Get endpoint** | `_app.GetEndpoint("resource", "http")` |
| **Get conn string** | `await _app.GetConnectionStringAsync("db")` |
| **Playwright image** | `mcr.microsoft.com/playwright:v{version}-noble` |
| **Browser connect** | `_playwright.Chromium.ConnectAsync($"ws://localhost:{port}/")` |
| **New page** | `await _browser.NewContextAsync(options)` → `context.NewPageAsync()` |
| **Navigate** | `await _page.GotoAsync("/path")` |
| **Wait for idle** | `await _page.WaitForLoadStateAsync(LoadState.NetworkIdle)` |
| **Find by role** | `_page.GetByRole(AriaRole.Button, new() { Name = "..." })` |
| **Find by label** | `_page.GetByLabel("Field Name")` |
| **Find by text** | `_page.GetByText("content")` |
| **Find by test id** | `_page.GetByTestId("id")` |
| **Click** | `await locator.ClickAsync()` |
| **Fill** | `await locator.FillAsync("value")` |
| **Assert visible** | `await Expect(locator).ToBeVisibleAsync()` |
| **Screenshot** | `await _page.ScreenshotAsync(new PageScreenshotOptions { ... })` |
| **Element screenshot** | `await locator.ScreenshotAsync(new LocatorScreenshotOptions { ... })` |
| **Block request** | `await context.RouteAsync("**/path", route => route.AbortAsync())` |
| **Inject script** | `await context.AddInitScriptAsync("...")` |
| **Mock external API** | `_fixture.WireMock.Given(...).RespondWith(...)` |
| **Per-test reset** | Drop DB + `WireMock.Reset()` + new page |
| **Enable video** | `PLAYWRIGHT_RECORD_VIDEO=true dotnet test` |
| **Save video** | `await _page.Video!.SaveAsAsync(path)` (before context close) |
| **Test method name** | `TestContext.Current.TestMethod?.MethodName` (xUnit v3, capture in `InitializeAsync`) |

## Rules

1. **Version alignment** — `Microsoft.Playwright` NuGet, Docker image tag, and `npx playwright@` must be the same version.
2. **Docker run-server** — always run browser in container for rendering consistency; never use local browser for E2E.
3. **`--host 0.0.0.0`** — required for `run-server`; without it the WebSocket binds to loopback only.
4. **`host.docker.internal`** — always use for container→host URLs in `BaseURL` and test API calls.
5. **Aspire params at CreateAsync** — all overrides must be in the args array; post-build config changes are ignored.
6. **Per-test isolation** — drop database + reset mocks + new browser page in every `InitializeAsync`.
7. **Dispose context, not page** — `DisposeAsync` should dispose `_page.Context`, which also closes the page.
8. **Playwright assertions over xUnit assertions** — use `Expect()` for DOM checks (auto-retry); reserve xUnit `Assert` for non-DOM values.
9. **Fixed rendering** — always set viewport, ReducedMotion, ColorScheme, DeviceScaleFactor for deterministic screenshots.
10. **Wait before screenshot** — always await `document.fonts.ready` and mask dynamic content.
11. **Video via env var** — recording is controlled by `PLAYWRIGHT_RECORD_VIDEO`; never enable unconditionally.
12. **`SaveAsAsync`, never `PathAsync`** — `PathAsync` throws on remote (Docker) connections; always use `SaveAsAsync` to download videos.
13. **Videos in `.gitignore`** — `videos/` directory must not be committed.
