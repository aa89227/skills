---
name: csharp-best-practices
description: Authoring rules and deeper notes for the C# best-practices skill (non-duplicated). See SKILL.md for the canonical examples.
license: MIT
metadata:
  author: aa89227
  version: "2.1"
  tags: ["csharp", "dotnet", "best-practices", "authoring"]
---

# C# Best Practices (Maintenance)

This file intentionally contains only what is NOT already in `SKILL.md`: maintenance rules, annotation conventions, and deeper rationale.

## Single Source of Truth

- `SKILL.md` is the canonical source for examples and rules.
- If something is already in `SKILL.md`, do NOT duplicate it here.
- Add only rationale, pitfalls, compatibility notes, and links.

## Version Scope

- Primary target: C# 14 / .NET 10.
- Keep C# 12 and C# 13 key features, but examples live in `SKILL.md`.

## Annotation Conventions

- Version tags: `[V12]`, `[V13]`, `[V14]` (use `V12+` when it remains valid).
- Topic tags (multi-select when useful):
  - `LANG` language features
  - `ASYNC` async
  - `COLL` collections
  - `NULL` null safety
  - `DI` dependency injection
  - `ERR` error handling
  - `PERF` performance
  - `SEC` security
  - `TEST` testing

## Document Growth Control

- `SKILL.md`
  - Keep high-density, copy/paste friendly rules and examples.
  - Prioritize: `required init`, `IReadOnlyList<T>`, `nameof(Foo<>.Member)`, XML docs (generic `cref` uses `{}`), collection expressions, raw string literals, and C# 12/13/14 features.
- `BEST-PRACTICES.md`
  - Keep the "why" and "gotchas": compatibility, overload resolution changes, Lock vs Monitor semantics, span rules, etc.

## High-value Pitfalls (No duplicate examples)

- C# 12 collection expressions: overload resolution behavior changes in C# 13/14; adding `Span<T>`/`ReadOnlySpan<T>` overloads can change which method binds or introduce ambiguity.
- C# 13 `System.Threading.Lock`: `lock (Lock)` uses the new semantics; but upcasting `Lock` to `object` before locking falls back to `Monitor`.
- C# 14 first-class spans / overload resolution (the `Contains` issue):
  - C# 14 makes span overloads applicable in more scenarios and prefers `ReadOnlySpan<T>` to reduce covariant-array `ArrayTypeMismatchException` risk.
  - Impact: calls like `array.Contains(x)` (and even `array.Reverse()`) may bind to `System.MemoryExtensions.*` instead of `System.Linq.Enumerable.*`.
  - Expression trees / LINQ providers (EF Core, Queryable, custom visitors) may fail because they expect `Enumerable.Contains` but see `MemoryExtensions.Contains`. Also `Expression<T>.Compile(preferInterpretation: true)` can fail at runtime because spans are `ref struct`.
  - Recommendation for translatable/stable trees: call `Enumerable.Contains(array, x)` explicitly, or use `array.AsEnumerable().Contains(x)` / cast to `IEnumerable<T>`.

## Official References

- C# 14: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14`
- C# 13: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13`
- C# 12: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12`
- C# version history: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history`
- Roslyn / .NET 10 breaking changes: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2010#%60span%3Ct%3E%60-and-%60readonlyspan%3Ct%3E%60-overloads-are-applicable-in-more-scenarios-in-c-14-and-newer`
- C# 14 overload resolution with span parameters: `https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution`
- First-class span types (proposal details): `https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/first-class-span-types`
