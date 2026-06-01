---
name: project-init
description: |
  Use when initializing a new C# / .NET solution from scratch (.NET 10+): creating the .slnx
  solution with `dotnet new sln --format slnx`, scaffolding repo-wide config (global.json,
  Directory.Build.props, Directory.Build.targets, Directory.Packages.props / Central Package
  Management, nuget.config, .editorconfig, .gitignore, .gitattributes, dotnet-tools.json),
  and laying out the folder structure (src/ + tests/
  vs flat). Also asks whether the solution uses Aspire and, if so, installs the Aspire CLI
  as a local dotnet tool and documents its usage in the README.
  Trigger phrases: "init project", "new solution", "scaffold solution", "set up repo",
  "專案初始化", "建立方案", "開新專案", "資料夾結構", "slnx", "Directory.Build.props",
  "central package management", "global.json", "aspire", "aspire cli".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["csharp", "dotnet", "project-init", "scaffolding", "msbuild", "dotnet10"]
  trigger_keywords: ["init", "solution", "slnx", "scaffold", "Directory.Build.props", "Directory.Packages.props", "global.json", "central package management", "aspire", "專案初始化", "資料夾結構"]
---

## Auto-Trigger Scenarios

This skill activates when:
- Starting a brand-new .NET solution / repository
- User asks how to scaffold a solution, set up repo-wide build config, or enable Central Package Management
- User asks how folders should be organized (`src` / `tests`, flat, layered)
- User mentions `.slnx`, `Directory.Build.props`, `Directory.Packages.props`, or `global.json`

# C# / .NET Project Init (.NET 10+)

**Requires:** .NET 10 SDK (`dotnet --version` ≥ `10.0.100`). The `.slnx` format and `dotnet new buildprops`/`buildtargets` are assumed available.

## Config Quick Reference

| File | CLI template | Purpose |
|---|---|---|
| `*.slnx` | `dotnet new sln --format slnx` | Solution file (XML, default in .NET 10) |
| `global.json` | `dotnet new globaljson` | Pin SDK version + roll-forward policy |
| `Directory.Build.props` | `dotnet new buildprops` | Repo-wide MSBuild props (imported **first**) |
| `Directory.Build.targets` | `dotnet new buildtargets` | Repo-wide MSBuild targets (imported **last**) |
| `Directory.Packages.props` | `dotnet new packagesprops`¹ | Central Package Management (CPM) |
| `nuget.config` | `dotnet new nugetconfig` | Package sources + source mapping |
| `.editorconfig` | `dotnet new editorconfig` | Code style + analyzer severities |
| `.gitignore` | `dotnet new gitignore` | .NET ignore rules |
| `.gitattributes` | *(manual)* | Line endings / diff / binary rules |
| `.config/dotnet-tools.json` | `dotnet new tool-manifest` | Local tool manifest |

¹ If `dotnet new packagesprops` is unavailable on your SDK, create `Directory.Packages.props` manually with the template below.

## Init Steps

Run from an empty repo root. CLI templates create **skeletons** — then paste in the recommended contents from the sections below.

```bash
# 0) Prerequisite: .NET 10 SDK
dotnet --version            # expect >= 10.0.100

# 1) Solution (slnx) — named after the folder
mkdir MySolution && cd MySolution
dotnet new sln --format slnx

# 2) Pin the SDK
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature

# 3) Repo-wide build config
dotnet new buildprops       # Directory.Build.props
dotnet new buildtargets     # Directory.Build.targets

# 4) Central Package Management
dotnet new packagesprops    # Directory.Packages.props (or create manually)

# 5) Package sources + code style
dotnet new nugetconfig      # nuget.config
dotnet new editorconfig     # .editorconfig

# 6) Git config
dotnet new gitignore        # .gitignore
#    .gitattributes has no CLI template — create it from the template below

# 7) Local tool manifest
dotnet new tool-manifest    # .config/dotnet-tools.json
# e.g. dotnet tool install csharpier

# ── ASK the user: will this solution use Aspire? ──────────────────────────
#    If YES → set it up now (see the "Aspire" section): install the Aspire CLI
#    as a local tool, then `aspire init` (existing dir) or `aspire new`.
#    If NO  → continue below.

# 8) Create projects and add them to the solution (src / tests split)
dotnet new classlib -o src/MyApp.Core
dotnet new nunit    -o tests/MyApp.Core.Tests
dotnet sln add src/MyApp.Core tests/MyApp.Core.Tests

# 9) Verify
dotnet build
```

> Add later projects to the solution the same way: `dotnet sln add <path-to-csproj>`.
> Migrate an existing `.sln` instead of recreating it: `dotnet sln migrate`.

