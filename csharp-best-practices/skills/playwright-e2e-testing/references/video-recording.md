# Video Recording

Load this reference when adding test execution video recording to Playwright E2E tests.

## Overview

Record browser sessions for review. Controlled by environment variable — off by default for performance. Videos are saved regardless of test pass or fail.

## Enable

```bash
PLAYWRIGHT_RECORD_VIDEO=true dotnet test
```

## AspireFixture Changes

Add video support to the fixture's `NewPageAsync`:

```csharp
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
```

## Test Class Changes

Capture the test method name in `InitializeAsync` and save the video in `DisposeAsync`:

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
}
```

## Output Structure

```
videos/
├── ShouldDisplayEmptyStateWhenNoData/
│   └── test.webm
├── ShouldCreateItemSuccessfully/
│   └── test.webm
```

## Key Implementation Details

**`RecordVideoDir` is a server-side path** — since the browser runs in Docker, this path (`/tmp/videos`) is inside the container. The actual video retrieval happens via `SaveAsAsync`, which downloads from the container to the host.

**`PathAsync()` throws on remote connections** — never use `page.Video.PathAsync()` with Docker run-server. Always use `SaveAsAsync(localPath)`.

**Capture test method name in `InitializeAsync`** — use `TestContext.Current.TestMethod?.MethodName` (xUnit v3) to get the method name. Store the **string value** in a field for use in `DisposeAsync`. The `TestContext` itself is a moment-in-time snapshot that must not be cached, but extracting a string from it is safe. `TestMethod` availability during `DisposeAsync` (cleanup stage) is not officially guaranteed.

**`SaveAsAsync` before context close** — call `SaveAsAsync` before `_page.Context.DisposeAsync()`. It is safe to call while the video is still in progress; it waits for completion internally.

## .gitignore

Add to the E2E test project's `.gitignore`:

```
videos/
```

## Rules

1. **Video via env var** — recording is controlled by `PLAYWRIGHT_RECORD_VIDEO`; never enable unconditionally.
2. **`SaveAsAsync`, never `PathAsync`** — `PathAsync` throws on remote (Docker) connections; always use `SaveAsAsync` to download videos.
3. **Videos in `.gitignore`** — `videos/` directory must not be committed.
