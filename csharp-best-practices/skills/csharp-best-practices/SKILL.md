---
name: csharp-best-practices
description: |
  Use when writing or reviewing C# code for: (1) Modern C# syntax guidance,
  (2) async/await patterns, (3) LINQ best practices, (4) record/required patterns.
  Trigger phrases: "write C# code", "review C#", "C# best practice", "async pattern".
license: MIT
metadata:
  author: aa89227
  version: "2.2"
  tags: ["csharp", "dotnet", "best-practices", "language-reference"]
  trigger_keywords: ["C#", "csharp", "dotnet", "async", "LINQ", "record", ".cs"]
---

## Auto-Trigger Scenarios

This skill activates when:
- User writes or reviews C# code
- User asks about C# patterns (async, LINQ, records)
- Files with `.cs` extension are in context

# C# Best Practices

## Quick Reference

**Latest version:** C# 14 (.NET 10, Nov 2025)

## General Rules

- Prefer `required init` for "must provide" data (avoid using empty strings as "uninitialized").
- Prefer `IReadOnlyList<T>` for parameters/returns to communicate read-only intent.
- Avoid magic strings: centralize as `const`/`static readonly`/`enum`, or use `nameof(...)`.
- For non-`private` APIs, prefer XML docs (`summary/param/returns/exception/example`).
- In XML docs, generic `cref` uses `{}` (example: `<see cref="IReadOnlyList{T}"/>`). In code, generics use `<>`.
- C# 14 span overload resolution can bind `array.Contains(x)` to `MemoryExtensions.Contains` (Expression trees / LINQ providers). When you need a stable/translatable tree, call `Enumerable.Contains(array, x)` explicitly (details in `BEST-PRACTICES.md`).

```csharp
/// <summary>
/// XML docs use { } for generics.
/// </summary>
/// <remarks>
/// Code uses < > for generics. C# 14 supports nameof on unbound generic members.
/// </remarks>
public static class Examples
{
    public static string MemberName = nameof(IReadOnlyList<>.Count);
}
```

## Core Example (High-density)

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyCompany.Api;

/// <summary>
/// User service demonstrating modern C# best practices.
/// </summary>
/// <typeparam name="TUser">A user model implementing <see cref="IUser"/>.</typeparam>
public sealed class UserService<TUser> where TUser : class, IUser
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService<TUser>> _logger;
    private readonly TimeSpan _defaultTimeout;

    /// <summary>
    /// API version.
    /// </summary>
    /// <remarks>
    /// Prefer <c>required init</c> for required configuration.
    /// </remarks>
    public required string ApiVersion { get; init; }

    /// <summary>
    /// Configuration items.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="IReadOnlyList{T}"/> to express read-only intent.
    /// </remarks>
    public required IReadOnlyList<string> ConfigurationItems { get; init; }

    /// <summary>
    /// nameof examples using unbound generic members (C# 14).
    /// </summary>
    public static (string Items, string Count) NameofExamples =>
        (nameof(UserService<>.ConfigurationItems), nameof(IReadOnlyList<>.Count));

    private string _message = string.Empty;

    /// <summary>
    /// Message.
    /// </summary>
    /// <remarks>
    /// C# 14: use <c>field</c> in accessors to validate assignments.
    /// </remarks>
    public string Message
    {
        get => _message;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Creates a new service.
    /// </summary>
    /// <param name="userRepository">Repository dependency.</param>
    /// <param name="logger">Logger dependency.</param>
    /// <param name="defaultTimeout">Default timeout (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when dependencies are null.</exception>
    public UserService(IUserRepository userRepository, ILogger<UserService<TUser>> logger, TimeSpan? defaultTimeout = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository), ErrorMessages.RepositoryCannotBeNull);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), ErrorMessages.LoggerCannotBeNull);
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Returns a read-only list of user DTOs.
    /// </summary>
    /// <param name="predicate">Filter predicate.</param>
    public IReadOnlyList<UserDto> QueryUsers(Func<UserDto, bool> predicate)
    {
        return _userRepository.ActiveUsers
            .Where(u => u.IsActive)
            .Select(MapToDto)
            .Where(predicate)
            .DistinctBy(u => u.Email)
            .OrderByDescending(u => u.LastLogin)
            .Take(NumericConstants.MaxPageSize)
            .ToList();
    }

    /// <summary>
    /// Fetches a user by id.
    /// </summary>
    /// <param name="id">User id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A DTO or <see langword="null"/>.</returns>
    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        try
        {
            var user = await _userRepository.FindByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return user is null ? null : MapToDto(user);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Operation cancelled");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch user {UserId}", id);
            throw new UserServiceException(string.Format(ErrorMessages.UserNotFound, id), ex);
        }
    }

    /// <summary>
    /// Batch-process users.
    /// </summary>
    /// <param name="userIds">Read-only ids.</param>
    public async Task<ImmutableArray<UserDto>> BatchProcessAsync(IReadOnlyList<int> userIds)
    {
        var ids = userIds.ToImmutableArray();

        // List patterns (C# 11)
        if (ids is [.., var lastId] && lastId > 0)
        {
            _logger.LogInformation("Processing {Count} users; last id {LastId}", ids.Length, lastId);
        }

        var results = new ConcurrentBag<UserDto>();

        await Parallel.ForEachAsync(ids, new ParallelOptions
        {
            CancellationToken = CancellationToken.None,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
        }, async (id, ct) =>
        {
            var dto = await GetByIdAsync(id, ct).ConfigureAwait(false);
            if (dto is not null)
                results.Add(dto);
        });

        return results.ToImmutableArray();
    }

    private static UserDto MapToDto(IUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        FullName = user.UserName,
        Tier = user.Tier,
        IsActive = user.IsActive,
        LastLogin = user.LastLogin,
        CreatedAt = user.CreatedAt,
    };

    /// <summary>
    /// Shared error messages.
    /// </summary>
    public static class ErrorMessages
    {
        public const string RepositoryCannotBeNull = "User repository cannot be null.";
        public const string LoggerCannotBeNull = "Logger cannot be null.";
        public const string UserNotFound = "User with ID {0} was not found.";
    }
}

