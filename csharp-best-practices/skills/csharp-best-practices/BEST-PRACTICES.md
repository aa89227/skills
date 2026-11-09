---
name: csharp-best-practices
description: Authoring rules and deeper notes for the C# best-practices skill (non-duplicated). See SKILL.md for the canonical examples.
license: MIT
metadata:
  author: aa89227
  version: "2.2"
  tags: ["csharp", "dotnet", "best-practices", "authoring"]
---

# C# Best Practices (Maintenance)

This file intentionally contains only what is NOT already in `SKILL.md`: maintenance rules, annotation conventions, and deeper rationale.

## Single Source of Truth

- `SKILL.md` is the canonical source for examples and rules.
- If something is already in `SKILL.md`, do NOT duplicate it here.
- Add only rationale, pitfalls, compatibility notes, and links.

## Version Scope

- Default target: C# 14 / .NET 10.
- Conditional target: non-preview C# 15 features when the project targets `net11.0` with a
  compatible .NET 11 SDK and has no lower explicit `LangVersion`.
- Keep C# 12 and C# 13 key features, but examples live in `SKILL.md`.
- Do not treat C# 15 memory safety, `safe`, `unsafe(expression)`, or
  `updated-memory-safety-rules` as supported guidance; those require preview settings.

## Annotation Conventions

- Version tags: `[V12]`, `[V13]`, `[V14]`, `[V15]` (use `V12+` when it remains valid).
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

- C# 15 collection expression arguments: `with(...)` changes how the target collection is
  constructed. Use it for meaningful capacity/comparer/factory arguments, and verify that the
  target type and constructor semantics remain clear.
- C# 15 union types: exhaustive switching is valuable only when the case set is intentionally
  closed. Treat adding a case as an API and downstream source-compatibility change, and verify
  serialization/reflection behavior before using a union in a persisted or public wire model.
- C# 15 closed hierarchies: `closed` restricts direct derivation to the declaring assembly, but the
  restriction is not transitive. Mark intermediate descendants `closed` when the whole branch must
  remain exhaustively known.
- C# 15 extension indexers: an indexer can make an O(n) operation look like an ordinary indexed
  lookup. Keep the complexity, bounds, and enumeration behavior obvious; do not hide expensive
  access behind familiar indexing syntax.
- C# 15 labeled `break` / `continue`: use labels to express a clear nested-loop target, not to make
  deeply nested control flow acceptable. Prefer extracting a method when the loop still needs a
  paragraph of explanation.
- C# 15 non-virtual static interface members: this feature does not make instance default
  implementations, `static abstract`, `static virtual`, or protected interface members available
  on runtimes that do not support them. Verify the runtime requirement for the exact member kind.

- C# 12 collection expressions: overload resolution behavior changes in C# 13/14; adding `Span<T>`/`ReadOnlySpan<T>` overloads can change which method binds or introduce ambiguity.
- C# 13 `System.Threading.Lock`: `lock (Lock)` uses the new semantics; but upcasting `Lock` to `object` before locking falls back to `Monitor`.
- C# 14 first-class spans / overload resolution (the `Contains` issue):
  - C# 14 makes span overloads applicable in more scenarios and prefers `ReadOnlySpan<T>` to reduce covariant-array `ArrayTypeMismatchException` risk.
  - Impact: calls like `array.Contains(x)` (and even `array.Reverse()`) may bind to `System.MemoryExtensions.*` instead of `System.Linq.Enumerable.*`.
  - Expression trees / LINQ providers (EF Core, Queryable, custom visitors) may fail because they expect `Enumerable.Contains` but see `MemoryExtensions.Contains`. Also `Expression<T>.Compile(preferInterpretation: true)` can fail at runtime because spans are `ref struct`.
  - Recommendation for translatable/stable trees: call `Enumerable.Contains(array, x)` explicitly, or use `array.AsEnumerable().Contains(x)` / cast to `IEnumerable<T>`.

## Official References

- C# 14: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14`
- C# 15: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15`
- C# language version configuration: `https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version`
- .NET 11 overview: `https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview`
- C# 15 compiler breaking changes: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2011`
- C# 13: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13`
- C# 12: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12`
- C# version history: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history`
- Roslyn / .NET 10 breaking changes: `https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/breaking-changes/compiler%20breaking%20changes%20-%20dotnet%2010#%60span%3Ct%3E%60-and-%60readonlyspan%3Ct%3E%60-overloads-are-applicable-in-more-scenarios-in-c-14-and-newer`
- C# 14 overload resolution with span parameters: `https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution`
- First-class span types (proposal details): `https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/first-class-span-types`