## Folder Layout

Pick **one** of the two layouts and keep it consistent. All repo-wide config lives at the **root** so every project inherits it.

### Option A — `src` / `tests` split (recommended default)

Best for anything that ships, has tests, or will grow beyond one project.

```
📁 MySolution/
├── MySolution.slnx
├── global.json
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── nuget.config
├── .editorconfig
├── .gitignore
├── .gitattributes
├── 📁 .config/
│   └── dotnet-tools.json
├── 📁 src/
│   ├── 📁 MyApp.Core/
│   │   └── MyApp.Core.csproj
│   └── 📁 MyApp.Api/
│       └── MyApp.Api.csproj
└── 📁 tests/
    └── 📁 MyApp.Core.Tests/
        └── MyApp.Core.Tests.csproj
```

### Option B — Flat

Best for a single small project / throwaway tool, where `src`/`tests` ceremony adds no value.

```
📁 MyApp/
├── MyApp.slnx
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── .editorconfig
├── .gitignore
├── .gitattributes
├── 📁 MyApp/
│   └── MyApp.csproj
└── 📁 MyApp.Tests/
    └── MyApp.Tests.csproj
```

| | Option A `src`/`tests` | Option B Flat |
|---|---|---|
| Use when | Multi-project, shipping, layered | Single project, scripts, demos |
| Test projects | Under `tests/`, named `<Project>.Tests` | Sibling `<Project>.Tests` |
| Grows well | ✅ | ❌ (migrate to A) |

> **Layered variant of A:** under `src/`, split by responsibility — `MyApp.Domain`, `MyApp.Application`, `MyApp.Infrastructure`, `MyApp.Api` — for DDD / Clean Architecture. Keep the same root config.

## Aspire (ask first)