/// <summary>
/// Service exception.
/// </summary>
public sealed class UserServiceException : Exception
{
    public UserServiceException(string message, Exception? innerException = null) : base(message, innerException) { }
}

/// <summary>
/// A user DTO.
/// </summary>
public record UserDto
{
    public required int Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required UserTier Tier { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastLogin { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// A common API result wrapper.
/// </summary>
/// <typeparam name="T">Result payload type.</typeparam>
public readonly record struct ApiResult<T>(T? Data, bool Success, string? ErrorMessage)
{
    public static ApiResult<T> Ok(T data) => new(data, true, null);
    public static ApiResult<T> Fail(string error) => new(default, false, error);
}

/// <summary>
/// User model contract.
/// </summary>
public interface IUser
{
    int Id { get; }
    string UserName { get; }
    string Email { get; }
    bool IsActive { get; }
    UserTier Tier { get; }
    DateTimeOffset? LastLogin { get; }
    DateTimeOffset CreatedAt { get; }
}

/// <summary>
/// User tier.
/// </summary>
public enum UserTier
{
    Standard = 0,
    Premium = 1,
    VIP = 2,
}

public interface IUserRepository
{
    IEnumerable<IUser> ActiveUsers { get; }
    Task<IUser?> FindByIdAsync(int id, CancellationToken cancellationToken);
}

public interface ILogger<T>
{
    void LogInformation(string message, params object[] args);
    void LogError(Exception exception, string message, params object[] args);
}

public static class NumericConstants
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
```

## C# 14: Extension Members

```csharp
using System;

public static class IntExtensions
{
    // Instance extensions
    extension (int n)
    {
        public bool IsEven() => n % 2 == 0;
        public bool IsOdd() => n % 2 != 0;
    }

    // Static extensions
    extension (int)
    {
        public static int Random(int min, int max) => new Random().Next(min, max);
    }
}
```

## C# 14: Partial Constructors / Events

```csharp
using System;

public partial class GateAttendance
{
    partial GateAttendance(int capacity);

    partial event Action<int> GateOpened;
}

public partial class GateAttendance
{
    partial GateAttendance(int capacity) : this() { }

    partial event Action<int> GateOpened
    {
        add { _gateOpened += value; }
        remove { _gateOpened -= value; }
    }

    private event Action<int>? _gateOpened;
}
```

## C# 14: User-defined Compound Assignment

```csharp
public sealed class Counter
{
    public int Value { get; private set; }

    public void operator +=(int increment) => Value += increment;
    public void operator -=(int decrement) => Value -= decrement;
}
```

## C# 14: Null-conditional Assignment

```csharp
public sealed class Customer
{
    public Order? CurrentOrder { get; set; }
}

public sealed class Order { }

public static class NullConditionalAssignmentExample
{
    public static Order GetCurrentOrder() => new();

    public static void Assign(Customer? customer)
    {
        customer?.CurrentOrder = GetCurrentOrder();
    }
}
```

## C# 12/13/14 Feature Highlights

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

public static class FeatureHighlights
{
    // C# 12: primary constructors (common DI style)
    public sealed class ExampleController(IService service)
    {
        public int GetValue() => service.GetValue();
    }

    public interface IService
    {
        int GetValue();
    }

    // C# 12: default lambda parameters
    public static int DefaultLambdaParametersExample()
    {
        var incrementBy = (int source, int increment = 1) => source + increment;
        return incrementBy(5) + incrementBy(5, 2);
    }

    // C# 12: ref readonly parameters
    public static int Add(ref readonly int x, int y) => x + y;

    // C# 12: experimental attribute
    [Experimental("SKILL0001")]
    public static void ExperimentalApi() { }

    // C# 13: params collections (ReadOnlySpan<T> / interfaces)
    public static int Sum(params ReadOnlySpan<int> values)
    {
        var total = 0;
        foreach (var v in values)
            total += v;
        return total;

        // Usage:
        // _ = Sum(1, 2, 3);
        // _ = Sum([1, 2, 3]); // collection expression
    }

    // C# 13: new lock semantics with System.Threading.Lock
    public static int ThreadSafeIncrement(ref int counter)
    {
        var gate = new Lock();
        lock (gate)
        {
            counter++;
            return counter;
        }
    }

    // C# 13: escape sequence \e (ESC, U+001B)
    public static char EscapeChar() => '\e';

    // C# 13: implicit indexer access (^ from-the-end) in object initializers
    public static int[] ImplicitIndexerAccessExample()
    {
        var timer = new TimerRemaining
        {
            Buffer =
            {
                [^1] = 0,
                [^2] = 1,
                [^3] = 2,
            }
        };

        return timer.Buffer;
    }

    public sealed class TimerRemaining
    {
        public int[] Buffer { get; } = new int[10];
    }

    // C# 12: collection expressions + spread
    public static IReadOnlyList<int> CollectionExpressionsExample()
    {
        IReadOnlyList<int> baseIds = [1, 2, 3];
        int[] allIds = [.. baseIds, 4, 5];
        return allIds;
    }

    // C# 11: raw string literals (great for JSON/SQL/templates)
    public static string RawStringTemplateExample(int userId)
    {
        string json = $$"""
            {
              "userId": {{userId}},
              "tags": ["csharp", "best-practices"],
              "memberNames": {
                "count": "{{nameof(IReadOnlyList<>.Count)}}",
                "items": "{{nameof(MyCompany.Api.UserService<>.ConfigurationItems)}}"
              }
            }
            """;

        return json;
    }
}
```

## File-based Apps (C# 14 preprocessor directives)

```csharp
#!/usr/bin/env dotnet run
#:package Some.Package
#:load other.csx
#:time

// These directives apply to file-based apps. Project compilation ignores them.
```

## Cheat Sheet

| Topic | Practice | Version | Notes |
|---|---|---:|---|
| Required data | `required init` | 11+ | Prefer explicit required initialization |
| Read-only APIs | `IReadOnlyList<T>` | 1.0+ | Communicate intent |
| Avoid magic strings | `const` / `nameof` | 1.0+ | Centralize and refactor-safe |
| XML docs | docs for non-private | 1.0+ | Use `{}` in generic `cref` |
| Collections | collection expressions | 12 | `[]`, `..` spread |
| Templates | raw string literals | 11 | Great for JSON/SQL |
| C# 13 | params collections / `Lock` / `\e` | 13 | Language + library interaction |
| C# 14 | extension members / `field` / span rules | 14 | New syntax + overload resolution |

## Notes

- This file is intentionally example-heavy.
- `BEST-PRACTICES.md` contains rationale and pitfalls without duplicating code.

## MCP Tools

Add `use microsoft-learn` to use:
- `microsoft_docs_search`
- `microsoft_docs_fetch`
- `microsoft_code_sample_search`
