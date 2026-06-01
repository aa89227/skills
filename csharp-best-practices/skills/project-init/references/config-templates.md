# Project Init — Long-form Config Templates

Long or situational templates that the main `SKILL.md` references. Copy what you need.

## Full `.editorconfig`

A practical, opinionated baseline for .NET 10. Start from `dotnet new editorconfig` and merge these in. Severities use `:warning` / `:error` so they are enforced when `EnforceCodeStyleInBuild` is on.

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

[*.cs]
indent_size = 4

# --- Language / style preferences ---
csharp_style_namespace_declarations = file_scoped:warning
csharp_using_directive_placement = outside_namespace:warning
csharp_prefer_braces = true:warning
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# --- "this." not required ---
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning

# --- Modern C# nudges ---
dotnet_style_prefer_collection_expression = true:suggestion
csharp_style_prefer_primary_constructors = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_coalesce_expression = true:warning
dotnet_style_null_propagation = true:warning

# --- Analyzer severities ---
dotnet_diagnostic.IDE0005.severity = warning   # remove unnecessary usings
dotnet_diagnostic.CA2007.severity = none        # ConfigureAwait — off for app code

# --- Naming: private fields _camelCase ---
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_prefix
dotnet_naming_rule.private_fields_underscore.severity = warning
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_prefix.capitalization = camel_case
dotnet_naming_style.underscore_prefix.required_prefix = _

# --- Naming: interfaces start with I ---
dotnet_naming_rule.interfaces_i.symbols = interfaces
dotnet_naming_rule.interfaces_i.style = i_prefix
dotnet_naming_rule.interfaces_i.severity = warning
dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_style.i_prefix.capitalization = pascal_case
dotnet_naming_style.i_prefix.required_prefix = I

# Config / markup files use 2-space indent
[*.{json,yml,yaml,xml,csproj,props,targets,slnx,config}]
indent_size = 2
```

> Tune `CA2007` per project: enable it for libraries, leave it off for app/host code.

## Artifacts output layout

Collect all build output under a single root `artifacts/` folder instead of per-project `bin/obj`.

Either scaffold with the flag:

```bash
dotnet new buildprops --use-artifacts
```

…or set it in `Directory.Build.props`:

```xml
<PropertyGroup>
  <UseArtifactsOutput>true</UseArtifactsOutput>
</PropertyGroup>
```

Add `artifacts/` to `.gitignore`.

## CI / reproducible-build pinning

For builds that must restore the exact same graph every time (CI, release):

`global.json` — lock the SDK exactly:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "disable"
  }
}
```

`Directory.Build.props` (or per project) — lock packages and analyzer rules:

```xml
<PropertyGroup>
  <!-- Fail restore if the lock file is stale -->
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  <RestoreLockedMode Condition="'$(CI)' == 'true'">true</RestoreLockedMode>

  <!-- Freeze analyzer rules across SDK upgrades -->
  <AnalysisLevel>10.0</AnalysisLevel>

  <!-- Deterministic / source-correct paths on CI -->
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

- Run `dotnet restore` once to generate `packages.lock.json`, then commit it.
- With lock files, prefer `rollForward: disable` so the SDK and dependency graph stay in lockstep.
- `RestoreLockedMode=true` makes restore fail (instead of updating the lock file) when dependencies drift — exactly what CI wants.
