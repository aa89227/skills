# MTP extensions and CI integration

Use this reference when adding reports, code coverage, crash/hang evidence, retries, telemetry
policy, or CI-specific output to an MTP test application.

## Contents

- [Extension registration](#extension-registration)
- [Reports](#reports)
- [Code coverage](#code-coverage)
- [Crash and hang evidence](#crash-and-hang-evidence)
- [Retry](#retry)
- [Telemetry and experimental features](#telemetry-and-experimental-features)
- [CI command patterns](#ci-command-patterns)
- [Manual registration](#manual-registration)
- [Official references](#official-references)

## Extension registration

MTP extensions are NuGet packages. When `Microsoft.Testing.Platform.MSBuild` is active—the normal
case for current MSTest, NUnit, and xUnit.net MTP runners—the package is detected and registered
automatically, and the entry point is generated. A package reference alone should be enough:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="2.3.3" />
</ItemGroup>
```

Check registration instead of guessing:

```bash
dotnet run --project tests/Orders.Tests -- --info
dotnet run --project tests/Orders.Tests -- --help
```

Align the platform, framework runner, and extension major versions. A package graph that combines
MTP v1 and v2 APIs can fail before discovery with a type-load or extension-setup error. If the
repository uses Central Package Management, pin the compatible set in one place and inspect the
resolved graph with `dotnet list package --include-transitive`.

Do not disable generated entry-point support casually. If the project sets
`<GenerateTestingPlatformEntryPoint>false</GenerateTestingPlatformEntryPoint>`, automatic extension
registration is not available and every required extension must be registered in the custom test
application builder.

## Reports

All report extensions write to the MTP results directory. Set it explicitly in local scripts and
CI so artifacts have a stable location:

```bash
dotnet test --project tests/Orders.Tests \
  --results-directory ./TestResults \
  --report-trx \
  --report-trx-filename "Orders_{tfm}_{arch}.trx"
```

### Report extension matrix

| Need | Package | Option | Notes |
|---|---|---|---|
| Visual Studio/Azure DevOps TRX | `Microsoft.Testing.Extensions.TrxReport` | `--report-trx` | Stable; MTP 2.3+ streams results while the run progresses |
| HTML | `Microsoft.Testing.Extensions.HtmlReport` | `--report-html` | MTP 2.3+ and experimental; output format may change |
| JUnit XML | `Microsoft.Testing.Extensions.JUnitReport` | `--report-junit` | MTP 2.3+ and experimental |
| CTRF JSON | `Microsoft.Testing.Extensions.CtrfReport` | `--report-ctrf` | MTP 2.3+ and experimental |
| Azure DevOps annotations | `Microsoft.Testing.Extensions.AzureDevOpsReport` | `--report-azdo` | Writes Azure DevOps workflow commands and can add CI context |
| GitHub Actions annotations/summary | Use the current GitHub Actions report extension documented by MTP | `--report-gh` | MTP 2.3+ and experimental; activates only in GitHub Actions |

For deterministic artifact names on MTP 2.3+, report names can use lower-case placeholders such as
`{asm}`, `{tfm}`, `{arch}`, `{pname}`, `{pid}`, and `{time}`. Keep the report filename inside the
results directory; do not use an arbitrary absolute path in a report filename option.

Do not use VSTest's `--logger trx` after migrating to native MTP. Install the TRX extension and use
`--report-trx`. For the .NET 9-and-earlier compatibility bridge, put the MTP option after the extra
separator:

```bash
# Native .NET 10 MTP
dotnet test --project tests/Orders.Tests --report-trx

# Legacy MTP MSBuild bridge
dotnet test --project tests/Orders.Tests -- --report-trx
```

## Code coverage

Choose one coverage implementation deliberately.

### Microsoft Code Coverage

Install `Microsoft.Testing.Extensions.CodeCoverage` and use the MTP coverage options:

```bash
dotnet test --project tests/Orders.Tests \
  --coverage \
  --coverage-output ./TestResults/coverage.cobertura.xml \
  --coverage-output-format cobertura
```

The extension supports managed coverage and can support native instrumentation when explicitly
enabled in its coverage settings. Test assemblies are excluded by default in the MTP extension;
do not assume the VSTest default of including the test assembly.

### Coverlet MTP integration

If the repository standardizes on Coverlet, use the MTP integration package rather than the
VSTest collector:

```bash
dotnet add tests/Orders.Tests package coverlet.MTP
dotnet test --project tests/Orders.Tests --coverlet --coverlet-output-format cobertura
```

Do not combine `coverlet.collector` assumptions with a native MTP run. The MTP extension owns its
own command-line options and output behavior.

## Crash and hang evidence

Install the relevant extension and capture evidence in a CI artifact directory:

```bash
dotnet test --project tests/Orders.Tests \
  --results-directory ./TestResults \
  --crashdump \
  --crashdump-type Triage \
  --hangdump \
  --hangdump-timeout 5m
```

| Failure | Package | Main options |
|---|---|---|
| Process crash | `Microsoft.Testing.Extensions.CrashDump` | `--crashdump`, `--crashdump-type`, `--crashdump-filename` |
| Process hang | `Microsoft.Testing.Extensions.HangDump` | `--hangdump`, `--hangdump-timeout`, `--hangdump-type`, `--hangdump-filename` |
| Linux/macOS crash report | Crash-dump extension | `--crash-report` or `--crash-report-if-supported` (MTP 2.3+) |

Crash-dump support is not the same as a test assertion failure. Keep dumps out of source control,
upload them only from trusted CI jobs, and make the artifact retention policy explicit. The crash
extension is ignored for .NET Framework; use a platform-appropriate postmortem tool for that
target.

## Retry

Install `Microsoft.Testing.Extensions.Retry` only for tests with a documented transient dependency:

```bash
dotnet test --project tests/Orders.Tests \
  --retry-failed-tests 2 \
  --retry-failed-tests-delay 1s \
  --retry-failed-tests-max-percentage 25
```

Retry is activated by `--retry-failed-tests <attempts>`. Use either the maximum-percentage or
maximum-test guard, not both. Record the initial failure and retry count in CI output. A retry that
passes is evidence of flakiness, not proof that the original failure was harmless.

The Retry package uses the MTP Tools license; review the package license before adding it to a
redistributed test tool or product repository.

## Telemetry and experimental features

MTP telemetry is enabled by default in current integrations. Opt out in a repository or CI job when
policy requires it:

```bash
TESTINGPLATFORM_TELEMETRY_OPTOUT=1 dotnet test --project tests/Orders.Tests
```

`DOTNET_CLI_TELEMETRY_OPTOUT=1` is also honored. Do not confuse telemetry opt-out with diagnostic
logging; diagnostics are controlled by `--diagnostic` or the `TESTINGPLATFORM_DIAGNOSTIC*`
variables.

Treat HTML, JUnit, CTRF, GitHub Actions, diagnostics/video, and Microsoft.Extensions integrations
marked experimental in the current MTP documentation as version-sensitive. Pin and test them in
the CI image, and avoid depending on undocumented option names.

## CI command patterns

### GitHub Actions

For a native .NET 10 MTP repository, a minimal shell step can be:

```yaml
- name: Test
  run: >-
    dotnet test --solution ./MyTests.slnx
    --results-directory ./TestResults
    --report-trx
    --coverage
    --coverage-output-format cobertura
- name: Upload test artifacts
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: TestResults/
```

Add `--report-gh` only after the current GitHub Actions report extension is referenced. The
extension is inert outside GitHub Actions and is experimental in MTP 2.3+.

### Azure DevOps

The `.NET` task is the appropriate task for native MTP. With a repository-root `global.json` that
selects MTP, pass native MTP arguments directly:

```yaml
- task: DotNetCoreCLI@2
  displayName: Run MTP tests
  inputs:
    command: test
    projects: '**/*Tests.csproj'
    arguments: '--results-directory $(Agent.TempDirectory)/TestResults --report-trx'
```

When the pipeline still uses the .NET 9-and-earlier compatibility bridge, use the bridge separator
and point the report directory explicitly:

```yaml
arguments: '-- --report-trx --results-directory $(Agent.TempDirectory)/TestResults'
```

Do not use the Azure DevOps `VSTest@3` task for a native MTP-only workflow. Use `DotNetCoreCLI@2`
or invoke the test executable directly.

The complete command runbook is available at
[`examples/ci-mtp-commands.sh`](../examples/ci-mtp-commands.sh).

### Direct executable in CI

Publishing or building the test project and invoking the output directly is useful when CI needs
the same command as a deployment-like test host:

```bash
dotnet publish tests/Orders.Tests/Orders.Tests.csproj -c Release -o ./artifacts/Orders.Tests
./artifacts/Orders.Tests/Orders.Tests --results-directory ./TestResults --list-tests
```

On a framework-dependent build, use the executable produced by the target runtime. Use
`dotnet exec` with the `.dll` only when the direct executable form is unavailable or unsuitable.

## Manual registration

Manual registration is only required when generated entry-point support is disabled or when a
custom host intentionally controls extension ordering. The current TRX documentation shows the
registration call:

```csharp
var builder = await TestApplication.CreateBuilderAsync(args);
builder.AddTrxReportProvider(); // Register TRX after other providers.
```

Register the TRX provider last when using the documented builder because its current implementation
depends on registration order. Do not copy a complete custom `Main` from an older MTP sample;
confirm the current `BuildAsync`/`RunAsync` host API and framework registration calls for the
installed MTP version.

## Official references

- [MTP features](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-features)
- [MTP test reports](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-test-reports)
- [MTP code coverage](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-code-coverage)
- [MTP crash and hang dumps](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-crash-hang-dumps)
- [MTP retry](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-retry)
- [MTP telemetry](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-telemetry)
