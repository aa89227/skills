# Video Recording

Load this reference when adding test execution video recording to Playwright E2E tests.

## Overview

Record browser sessions for review. Controlled by environment variable — off by default for performance. Videos are saved regardless of test pass or fail.

## Enable

```bash
PLAYWRIGHT_RECORD_VIDEO=true dotnet test
```

## AspireFixture Changes

Add video support to the fixture's `NewContextAsync`:

```csharp
public static bool IsVideoEnabled =>
    Environment.GetEnvironmentVariable("PLAYWRIGHT_RECORD_VIDEO") is "true" or "1";

public async Task<IBrowserContext> NewContextAsync()
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

    return await _browser.NewContextAsync(options);
}
```

## Test Method Changes

### With ICollectionFixture (primary pattern)

Save the video before disposing the context. Use `TestContext.Current.TestMethod?.MethodName` (xUnit v3) to name the output folder:

```csharp
[Collection(E2ECollection.Name)]
public sealed class MyFeatureE2ETests(AspireFixture fixture)
{
    [Fact]
    public async Task ShouldCreateItemSuccessfully()
    {
        await fixture.ResetDatabaseAsync();
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        // ... test logic ...

        await SaveVideoAsync(page);
    }

    private static async Task SaveVideoAsync(IPage page)
    {
        if (!AspireFixture.IsVideoEnabled || page.Video is null) return;

        var methodName = TestContext.Current.TestMethod?.MethodName ?? "unknown";
        var videoDir = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "videos", methodName);
        Directory.CreateDirectory(videoDir);
        await page.Video.SaveAsAsync(Path.Combine(videoDir, "test.webm"));
    }
}
```

### With IClassFixture + IAsyncLifetime (alternative)

```csharp
public sealed class MyFeatureE2ETests : IClassFixture<AspireFixture>, IAsyncLifetime
{
    private readonly AspireFixture _fixture;
    private IBrowserContext _context = null!;
    private IPage _page = null!;
    private string _testMethodName = string.Empty;

    public MyFeatureE2ETests(AspireFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        _testMethodName = TestContext.Current.TestMethod?.MethodName ?? "unknown";
        await _fixture.ResetDatabaseAsync();
        _context = await _fixture.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (AspireFixture.IsVideoEnabled && _page.Video is not null)
        {
            var videoDir = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "videos", _testMethodName);
            Directory.CreateDirectory(videoDir);
            await _page.Video.SaveAsAsync(Path.Combine(videoDir, "test.webm"));
        }

        await _context.DisposeAsync();
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

**`SaveAsAsync` before context dispose** — call `SaveAsAsync` before the context is disposed. It is safe to call while the video is still in progress; it waits for completion internally.

**`TestContext.Current.TestMethod?.MethodName`** — xUnit v3 API to get the method name. In the `IAsyncLifetime` pattern, capture the string value in `InitializeAsync` and store in a field for `DisposeAsync`, because `TestContext` availability during cleanup is not officially guaranteed. The `TestContext` itself is a moment-in-time snapshot that must not be cached, but extracting a string from it is safe.

## .gitignore

Add to the E2E test project's `.gitignore`:

```
videos/
```

## Rules

1. **Video via env var** — recording is controlled by `PLAYWRIGHT_RECORD_VIDEO`; never enable unconditionally.
2. **`SaveAsAsync`, never `PathAsync`** — `PathAsync` throws on remote (Docker) connections; always use `SaveAsAsync` to download videos.
3. **Videos in `.gitignore`** — `videos/` directory must not be committed.
