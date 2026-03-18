// Pattern Matching Examples (C# 11-14)
// Demonstrates: switch expression, property pattern, relational pattern,
//   list pattern, type pattern, logical pattern, tuple pattern

using System;
using System.Collections.Generic;

public static class PatternMatchingExamples
{
    // --- Switch Expression ---

    public static string GetDiscount(UserTier tier) => tier switch
    {
        UserTier.Standard => "0%",
        UserTier.Premium => "10%",
        UserTier.VIP => "25%",
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    // --- Property Pattern ---

    public static string Classify(Order order) => order switch
    {
        { Total: > 1000, IsPriority: true } => "VIP Rush",
        { Total: > 1000 } => "High Value",
        { Total: > 100 } => "Standard",
        { Total: <= 0 } => "Invalid",
        _ => "Small"
    };

    // --- Relational + Logical Patterns ---

    public static string GetGrade(int score) => score switch
    {
        >= 90 and <= 100 => "A",
        >= 80 and < 90 => "B",
        >= 70 and < 80 => "C",
        >= 60 and < 70 => "D",
        >= 0 and < 60 => "F",
        _ => throw new ArgumentOutOfRangeException(nameof(score))
    };

    // --- Type Pattern ---

    public static string Describe(object obj) => obj switch
    {
        int n when n > 0 => $"Positive integer: {n}",
        int n => $"Non-positive integer: {n}",
        string { Length: > 10 } s => $"Long string: {s[..10]}...",
        string s => $"Short string: {s}",
        null => "null",
        _ => $"Unknown: {obj.GetType().Name}"
    };

    // --- List Patterns (C# 11) ---

    public static string DescribeList(int[] items) => items switch
    {
        [] => "Empty",
        [var single] => $"Single: {single}",
        [var first, .., var last] => $"First: {first}, Last: {last}",
    };

    // --- Tuple Pattern ---

    public static string RockPaperScissors(string first, string second)
        => (first, second) switch
        {
            ("rock", "scissors") or ("scissors", "paper") or ("paper", "rock") => "Player 1 wins",
            _ when first == second => "Tie",
            _ => "Player 2 wins"
        };

    // --- Pattern in if / is ---

    public static void ProcessValue(object? value)
    {
        if (value is int x and > 0)
        {
            Console.WriteLine($"Positive int: {x}");
        }

        if (value is string { Length: > 0 } name)
        {
            Console.WriteLine($"Non-empty string: {name}");
        }

        if (value is not null and not "")
        {
            Console.WriteLine($"Has value: {value}");
        }
    }

    // --- Supporting types ---

    public record Order(decimal Total, bool IsPriority);
    public enum UserTier { Standard, Premium, VIP }
}
