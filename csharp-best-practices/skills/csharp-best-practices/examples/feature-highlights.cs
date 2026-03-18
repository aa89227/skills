// C# 12/13 Feature Highlights
// Demonstrates: primary constructors, default lambda params, ref readonly,
//   collection expressions, params collections, Lock, raw string literals,
//   implicit indexer access, IAsyncEnumerable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public static class FeatureHighlights
{
    // --- C# 12 ---

    // Primary constructors (common DI pattern)
    // WARNING: captured parameter is NOT a field — avoid mutation and be aware of multiple captures
    public sealed class ExampleController(IService service)
    {
        public int GetValue() => service.GetValue();

        // If mutation or multiple access is needed, assign to a field explicitly:
        // private readonly IService _service = service;
    }

    public interface IService { int GetValue(); }

    // Default lambda parameters
    public static int DefaultLambdaExample()
    {
        var incrementBy = (int source, int increment = 1) => source + increment;
        return incrementBy(5) + incrementBy(5, 2); // 6 + 7 = 13
    }

    // ref readonly parameters
    public static int Add(ref readonly int x, int y) => x + y;

    // Experimental attribute
    [Experimental("SKILL0001")]
    public static void ExperimentalApi() { }

    // Collection expressions + spread
    public static IReadOnlyList<int> CollectionExpressionsExample()
    {
        IReadOnlyList<int> baseIds = [1, 2, 3];
        int[] allIds = [.. baseIds, 4, 5];
        return allIds;
    }

    // --- C# 13 ---

    // params collections (ReadOnlySpan<T> — zero allocation)
    public static int Sum(params ReadOnlySpan<int> values)
    {
        var total = 0;
        foreach (var v in values) total += v;
        return total;
        // Usage: Sum(1, 2, 3) or Sum([1, 2, 3])
    }

    // System.Threading.Lock (typed lock)
    public static int ThreadSafeIncrement(ref int counter)
    {
        var gate = new Lock();
        lock (gate) { return ++counter; }
    }

    // Escape sequence \e (ESC, U+001B)
    public static char EscapeChar() => '\e';

    // Implicit indexer access (^ from-the-end) in object initializers
    public static int[] ImplicitIndexerExample()
    {
        var data = new DataHolder
        {
            Buffer = { [^1] = 0, [^2] = 1, [^3] = 2 }
        };
        return data.Buffer;
    }

    public sealed class DataHolder
    {
        public int[] Buffer { get; } = new int[10];
    }

    // --- C# 11 ---

    // Raw string literals (great for JSON/SQL/templates)
    public static string RawStringExample(int userId)
    {
        return $$"""
            {
              "userId": {{userId}},
              "tags": ["csharp", "best-practices"]
            }
            """;
    }

    // --- IAsyncEnumerable<T> ---

    // Produce items asynchronously with yield return
    public static async IAsyncEnumerable<int> GenerateAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            yield return i;
        }
    }

    // Consume with await foreach
    public static async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await foreach (var item in GenerateAsync(10, cancellationToken))
        {
            Console.WriteLine(item);
        }
    }
}
