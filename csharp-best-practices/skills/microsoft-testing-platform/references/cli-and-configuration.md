# MTP CLI and configuration

Use this reference when selecting tests or modules, forwarding arguments, configuring
`testconfig.json`, diagnosing a test process, or interpreting an MTP exit code.

## Contents

- [Command boundaries](#command-boundaries)
- [Select test modules](#select-test-modules)
- [Core platform options](#core-platform-options)
- [Filter tests inside a module](#filter-tests-inside-a-module)
- [Configure with testconfig.json](#configure-with-testconfigjson)
- [Use response files](#use-response-files)
- [Diagnose the test application](#diagnose-the-test-application)
- [Interpret exit codes](#interpret-exit-codes)
- [Migrate common VSTest commands](#migrate-common-vstest-commands)
- [Official references](#official-references)

## Command boundaries

The same MTP application can be started through several drivers. The argument separator depends
on the driver and SDK mode:

| Command | Correct argument shape |
|---|---|
| `dotnet run` | `dotnet run --project tests/UnitTests -- --list-tests` — the separator forwards the rest to the application |
| Native .NET 10 `dotnet test` | `dotnet test --project tests/UnitTests --list-tests` — no extra separator |
| .NET 9-and-earlier MTP MSBuild bridge | `dotnet test --project tests/UnitTests -- --list-tests` — the separator crosses the VSTest/MTP bridge |
| Direct DLL | `dotnet exec path/to/UnitTests.dll --list-tests` |
| Direct executable | `./path/to/UnitTests --list-tests` on Unix or `path\to\UnitTests.exe --list-tests` on Windows |

Do not put the legacy separator into a native MTP `dotnet test` invocation.

`dotnet exec` takes the built **DLL**, not the generated executable. Passing the `.exe` to
`dotnet exec` can produce a misleading dependency-manifest error; invoke the executable directly
when using that form.

## Select test modules

Native MTP `dotnet test` in the .NET 10 SDK separates module selection from test filtering:

| Option | Purpose | Example |
|---|---|---|
| `--project` | Build/run one project | `dotnet test --project tests/Orders.Tests/Orders.Tests.csproj` |
| `--solution` | Build/run projects in one solution | `dotnet test --solution MyTests.slnx` |
| `--test-modules` | Run already-built modules selected by a file glob | `dotnet test --test-modules "**/bin/**/Debug/net10.0/*.dll"` |
| `--root-directory` | Root used to resolve the `--test-modules` glob | `dotnet test --test-modules "**/*.dll" --root-directory .` |
| `--max-parallel-test-modules` | Limit process-level module parallelism | `dotnet test --max-parallel-test-modules 2` |

`--project`, `--solution`, and `--test-modules` are mutually exclusive. An already-built
`--test-modules` run cannot also use build-selection options such as `--framework` or
`--configuration`.

Use module selection to answer “which test application should start?” Use a framework filter to
answer “which tests inside that application should run?” This distinction prevents an apparently
valid filter from hiding a wrong or empty module selection.

## Core platform options

The exact options available are the union of the MTP version, framework runner, and installed
extensions. Run `--help` for the executable's complete list and `--info` to inspect registered
providers and tools.

| Option | Use |
|---|---|
| `--help` | List the active command-line options |
| `--info` | Show platform/environment and registered providers, tools, versions, and options |
| `--list-tests` | Discover tests without executing them; MTP 2.3+ supports `text` and `json` output formats |
| `--results-directory <dir>` | Put reports and result artifacts in a known directory |
| `--config-file <file>` | Select a `testconfig.json` explicitly |
| `--filter-uid <uid...>` | Select tests by stable test-node UID when the framework supports it |
| `--treenode-filter <expression>` | Use the richer MTP test-tree filter when supported |
| `--minimum-expected-tests <n>` | Fail if fewer than `n` tests execute; native `dotnet test` reports exit code 9 |
| `--maximum-failed-tests <n>` | Stop after the configured number of failures; exit code 13 when reached |
| `--ignore-exit-code <n>` | Convert a known, intentionally acceptable non-zero code to success |
| `--timeout <duration>` | Apply a global test execution timeout, e.g. `90s` |
| `--debug` | Pause at startup for a debugger to attach; available in MTP 1.9+ |
| `--no-banner` | Suppress banner and telemetry notice output |
| `--zero-tests-policy <value>` | In MTP 2.3+, choose `allow-skipped` or `strict` for all-skipped sessions |

Prefer `--minimum-expected-tests` in CI to catch a bad module glob or overly broad filter. Do not
use `--ignore-exit-code 8` as the first response to an empty run.

## Filter tests inside a module

Filtering is provided by the framework runner or an MTP extension; it is not one universal
VSTest-compatible grammar.

### MSTest and NUnit

MSTest and NUnit preserve the familiar filter format in MTP:

```bash
dotnet test --project tests/Orders.Tests --filter "FullyQualifiedName~Orders.Tests.OrderTests"
dotnet run --project tests/Orders.Tests -- --filter "TestCategory=Smoke"
```

Verify the exact supported properties for the framework version. Do not assume every adapter
metadata field is available in every framework.

### xUnit.net v3

xUnit.net v3's MTP runner uses explicit options instead of the VSTest filter grammar:

```bash
dotnet test --project tests/Orders.Tests --filter-class Orders.Tests.OrderTests
dotnet test --project tests/Orders.Tests --filter-method Creates_an_order
dotnet test --project tests/Orders.Tests --filter-trait Category=Smoke
dotnet test --project tests/Orders.Tests --filter-namespace Orders.Tests
```

Available families include `--filter-class`, `--filter-not-class`, `--filter-method`,
`--filter-not-method`, `--filter-namespace`, `--filter-not-namespace`, `--filter-trait`,
`--filter-not-trait`, and `--filter-query`. Multiple values may be accepted by one option in MTP;
confirm with `--help` for the installed xUnit version.

## Configure with testconfig.json

MTP's core configuration file is `[appname].testconfig.json` next to the test executable. When
`Microsoft.Testing.Platform.MSBuild` is active, adding a source `testconfig.json` causes MSBuild to
rename it to the application-specific name and copy it to the output directory. The generated file
is overwritten on later builds.

Minimal platform configuration:

```json
{
  "platformOptions": {
    "resultDirectory": "./TestResults",
    "exitProcessOnUnhandledException": false
  }
}
```

Use `--config-file ./path/to/testconfig.json` when several projects need a central file. For a
repository-wide default, append the argument through MSBuild rather than duplicating it in every
CI command:

```xml
<PropertyGroup>
  <TestingPlatformCommandLineArguments>
    $(TestingPlatformCommandLineArguments) --config-file $(MSBuildThisFileDirectory)testconfig.json
  </TestingPlatformCommandLineArguments>
</PropertyGroup>
```

Always preserve the existing `$(TestingPlatformCommandLineArguments)` value. Overwriting it can
silently remove framework or extension arguments supplied by another imported props file.

When a setting exists in multiple places, MTP resolves it in this order:

1. Command-line arguments.
2. Environment variables.
3. `testconfig.json`.
4. Built-in defaults.

`.runsettings` is not the core MTP configuration format. MSTest and NUnit retain support for
`--settings` for framework-specific compatibility, but use `testconfig.json` and MTP extension
options for platform behavior.

Companion templates: [`testconfig.json`](../examples/testconfig.json) and
[`mixed-solution-arguments.props`](../examples/mixed-solution-arguments.props).

## Use response files

Response files are useful for long filter or CI commands. For a direct executable, the response
file name immediately follows `@`:

```text
--filter "FullyQualifiedName~Orders.Tests"
--timeout 90s
```

```bash
./bin/Debug/net10.0/Orders.Tests @run.rsp
dotnet exec bin/Debug/net10.0/Orders.Tests.dll @run.rsp
```

For `dotnet test`, the .NET SDK response-file parser treats each line as one token. Write each
argument and its value on separate lines when the response file is consumed by `dotnet test`:

```text
--project
tests/Orders.Tests/Orders.Tests.csproj
--results-directory
TestResults
--minimum-expected-tests
1
```

Do not use shell continuation characters such as `\\` to join response-file lines.

## Diagnose the test application

Start with registration and argument discovery:

```bash
dotnet run --project tests/Orders.Tests -- --info
dotnet run --project tests/Orders.Tests -- --help
dotnet run --project tests/Orders.Tests -- --list-tests
```

Enable file diagnostics when the process fails before or during discovery:

```bash
dotnet test --project tests/Orders.Tests \
  --diagnostic \
  --diagnostic-verbosity Trace \
  --diagnostic-output-directory ./TestResults/diagnostics \
  --diagnostic-file-prefix orders
```

`--diagnostic-synchronous-write` reduces the chance of losing the final log entries during a
crash, at the cost of slower execution. The renamed MTP 2 options are
`--diagnostic-file-prefix` and `--diagnostic-synchronous-write`; older v1 spellings must not be
copied into a v2 command.

The equivalent environment variables are useful in CI wrappers:

```bash
TESTINGPLATFORM_DIAGNOSTIC=1 \
TESTINGPLATFORM_DIAGNOSTIC_VERBOSITY=Trace \
TESTINGPLATFORM_DIAGNOSTIC_OUTPUT_DIRECTORY=./TestResults/diagnostics \
dotnet test --project tests/Orders.Tests
```

Run `--info` when an extension option is unknown. It shows which command-line providers registered
the option and makes duplicate or missing extension registrations visible.

## Interpret exit codes

MTP returns process exit codes, not VSTest result objects. Preserve them in local scripts and CI.

| Code | Meaning | First action |
|---:|---|---|
| `0` | Selected tests completed with no errors | Success |
| `1` | Unknown/catch-all error | Read output and diagnostics |
| `2` | At least one test failed | Inspect test failure details |
| `3` | Session aborted, such as cancellation | Check cancellation/CI timeout |
| `4` | Extension setup is invalid | Compare runner/extension versions and `--info` |
| `5` | Test-app command-line arguments are invalid | Run `--help`; check framework-specific syntax |
| `7` | Session could not complete, likely a crash | Enable crash diagnostics/dumps |
| `8` | Session ran zero tests | Fix module/discovery/filter; ignore only intentionally |
| `9` | Minimum execution policy was violated | Check `--minimum-expected-tests` and discovery |
| `10` | Framework/adapter infrastructure failed | Inspect fixture/adapter startup errors |
| `13` | Maximum failed-test limit stopped the run | Inspect failures and the configured limit |

Exit code meanings can grow with MTP versions. Use the current troubleshooting documentation when
handling a code not listed here.

## Migrate common VSTest commands

| VSTest-shaped command | MTP replacement |
|---|---|
| `--list-tests` | `--list-tests` |
| `--results-directory <dir>` | `--results-directory <dir>` |
| `--logger trx` | Install `Microsoft.Testing.Extensions.TrxReport`; use `--report-trx` |
| `--collect "Code Coverage"` | Install a coverage extension; use `--coverage` or its extension-specific option |
| `--blame-crash` | Install crash-dump extension; use `--crashdump` |
| `--blame-hang` | Install hang-dump extension; use `--hangdump` |
| `--diag <file>` | Use `--diagnostic` plus `--diagnostic-output-directory`/prefix |
| `--test-adapter-path` | Not relevant to the MTP extension model |
| `--filter <expr>` | Keep for MSTest/NUnit where supported; translate to xUnit.net v3 `--filter-*` options |
| `--settings <file>` | Keep only for framework compatibility; use `testconfig.json` for MTP core settings |

The replacement option is available only when its extension is referenced and registered. Use
`--info` and `--help` instead of assuming that a package was brought in transitively.

## Official references

- [MTP CLI options](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-cli-options)
- [MTP configuration](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-config)
- [MTP troubleshooting and exit codes](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-troubleshooting)
- [MTP `dotnet test` command](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp)
- [Testing with `dotnet test`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test)
