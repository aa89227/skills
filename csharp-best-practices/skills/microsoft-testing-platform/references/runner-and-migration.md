# MTP runner setup and VSTest migration

Use this reference when changing the test project's runner, moving from VSTest, enabling MTP in a
framework, or diagnosing a solution that contains more than one test framework.

## Contents

- [Separate the layers](#separate-the-layers)
- [Choose native or compatibility mode](#choose-native-or-compatibility-mode)
- [Select a framework runner](#select-a-framework-runner)
- [Configure the repository](#configure-the-repository)
- [Migration checklist](#migration-checklist)
- [Common setup failures](#common-setup-failures)
- [Official references](#official-references)

## Separate the layers

The test framework and test platform are different components:

| Layer | Examples | What it controls |
|---|---|---|
| Framework | MSTest, NUnit, xUnit.net, TUnit | Test attributes, assertions, fixtures, framework-specific filters |
| Framework runner | MSTest runner, NUnit runner, xUnit.net MTP integration | Registers the framework with the MTP host |
| Platform | `Microsoft.Testing.Platform` | Test application lifecycle, common CLI, extension points, process exit codes |
| Driver | `dotnet test`, `dotnet run`, `dotnet exec`, `TestProject.exe` | Builds/selects/starts one or more test applications |

Adding `Microsoft.Testing.Platform` alone does not convert a test framework project. Use the
framework's supported MTP integration, then verify the generated output and `--info` registration
list.

## Choose native or compatibility mode

MTP supports .NET 8+ and .NET Framework 4.6.2+, but the `dotnet test` driver has two materially
different paths.

| SDK / path | How to select it | Platform arguments | Use it for |
|---|---|---|---|
| .NET 10+ native MTP | Add `"test": { "runner": "Microsoft.Testing.Platform" }` to the root `global.json` | Pass directly; no extra separator | New migrations and repositories that can run all selected test projects with MTP |
| .NET 9 and earlier compatibility bridge | Enable `TestingPlatformDotnetTestSupport` and reference the MTP MSBuild integration | Put an extra `--` before arguments for the test app | Transitional repositories that still use an older SDK |
| Any supported SDK, direct app | Build the MTP project as an executable | Pass directly to the executable; `dotnet run` uses `--` to forward | Local debugging, unusual hosts, Native AOT or deployment-oriented workflows |

The native mode is the preferred .NET 10+ path. The compatibility bridge is a legacy VSTest-shaped
invocation and must not be treated as the native MTP command-line contract. In particular, do not
add `TestingPlatformDotnetTestSupport` to a .NET 10/MTP 2 migration to compensate for a missing
`global.json` selection.

### Native .NET 10 selection

Merge the `test` object into the existing root `global.json`; preserve `sdk`, `rollForward`, and
other repository settings:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Then confirm the active path before troubleshooting a test failure:

```bash
dotnet --version
dotnet test --help
dotnet test --project tests/UnitTests/UnitTests.csproj --info
```

Native MTP `dotnet test` expects every test project selected by the solution to use MTP. If one
project remains VSTest-only, split the invocations or migrate that project before enabling the
solution-wide selection.

Companion templates: [`global.json`](../examples/global.json),
[`mstest-sdk.csproj`](../examples/mstest-sdk.csproj),
[`nunit-mtp.csproj`](../examples/nunit-mtp.csproj), and
[`xunit-v3-mtp.csproj`](../examples/xunit-v3-mtp.csproj).

### Compatibility bridge for .NET 9 and earlier

The bridge is useful only when the repository cannot yet use the .NET 10 SDK. The shape is:

```xml
<PropertyGroup>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

```bash
# The second -- separates dotnet-test/VSTest arguments from MTP application arguments.
dotnet test --no-build -- --list-tests
dotnet test -- --report-trx --results-directory ./TestResults
```

Do not copy the extra separator into native .NET 10 MTP commands. It changes which parser receives
the argument.

## Select a framework runner

The examples in this section are companion files under `../examples/`. Their package versions are
representative stable versions at the time this skill was authored; update them as a compatible
set rather than mixing old framework packages with a new MTP extension.

### MSTest

For a normal SDK-style test project, prefer `MSTest.Sdk`:

```xml
<Project Sdk="MSTest.Sdk/4.3.2">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

`MSTest.Sdk` enables the MSTest runner/MTP integration by default. Do not add a second direct
reference to `MSTest` just to obtain the runner when the SDK already supplies the framework.

Use manual setup when the project needs a different top-level SDK, such as
`Microsoft.NET.Sdk.Web` for an ASP.NET Core integration test project:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableMSTestRunner>true</EnableMSTestRunner>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MSTest" Version="4.3.2" />
  </ItemGroup>
</Project>
```

The manual setup is also the right place to keep an ASP.NET Core host SDK. Replacing
`Microsoft.NET.Sdk.Web` with `MSTest.Sdk` can remove the Web SDK targets the test project needs.

MSTest's MTP support starts with MSTest 3.2.0, but use the current supported release when
migrating. If the project must run through the .NET 9-and-earlier compatibility bridge, also apply
`TestingPlatformDotnetTestSupport` at the repository level.

### NUnit

Enable the NUnit runner and build the project as an executable:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableNUnitRunner>true</EnableNUnitRunner>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
    <PackageReference Include="NUnit" Version="4.6.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
    <PackageReference Include="NUnit.Analyzers" Version="4.11.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

`NUnit3TestAdapter` 5.0.0 is the minimum version documented for MTP support. Keep
`Microsoft.NET.Test.Sdk` when Visual Studio or other consumers still need the VSTest path; it is
not a reason to use VSTest when the repository has opted into native MTP.

### xUnit.net v3

For a repository that explicitly targets MTP v2, select the MTP-v2 package variant:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-v2" Version="3.2.2" />
  </ItemGroup>
</Project>
```

The `xunit.v3` package family can select a default MTP major version. Use
`xunit.v3.mtp-v1`, `xunit.v3.mtp-v2`, or `xunit.v3.mtp-off` when the repository needs an explicit
choice. xUnit.net v3's MTP filters are not the VSTest filter grammar; use options such as
`--filter-class`, `--filter-method`, `--filter-namespace`, and `--filter-trait`.

Keep `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` only when supported IDEs or CI still
require VSTest. Remove them after all supported consumers use MTP and the compatibility requirement
has been deliberately retired.

### TUnit

TUnit is built on MTP rather than being an adapter layered over VSTest. Do not add MSTest, NUnit,
or xUnit runner properties to a TUnit project. Follow the TUnit package and CLI documentation for
the current version, then apply the common MTP rules in this skill.

## Configure the repository

Use `Directory.Build.props` to keep a migration consistent, but scope properties to the projects
that actually use them. Do not blindly set all projects to `Exe` if the repository contains
non-test applications.

For an all-MTP test repository, a simple shared policy can look like this:

```xml
<Project>
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <OutputType>Exe</OutputType>
  </PropertyGroup>
</Project>
```

In repositories where `IsTestProject` is not available early enough in the import graph, use
explicit project-name or project-path conditions, or set `OutputType` in each test project. Test
the evaluated properties with a binary log if an SDK import appears to overwrite them.

Do not set a custom entry point unless the project intentionally opts out of generated entry-point
support:

```xml
<PropertyGroup>
  <GenerateTestingPlatformEntryPoint>false</GenerateTestingPlatformEntryPoint>
</PropertyGroup>
```

When that property is false, the project must register the framework and every desired MTP
extension in its own `Main`/`TestApplication` builder. That is an advanced host customization;
keep it isolated and follow the current extension registration API.

## Migration checklist

1. Inventory framework, target framework, SDK, package versions, CI task, filters, reporters,
   coverage, dumps, and `.runsettings` usage.
2. Upgrade the framework runner and extensions as a compatible set.
3. Set `OutputType` to `Exe` for every MTP project and remove accidental hand-written entry points.
4. Enable the framework runner (`MSTest.Sdk`, `EnableMSTestRunner`, `EnableNUnitRunner`, or
   `UseMicrosoftTestingPlatformRunner`).
5. For .NET 10+, add the native `global.json` test-runner selection.
6. Replace VSTest-only options: `--logger trx` → `--report-trx`, `--collect "Code Coverage"` →
   `--coverage`, and `--blame-*` → the crash/hang dump extensions.
7. Convert filters per framework; do not carry an xUnit VSTest filter into xUnit.net v3 MTP mode.
8. Run `--help`, `--info`, and `--list-tests` for each test module.
9. Run the complete solution and CI pipeline, preserving results and exit codes.
10. Remove the compatibility bridge only after the repository no longer supports .NET 9 or earlier.

## Common setup failures

| Symptom | Likely cause | Check |
|---|---|---|
| `dotnet test` says the project uses the wrong test runner | Native MTP was selected but one project is still VSTest | Inspect every test project and split/migrate the solution |
| Duplicate entry point / `Main` conflict | Generated MTP entry point plus user-defined `Main` | Remove the custom entry point or explicitly opt out and register everything manually |
| `--report-trx` is unknown | TRX extension is not referenced or not registered | Run `--info`, install `Microsoft.Testing.Extensions.TrxReport`, check MTP major alignment |
| A VSTest filter selects nothing | The framework's MTP filter grammar differs | Use the framework's MTP options; xUnit.net v3 uses `--filter-*` options |
| `--` appears in the wrong place | Native and compatibility command shapes were mixed | Native .NET 10: no separator for `dotnet test`; legacy bridge: use one |
| Test project builds as a library | `OutputType` or runner SDK was not enabled | Evaluate the project and verify an executable is emitted |
| CI reports success with no tests | A zero-test policy was suppressed or modules were filtered out | Fix selection/discovery first; only intentionally ignore exit code 8 |

## Official references

- [MTP overview](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
- [Migrate from VSTest to MTP](https://learn.microsoft.com/dotnet/core/testing/migrating-vstest-microsoft-testing-platform)
- [Run tests with MSTest](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-running-tests)
- [MTP support in NUnit](https://learn.microsoft.com/dotnet/core/testing/unit-testing-nunit-runner-intro)
- [xUnit.net v3 MTP support](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [Test platforms overview](https://learn.microsoft.com/dotnet/core/testing/test-platforms-overview)
