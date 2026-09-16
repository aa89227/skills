---
name: csharp-best-practices
description: |
  This skill should be used when writing or reviewing C# code, asking about modern C# syntax,
  async/await patterns, LINQ best practices, record/required patterns, pattern matching,
  C# dependency installation, or C# 12/13/14 language features. Trigger phrases: "write C# code", "review C#",
  "C# best practice", "async pattern", "pattern matching", "C# 14 feature",
  "extension members", "field keyword", "collection expressions", "NuGet package version",
  "install C# dependency".
license: MIT
metadata:
  author: aa89227
  version: "3.1"
  tags: ["csharp", "dotnet", "best-practices", "language-reference"]
  trigger_keywords: ["C#", "csharp", "dotnet", "async", "LINQ", "record", ".cs", "pattern matching", "NuGet", "package version"]
---

# C# Best Practices

## Instruction Retention

Read this file when this skill is first activated for the current task. Once read, retain and
follow its instructions without rereading the file on every turn or before every action.

Reread it only when the file may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

**Latest version:** C# 14 (.NET 10, Nov 2025)

## Core Rules

- Prefer `required init` for must-provide data; avoid empty strings as "uninitialized".
- Prefer `IReadOnlyList<T>` / `IReadOnlyCollection<T>` for parameters/returns to express read-only intent.
- Avoid magic strings: centralize as `const` / `static readonly` / `enum` / `nameof(...)`.
- Document non-`private` APIs with XML docs (`summary/param/returns/exception`).
- In XML docs, generic `cref` uses `{}` (e.g., `<see cref="IReadOnlyList{T}"/>`).
- Use static guard methods: `ArgumentNullException.ThrowIfNull()`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero()`.
- Always propagate `CancellationToken` through async chains; use `ConfigureAwait(false)` in libraries.
- Avoid `async void`; prefer `async Task` or `async ValueTask`.
- Primary constructor parameters are captures, **not** fields — assign to a field when mutation or multiple access is needed.
- C# 14 span overload resolution may bind `array.Contains(x)` to `MemoryExtensions.Contains` — call `Enumerable.Contains()` explicitly in Expression trees.

## Dependency Installation

When a task introduces a NuGet package or a .NET tool, resolve its latest stable release from the
configured online package feed at task time. Do not invent a version, copy one from memory, or copy
one from this skill, another example, or an older project.

- For .NET 10+, use `dotnet package add <PackageId> --project <path-to-project>` without
  `--version`; for older SDKs use `dotnet add <path-to-project> package <PackageId>` without a
  version option.
- On the initial add, allow the command's restore to run; do not add `--no-restore` or rely only on
  cached packages when the goal is to resolve the current release from the feed.
- For a .NET tool, use `dotnet tool install <ToolId>` without `--version` (prefer a local tool
  manifest when the tool belongs to the repository).
- Let the CLI/package manager update the project, `Directory.Packages.props`, or tool manifest.
  A resolved version written by the tool is acceptable; manually typing a remembered
  `Version="..."`, `<PackageVersion ...>`, `VersionOverride`, or `@<version>` is not.
- Do not hand-edit a `.csproj` merely to add a new dependency; the project-file change should be
  produced by the package tooling after it resolves the feed.
- If a package family must use aligned versions, query the current releases for every member and
  choose the latest compatible set before restoring/building. Do not reuse an old set merely
  because it appears in a sample.
- If the configured feed cannot be reached, report that the latest version could not be resolved;
  do not silently substitute a remembered version. Honor an explicit user or repository pin when
  one already exists.

## C# 14 New Features Summary

| Feature | Example | Notes |
|---|---|---|
| Extension members | `extension(int n) { bool IsEven() => ...; }` | Properties, methods, static, operators |
| `field` keyword | `set => field = value ?? throw ...` | No manual backing field needed |
| Null-conditional assignment | `customer?.Order = new()` | `?.` on LHS of assignment |
| Partial constructors/events | `partial MyClass(int x);` | Split across files |
| User-defined `+=` / `-=` | `void operator +=(int v) => ...` | Compound assignment operators |
| Unbound generic `nameof` | `nameof(List<>)` → `"List"` | Works on open generic types |

## C# 12/13 Key Features

| Feature | Version | Example |
|---|---|---|
| Primary constructors | 12 | `class Svc(IDep dep) { }` |
| Collection expressions | 12 | `int[] a = [1, 2, .. other];` |
| Default lambda params | 12 | `var f = (int x, int y = 1) => x + y;` |
| `ref readonly` params | 12 | `void M(ref readonly int x)` |
| `params` collections | 13 | `void M(params ReadOnlySpan<int> v)` |
| `System.Threading.Lock` | 13 | `lock (new Lock()) { }` |
| `\e` escape | 13 | ESC character (U+001B) |
| `[^n]` in initializers | 13 | `Buffer = { [^1] = 0 }` |
| Raw string literals | 11 | `$$"""{ "id": {{id}} }"""` |
| List patterns | 11 | `[first, .., last]` |

## Pattern Matching Quick Reference

```csharp
// Switch expression
string label = score switch
{
    >= 90 => "A",
    >= 80 => "B",
    _ => "F"
};

