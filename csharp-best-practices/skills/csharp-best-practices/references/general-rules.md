# C# General Rules & Pitfalls

## Required Init Pattern

Prefer `required init` for properties that must be provided at construction time.
Avoid using empty strings or sentinel values as "uninitialized" markers.

```csharp
// Correct
public required string Name { get; init; }

// Avoid
public string Name { get; set; } = "";
```

## Read-only Intent

Prefer `IReadOnlyList<T>` / `IReadOnlyCollection<T>` for parameters and return types
to communicate that the caller should not modify the collection.

```csharp
public IReadOnlyList<string> GetTags() => _tags.ToList();
public void Process(IReadOnlyList<int> ids) { ... }
```

## Magic Strings

Centralize string constants. Use `const`, `static readonly`, `enum`, or `nameof(...)`.

```csharp
// Correct
public const string DefaultRole = "user";
_logger.LogError("Failed {Method}", nameof(GetByIdAsync));

// Avoid
if (role == "user") { ... }
_logger.LogError("Failed GetByIdAsync");
```

## XML Documentation

Document non-`private` APIs with `summary`, `param`, `returns`, `exception`, `example`.
In XML docs, generic `cref` uses `{}`: `<see cref="IReadOnlyList{T}"/>`.
In code, generics use `<>`.

## Guard Methods (Argument Validation)

Prefer static throw helpers over manual checks:

```csharp
ArgumentNullException.ThrowIfNull(service);
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 1000);
ArgumentException.ThrowIfNullOrWhiteSpace(name);
```

## Async Best Practices

- Always pass `CancellationToken` through async call chains.
- Use `ConfigureAwait(false)` in library code.
- Catch `OperationCanceledException` specifically with `when (ct.IsCancellationRequested)`.
- Avoid `async void` — use `async Task` instead.
- Prefer `ValueTask` for hot paths that frequently complete synchronously.

## Primary Constructor Pitfalls

Captured parameters are **not** fields — they may be captured multiple times:

```csharp
// Careful: if service is mutable, multiple captures can diverge
public sealed class MyController(IService service)
{
    // If needed as a field, assign explicitly:
    private readonly IService _service = service;
}
```

## C# 14 Span Overload Resolution

C# 14 span overload resolution can bind `array.Contains(x)` to
`MemoryExtensions.Contains` instead of `Enumerable.Contains`.
This breaks Expression trees / LINQ providers.

**Fix:** Call `Enumerable.Contains(array, x)` explicitly when a
stable/translatable tree is needed.

## record vs record struct

| Feature | `record` (class) | `record struct` |
|---|---|---|
| Allocation | Heap | Stack |
| Default equality | Reference + value | Value |
| Mutability | Immutable by default | Mutable by default |
| `with` expression | Yes (shallow copy) | Yes (copy) |
| Inheritance | Yes | No |

Prefer `readonly record struct` for small, value-semantic DTOs:

```csharp
public readonly record struct Point(double X, double Y);
public readonly record struct ApiResult<T>(T? Data, bool Success, string? Error);
```