**Before scaffolding projects, ASK the user whether the solution will use [Aspire](https://aspire.dev)** — local orchestration of multi-service apps with a dashboard. Don't assume; only set it up on an explicit yes.

If **yes**, install the Aspire CLI as a **local tool** (reuses the tool-manifest from step 7), then initialize:

```bash
dotnet tool install aspire.cli      # pinned into .config/dotnet-tools.json
dotnet tool restore                 # restore the manifest

aspire init                         # add Aspire to the current solution
#   or: aspire new                  # scaffold a new Aspire solution from a template
```

- `aspire init` generates a file-based AppHost (`AppHost.cs` + `apphost.run.json`). Aspire 13 targets `net10.0` and needs **no** workload.
- **Requires** a container runtime (Docker Desktop or Podman) to run locally.
- Installing via `dotnet tool` (not the global `aspire.dev/install.sh` script) keeps the CLI version pinned per-repo and restorable by every contributor (`dotnet tool restore`).

### Common Aspire commands

| Command | Purpose |
|---|---|
| `aspire run` | Run the AppHost and open the dashboard |
| `aspire add <integration>` | Add an integration (e.g. `redis`, `postgres`) |
| `aspire update` | Upgrade Aspire to the latest version (CPM-aware) |
| `aspire deploy` | Deploy to Azure Container Apps |

### Document it in the README

When Aspire is used, add this section to the repo `README.md`:

````markdown
## Aspire

This solution uses [Aspire](https://aspire.dev) for local orchestration.

### Prerequisites
- .NET 10 SDK
- A container runtime (Docker Desktop or Podman)

### Setup & run
```bash
dotnet tool restore     # restore the pinned Aspire CLI
aspire run              # start the AppHost + dashboard
```

### Useful commands
| Command | Purpose |
|---|---|
| `aspire run` | Run locally with the dashboard |
| `aspire add <integration>` | Add an integration (redis, postgres, …) |
| `aspire update` | Upgrade Aspire |
| `aspire deploy` | Deploy to Azure Container Apps |
````

## Config Templates

CLI templates generate minimal skeletons. Replace their contents with these.

### `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

- `version` must be a **full** version (`10.0.100`), not `10`, `10.0`, or `10.0.x`.
- `rollForward`: `latestFeature` = same `major.minor`, allow newer feature band/patch. CI that must be byte-reproducible: use `disable` together with a lock file.

### `Directory.Build.props` (imported first, top-down)

```xml
<Project>
  <PropertyGroup>
    <!-- Target & language -->
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Quality gates -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- Reproducible builds -->
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

- Setting `TargetFramework` here makes every project net10.0. To let a project override it, move that line out into the individual `.csproj`.
- `AnalysisLevel` can be pinned (e.g. `10.0`) to freeze analyzer rules when the SDK upgrades.

### `Directory.Build.targets` (imported last, bottom-up)

Use for things that must run **after** each project is defined — e.g. injecting a repo-wide analyzer package (version controlled by CPM).

```xml
<Project>
  <!-- Imported after every project's own content. Props go in Directory.Build.props;
       targets / late ItemGroup additions go here. -->
  <ItemGroup>
    <!-- Example: enforce an analyzer across the whole repo
    <PackageReference Include="Meziantou.Analyzer" PrivateAssets="all" />
    -->
  </ItemGroup>
</Project>
```

### `Directory.Packages.props` (Central Package Management)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- All versions live here; projects reference WITHOUT a Version -->
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageVersion Include="NUnit" Version="4.2.2" />
  </ItemGroup>
</Project>
```

In each `.csproj`, omit the version:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Hosting" />
</ItemGroup>
```

- CPM rule: a `<PackageReference>` with a `Version` under CPM is an error (NU1008). Versions belong on `<PackageVersion>`.
- Need a one-off different version: `<PackageReference Include="X" VersionOverride="3.0.0" />`.

### `nuget.config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <!-- Source mapping hardens against dependency-confusion attacks -->
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

### `.gitattributes` (no CLI template — create manually)

```gitattributes
# Normalize to LF on commit; check out as-is
* text=auto eol=lf

# Keep CRLF where tooling expects it
*.{cmd,bat} text eol=crlf
*.sln text eol=crlf

# slnx is plain XML — LF is fine
*.slnx text eol=lf

# Better C# diffs
*.cs text diff=csharp

# Binaries
*.{png,jpg,gif,ico,snk,dll,exe,pdf} binary
```

### `.editorconfig`

Generate the .NET defaults with `dotnet new editorconfig`, then add `root = true` at the top and raise the rules you want enforced. Minimum recommended additions:

```ini
root = true

[*.cs]
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0005.severity = warning   # remove unnecessary usings

[*.{json,yml,yaml,csproj,props,targets,slnx}]
indent_size = 2
```

Full opinionated `.editorconfig` (naming rules, analyzer severities): see `references/config-templates.md`.

### `.config/dotnet-tools.json` (local tools)

```bash
dotnet new tool-manifest
dotnet tool install csharpier        # example formatter
dotnet tool restore                  # contributors run this after clone
```

Commit the manifest so the whole team uses the same tool versions.

## Rules

1. **One solution format**: `.slnx` (XML). Don't keep both `.sln` and `.slnx`.
2. **All repo-wide config at the root** so every project inherits it; never duplicate `TargetFramework`/`Nullable` per project unless overriding on purpose.
3. **Central Package Management is on**: versions live only in `Directory.Packages.props`; `<PackageReference>` never carries a `Version`.
4. **Pin the SDK** with `global.json` and commit it — local and CI use the same SDK.
5. **Warnings are errors** (`TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`) so style/analyzer issues fail the build, not review.
6. **Pick one folder layout** (Option A by default) and keep test projects named `<Project>.Tests`.
7. **`props` vs `targets`**: shared properties → `Directory.Build.props`; shared targets / late items → `Directory.Build.targets`.
8. **Commit the tool manifest**; contributors run `dotnet tool restore`.
9. **Ask about Aspire** before scaffolding — never assume. If the solution uses it, install `aspire.cli` as a local tool, run `aspire init` / `aspire new`, and document usage in the README.

## Cheat Sheet

| Topic | Rule |
|---|---|
| Solution | `dotnet new sln --format slnx`; add projects via `dotnet sln add` |
| SDK pin | `global.json` with full version + `rollForward: latestFeature` |
| Shared props | `Directory.Build.props` (TFM, Nullable, analyzers, warnings-as-errors) |
| Shared targets | `Directory.Build.targets` (repo-wide analyzers, late items) |
| Package versions | Only in `Directory.Packages.props`; refs are version-less |
| One-off version | `VersionOverride` on the `<PackageReference>` |
| Line endings | `.gitattributes` `* text=auto eol=lf` |
| Folder layout | `src/` + `tests/` (default) or flat for single project |
| Aspire | ASK first; if yes: `dotnet tool install aspire.cli` → `aspire init`, document in README |

## Notes

- `dotnet new sln` defaults to `.slnx` since .NET 10; pass `--format sln` only if you need the legacy format.
- `Directory.Build.props`/`.targets` and `Directory.Packages.props` are auto-imported from the project folder or any ancestor — they don't need an explicit `<Import>`.
- For full `.editorconfig`, artifacts output layout, and CI pinning notes, see `references/config-templates.md`.
- To run dotnet CLI operations, use the `dotnet-runner` agent.
