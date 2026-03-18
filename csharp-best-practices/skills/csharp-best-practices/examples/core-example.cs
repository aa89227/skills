// Core Example: Modern C# best practices in one file
// Demonstrates: required init, IReadOnlyList<T>, XML docs, async/await,
//   CancellationToken, list patterns, Parallel.ForEachAsync, record, record struct

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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

    /// <summary>
    /// Message with field keyword validation (C# 14).
    /// No manual backing field needed — compiler synthesizes it.
    /// </summary>
    public string Message
    {
        get;
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
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            throw;
        }
    }

    /// <summary>
    /// Batch-process users with Parallel.ForEachAsync.
    /// </summary>
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

        return [.. results];
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
}

// --- Supporting types ---

/// <summary>A user DTO using record with required init.</summary>
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

/// <summary>A common API result wrapper using readonly record struct.</summary>
public readonly record struct ApiResult<T>(T? Data, bool Success, string? ErrorMessage)
{
    public static ApiResult<T> Ok(T data) => new(data, true, null);
    public static ApiResult<T> Fail(string error) => new(default, false, error);
}

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

public enum UserTier { Standard = 0, Premium = 1, VIP = 2 }

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
