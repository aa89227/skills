---
name: microsoft-testing-platform
description: |
  Configure, migrate, run, debug, and review .NET tests with Microsoft.Testing.Platform (MTP),
  including MSTest, NUnit, xUnit.net v3, TUnit, MTP CLI options, testconfig.json, extensions,
  direct test executables, and CI reporting. Use when a task mentions Microsoft Testing Platform,
  MTP, MTP runners, EnableMSTestRunner, EnableNUnitRunner, UseMicrosoftTestingPlatformRunner,
  MSTest.Sdk, test runner selection in global.json, MTP migration, --report-trx, --coverage,
  test modules, or troubleshooting MTP exit codes. Do not trigger for ordinary test-writing tasks
  unless the test execution platform or runner configuration is part of the request.
  Trigger phrases: "Microsoft Testing Platform", "MTP", "Microsoft.Testing.Platform", "MTP runner",
  "MSTest.Sdk", "EnableMSTestRunner", "EnableNUnitRunner", "UseMicrosoftTestingPlatformRunner",
  "testconfig.json", "dotnet test with MTP", "MTP migration", "MTP extension", "MTP exit code".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["csharp", "dotnet", "testing", "microsoft-testing-platform", "mtp", "test-runner", "ci"]
  trigger_keywords: ["Microsoft Testing Platform", "MTP", "Microsoft.Testing.Platform", "MSTest.Sdk", "EnableMSTestRunner", "EnableNUnitRunner", "UseMicrosoftTestingPlatformRunner", "testconfig.json", "MTP migration"]
---

# Microsoft Testing Platform

Use MTP as a test platform, not as a replacement for a test framework. The framework (MSTest,
NUnit, xUnit.net, or TUnit) owns test attributes and assertions; MTP owns the test application,
execution lifecycle, CLI extension model, reporting, and platform-level diagnostics.

## Core model

Keep these layers separate when inspecting or changing a test project:

| Layer | Responsibility | Typical evidence |
|---|---|---|
| Test framework | Test cases, attributes, assertions, framework filters | `MSTest`, `NUnit`, `xunit.v3`, `TUnit` |
| Framework runner | Bridges the framework into MTP | `MSTest.Sdk`, `EnableNUnitRunner`, `UseMicrosoftTestingPlatformRunner` |
| MTP host | Builds the test application, discovers/runs tests, owns platform options | `Microsoft.Testing.Platform`, generated `Main`, `--info` |
| MTP extensions | Reports, coverage, retry, dumps, telemetry, CI integration | `Microsoft.Testing.Extensions.*` packages |
| Driver | Builds/selects test modules and starts the host | `dotnet run`, `dotnet test`, `dotnet exec`, executable |

Do not infer an MTP option from a VSTest option. Ask the built test application what it supports:

```bash
dotnet run --project tests/UnitTests -- --help
dotnet run --project tests/UnitTests -- --info
```

## Default workflow

1. **Inspect before editing.** Check the SDK selected by `global.json`, target frameworks, the
   test framework package, runner properties, `OutputType`, `Directory.Build.props`, and CI
   commands. Check every test project when the solution contains more than one framework.

2. **Choose the driver mode.** For the native MTP `dotnet test` experience, use the .NET 10 SDK
   or later and add this to the existing root `global.json` (merge it; do not replace other keys):

   ```json
   {
     "test": {
       "runner": "Microsoft.Testing.Platform"
     }
   }
   ```

   MTP itself supports .NET 8+ and .NET Framework 4.6.2+, but native MTP mode in `dotnet test`
   starts with the .NET 10 SDK. With .NET 9 or earlier, direct execution still works; the
   `Microsoft.Testing.Platform.MSBuild` bridge uses the legacy VSTest-shaped `dotnet test` path
   and requires an extra `--` before platform arguments. See
   [`references/runner-and-migration.md`](references/runner-and-migration.md).

3. **Enable the framework runner.** Use the framework's current integration rather than adding
   the core MTP package directly to an ordinary test project:

   | Framework | Preferred setup |
   |---|---|
   | MSTest | Use `MSTest.Sdk` for a normal SDK-style test project; it enables MTP by default. For a project that must keep another top-level SDK, use the manual `EnableMSTestRunner` setup. |
   | NUnit | Set `EnableNUnitRunner` to `true`, set `OutputType` to `Exe`, and use `NUnit3TestAdapter` 5.0.0 or newer. |
   | xUnit.net | Use xUnit.net v3 and set `UseMicrosoftTestingPlatformRunner` to `true`. Select the MTP major version explicitly when the repository requires it. |
   | TUnit | Treat TUnit as MTP-native and follow its current package/setup guidance. |

4. **Make the output executable.** MTP test projects run as applications. Verify that the
   framework SDK/runner produces an executable; add `<OutputType>Exe</OutputType>` for manual or
   migrated setups. Do not add a hand-written `Main` while the generated MTP entry point is active.

