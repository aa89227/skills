# Visual Regression (Screenshot Comparison)

Load this reference when implementing screenshot-based visual regression testing in Playwright E2E tests.

## Screenshot Stability Settings

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

## Baseline Management

- Store baselines in source control (e.g., `Snapshots/{TestName}/{name}.png`).
- First run without a baseline auto-creates it.
- On mismatch, save `{name}.actual.png` next to the baseline for visual diff.

## SkiaSharp Pixel Comparison

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

## SkiaSharp vs Playwright JS `toHaveScreenshot`

For reference if .NET adds parity later:

| | SkiaSharp (current .NET approach) | Playwright `toHaveScreenshot` (JS only) |
|---|---|---|
| **Algorithm** | Per-channel RGBA threshold | YIQ perceptual color distance |
| **Tolerance** | `ChannelThreshold` (e.g., 30 per channel) | `threshold` (0-1 perceptual) + `maxDiffPixelRatio` |
| **Diff output** | Manual: save `actual.png` on mismatch | Auto: expected + actual + diff images |
| **Auto-retry** | None (single capture) | Retries until timeout (absorbs render lag) |
| **Baseline update** | Manual: delete baseline to regenerate | `--update-snapshots` CLI flag |
| **Extra deps** | SkiaSharp + NativeAssets (~3 MB) | None |

## Rules

1. **Fixed rendering** — always set viewport, ReducedMotion, ColorScheme, DeviceScaleFactor for deterministic screenshots.
2. **Wait before screenshot** — always await `document.fonts.ready` and mask dynamic content.
