---
name: file-based-apps
description: |
  Use when writing or reviewing C# file-based apps (.NET 10+): single-file programs without .csproj,
  #: directives (#:package, #:sdk, #:property, #:project), dotnet run file.cs, shebang scripts,
  Native AOT publishing, packaging as .NET tools.
  Trigger phrases: "file-based app", "single file C#", "dotnet run .cs", "#:package", "shebang C#",
  "dotnet file", "script C#", "no csproj".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["csharp", "dotnet", "file-based-apps", "scripting", "dotnet10"]
  trigger_keywords: ["file-based", "#:package", "#:sdk", "#:property", "dotnet run", "shebang", "single-file"]
---

## Auto-Trigger Scenarios

This skill activates when:
- User writes or runs a single `.cs` file without a `.csproj`
- User uses `#:` directives (`#:package`, `#:sdk`, `#:property`, `#:project`)
- User asks about scripting with C# or lightweight app setup
- User wants to publish a single-file C# app or package it as a .NET tool

# File-based Apps (.NET 10+)

## Quick Reference

**Requires:** .NET 10 SDK+
**Default SDK:** `Microsoft.NET.Sdk`
**Default publish:** Native AOT enabled
**Default pack:** `PackAsTool=true`

## Supported Directives

All `#:` directives must be placed at the **top** of the file, before any C# code.

| Directive | Purpose | Example |
|---|---|---|
| `#:package` | Add NuGet package reference | `#:package Newtonsoft.Json@13.0.3` |
| `#:sdk` | Specify SDK (default: `Microsoft.NET.Sdk`) | `#:sdk Microsoft.NET.Sdk.Web` |
| `#:property` | Set MSBuild property | `#:property TargetFramework=net10.0` |
| `#:project` | Reference another project | `#:project ../Shared/Shared.csproj` |

## Core Examples

### Minimal file-based app

```csharp
Console.WriteLine("Hello from file-based app!");
```

```shell
dotnet run hello.cs
```

### With package references

```csharp
#:package Spectre.Console@*

using Spectre.Console;

AnsiConsole.MarkupLine("[green]Hello[/] from [blue]file-based app[/]!");
```

### Web app (SDK override)

```csharp
#:sdk Microsoft.NET.Sdk.Web

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello from file-based web app!");

app.Run();
```

### Aspire AppHost

```csharp
#:sdk Aspire.AppHost.Sdk@13.0.2

var builder = DistributedApplication.CreateBuilder(args);
builder.Build().Run();
```

### With MSBuild properties

```csharp
#:property TargetFramework=net10.0
#:property PublishAot=false
#:property LangVersion=preview

Console.WriteLine("Custom properties set.");
```

### Conditional property (environment variable with default)

```csharp
#:property LogLevel=$([MSBuild]::ValueOrDefault('$(LOG_LEVEL)', 'Information'))

Console.WriteLine("App starting...");
```

### Unix shebang script

```csharp
#!/usr/bin/env dotnet
#:package Spectre.Console

using Spectre.Console;

AnsiConsole.MarkupLine("[green]Hello, World![/]");
```

```shell
chmod +x script.cs
./script.cs
```

> **Note:** Use `LF` line endings (not `CRLF`). Do not include a BOM.

## CLI Commands

| Command | Description |
|---|---|
| `dotnet run file.cs` | Run the file-based app |
| `dotnet run --file file.cs` | Explicit file mode (use when `.csproj` exists in cwd) |
| `dotnet file.cs` | Shorthand syntax |
| `dotnet build file.cs` | Build without running |
| `dotnet publish file.cs` | Publish (Native AOT by default) |
| `dotnet pack file.cs` | Package as .NET tool |
| `dotnet restore file.cs` | Restore NuGet packages explicitly |
| `dotnet project convert file.cs` | Convert to traditional `.csproj` project |

### User secrets

```shell
dotnet user-secrets set "ApiKey" "your-secret-value" --file file.cs
dotnet user-secrets list --file file.cs
```

A stable user secrets ID is auto-generated based on a hash of the full file path.

## Launch Profiles

Use `[AppName].run.json` (flat file, same directory) instead of `Properties/launchSettings.json`:

```
📁 myapps/
├── foo.cs
├── foo.run.json
├── bar.cs
└── bar.run.json
```

## Default Behaviors

| Behavior | Default | Override |
|---|---|---|
| Native AOT publishing | Enabled | `#:property PublishAot=false` |
| Pack as tool | Enabled | `#:property PackAsTool=false` |
| SDK | `Microsoft.NET.Sdk` | `#:sdk Microsoft.NET.Sdk.Web` |
| Implicit restore | On build/run | `--no-restore` flag |
| Publish output | `artifacts/` next to `.cs` | `--output <path>` |

## Implicit Build Files

File-based apps respect these files from parent directories:

- `Directory.Build.props` — inherited MSBuild properties
- `Directory.Packages.props` — central package management (allows omitting version in `#:package`)
- `global.json` — .NET SDK version selection

## Folder Layout Rules

### Do: Keep file-based apps outside project directories

```
✅ Recommended:
📁 MyProject/
├── MyProject.csproj
└── Program.cs
📁 scripts/
└── utility.cs          ← file-based app here
```

### Don't: Place inside a .csproj directory tree

```
❌ Not recommended:
📁 MyProject/
├── MyProject.csproj
├── Program.cs
└──📁 scripts/
    └── utility.cs      ← project settings will interfere
```

### Isolate build configurations

```
✅ Recommended:
📁 repo/
├── Directory.Build.props
├──📁 projects/
│   └── MyProject.csproj
└──📁 scripts/
    ├── Directory.Build.props  ← isolated config
    ├── app1.cs
    └── app2.cs
```

## Cheat Sheet

| Topic | Rule |
|---|---|
| Directives placement | Top of file, before any C# code |
| Package version | Specify explicitly (`@3.1.1`) or use `@*` for latest; omit only with central package management |
| Shebang | `#!/usr/bin/env dotnet` — first line, LF endings, no BOM |
| Avoid csproj cone | Never nest file-based apps inside a `.csproj` project directory |
| Convert to project | `dotnet project convert file.cs` when complexity grows |
| Web/Aspire | Use `#:sdk` to switch SDK |
| Secrets | `dotnet user-secrets set/list --file file.cs` |

## Notes

- File-based apps are a C# 14 / .NET 10 feature.
- The compiler ignores `#:` and `#!` directives; the build system processes them.
- In project-based compilation, `#:` directives generate warnings.
- `dotnet run file.cs` without `--file` will prefer an existing `.csproj` in cwd (backward compat).