5. **Run in increasing scope.** Start with `--help`/`--list-tests`, then one project, then the
   solution or CI command. In native .NET 10 MTP mode, pass platform arguments directly to
   `dotnet test`; the extra `--` is for `dotnet run` argument forwarding, or for the legacy
   .NET 9-and-earlier bridge.

   ```bash
   dotnet test --project tests/UnitTests/UnitTests.csproj --list-tests
   dotnet test --project tests/UnitTests/UnitTests.csproj --results-directory ./TestResults
   dotnet test --solution ./MyTests.slnx --max-parallel-test-modules 2
   ```

6. **Add extensions deliberately.** Install only the extension package required by the workflow;
   with `Microsoft.Testing.Platform.MSBuild` active, the package is auto-detected and registered.
   Use `--report-trx`, `--coverage`, `--crashdump`, `--hangdump`, or `--retry-failed-tests` only
   after confirming that the corresponding extension is referenced and appears in `--info`.
   See [`references/extensions-and-ci.md`](references/extensions-and-ci.md).

7. **Validate failures as platform failures or test failures.** Preserve the process exit code.
   Exit code `2` means a test failed; `5` means invalid test-app arguments; `8` means zero tests;
   `9` means a minimum-test policy was violated. Inspect the output and enable `--diagnostic`
   before changing test code. See [`references/cli-and-configuration.md`](references/cli-and-configuration.md).

## Rules that prevent common MTP mistakes

- Treat `.NET 10+ + global.json test.runner` as the native MTP path. Do not add
  `TestingPlatformDotnetTestSupport` as a workaround in a .NET 10/MTP 2 migration; that property
  belongs to the .NET 9-and-earlier compatibility path.
- When native MTP mode is selected in `global.json`, ensure every test project selected by the
  solution uses MTP. Mixed MTP/VSTest solutions can fail before tests run.
- Distinguish module selection from test selection: `--project`, `--solution`, and
  `--test-modules` select test applications; framework-specific options select tests inside one
  application. `--filter` is not a universal MTP filter—xUnit.net v3 uses options such as
  `--filter-class` and `--filter-method`.
- Replace VSTest's broad `--logger trx` and `--collect "Code Coverage"` mental model with the
  extension-specific MTP options `--report-trx` and `--coverage`.
- Prefer `testconfig.json` for platform configuration. MTP resolves command-line arguments before
  environment variables, then config-file values, then defaults. MSTest and NUnit may still
  support `.runsettings` for framework-specific settings; that does not make `.runsettings` the
  MTP core configuration format.
- Do not invent a `Main` method when `GenerateTestingPlatformEntryPoint` is enabled. A second
  entry point is a common cause of MTP build errors.
- Align the MTP major version across the framework runner and extensions. Do not copy a package
  version from an old VSTest example without checking the current framework and extension
  compatibility notes.
- Do not hide a zero-test result with `--ignore-exit-code 8` unless the project intentionally
  allows an empty module; first check module selection, target framework, discovery, and filters.
- Treat retry as a diagnostic or transient-infrastructure aid, not as a way to make deterministic
  test failures green. Record retry behavior in CI output.

## Progressive disclosure map

Load only the reference that matches the task:

- **Runner setup, framework matrix, migration, or mixed solutions:**
  [`references/runner-and-migration.md`](references/runner-and-migration.md)
- **CLI switches, filters, `testconfig.json`, response files, diagnostics, or exit codes:**
  [`references/cli-and-configuration.md`](references/cli-and-configuration.md)
- **Coverage, reports, dumps, retry, telemetry, custom CI, or extension registration:**
  [`references/extensions-and-ci.md`](references/extensions-and-ci.md)

Use the companion examples as focused templates, not as a reason to copy every package into every
project:

- [`examples/global.json`](examples/global.json) — native .NET 10 MTP selection.
- [`examples/mstest-sdk.csproj`](examples/mstest-sdk.csproj),
  [`examples/nunit-mtp.csproj`](examples/nunit-mtp.csproj), and
  [`examples/xunit-v3-mtp.csproj`](examples/xunit-v3-mtp.csproj) — framework runner setup.
- [`examples/testconfig.json`](examples/testconfig.json) — platform configuration shape.
- [`examples/mixed-solution-arguments.props`](examples/mixed-solution-arguments.props) — scoped
  `TestingPlatformCommandLineArguments` for framework-specific options.
- [`examples/ci-mtp-commands.sh`](examples/ci-mtp-commands.sh) — native, legacy, direct-run, and
  diagnostic commands.

## Official source of truth

For version-sensitive behavior, consult the current Microsoft documentation before editing a
project:

- [MTP overview](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
- [Testing with `dotnet test`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test)
- [MTP `dotnet test` options](https://learn.microsoft.com/dotnet/core/tools/dotnet-test-mtp)
- [Migrate from VSTest to MTP](https://learn.microsoft.com/dotnet/core/testing/migrating-vstest-microsoft-testing-platform)
- [MTP CLI options](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-cli-options)