// Property pattern
if (order is { Total: > 1000, IsPriority: true })
    Console.WriteLine("VIP Rush");

// List pattern (C# 11)
if (items is [var first, .., var last])
    Console.WriteLine($"{first}..{last}");

// Type + logical pattern
if (value is int x and > 0)
    Console.WriteLine($"Positive: {x}");
```

## IAsyncEnumerable Quick Reference

```csharp
// Produce
async IAsyncEnumerable<int> GenerateAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    for (var i = 0; i < 100; i++)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(10, ct);
        yield return i;
    }
}

// Consume
await foreach (var item in GenerateAsync(ct))
    Console.WriteLine(item);
```

## Cheat Sheet

| Topic | Practice | Since |
|---|---|---:|
| Required data | `required init` | C# 11 |
| Read-only APIs | `IReadOnlyList<T>` | C# 1.0 |
| Avoid magic strings | `const` / `nameof` | C# 1.0 |
| XML docs | `{}` in generic `cref` | C# 1.0 |
| Guard methods | `ArgumentNullException.ThrowIfNull()` | .NET 6 |
| Collections | `[1, 2, .. spread]` | C# 12 |
| Primary constructors | `class Svc(IDep dep)` | C# 12 |
| Templates | `$$"""raw string"""` | C# 11 |
| Pattern matching | `switch`, property, list, relational | C# 8-11 |
| Async streaming | `IAsyncEnumerable<T>` + `await foreach` | C# 8 |
| `params` span | `params ReadOnlySpan<T>` | C# 13 |
| Typed lock | `System.Threading.Lock` | C# 13 |
| Extension members | `extension(T) { ... }` | C# 14 |
| `field` keyword | `set => field = ...` | C# 14 |
| Null-conditional `=` | `obj?.Prop = val` | C# 14 |
| `record struct` | `readonly record struct P(int X)` | C# 10 |
| `record` vs `record struct` | class=heap, struct=stack | C# 10 |

## Additional Resources

### Reference Files

For detailed rules, pitfalls, and comparisons:
- **`references/general-rules.md`** — Guard methods, async best practices, primary constructor pitfalls, `record` vs `record struct`, span overload resolution
- **`BEST-PRACTICES.md`** — Rationale and design decisions behind each rule

### Example Files

Complete, runnable `.cs` examples in `examples/`:
- **`examples/core-example.cs`** — High-density example: required init, IReadOnlyList, XML docs, async/await, CancellationToken, list patterns, Parallel.ForEachAsync, record, record struct
- **`examples/csharp14-features.cs`** — Extension members, partial constructors/events, compound assignment, null-conditional assignment, field keyword, unbound generic nameof
- **`examples/feature-highlights.cs`** — C# 12/13 features: primary constructors, collection expressions, default lambda params, params collections, Lock, raw string literals, IAsyncEnumerable
- **`examples/pattern-matching.cs`** — Switch expression, property/relational/list/tuple/type patterns, pattern in if/is
