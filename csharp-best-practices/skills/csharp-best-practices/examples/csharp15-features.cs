// C# 15 Feature Examples (.NET 11)
// Requires a compatible .NET 11 SDK and an effective C# 15 language version.
// These examples intentionally exclude C# 15 preview memory-safety features.

using System.Collections.Generic;
using System.Linq;

// --- Collection expression arguments ---

public static class CSharp15CollectionExamples
{
    public static List<string> CreateNames(IEnumerable<string> values) =>
        [with(capacity: 16), .. values];

    public static HashSet<string> CreateCaseInsensitiveSet() =>
        [with(System.StringComparer.OrdinalIgnoreCase), "Hello", "HELLO", "hello"];
}

// --- Union types ---

public record class CSharp15Cat(string Name);
public record class CSharp15Dog(string Name);
public record class CSharp15Bird(string Name);

public union CSharp15Pet(CSharp15Cat, CSharp15Dog, CSharp15Bird);

public static class CSharp15UnionExamples
{
    public static string Describe(CSharp15Pet pet) => pet switch
    {
        CSharp15Cat cat => $"cat:{cat.Name}",
        CSharp15Dog dog => $"dog:{dog.Name}",
        CSharp15Bird bird => $"bird:{bird.Name}",
    };
}

// --- Closed hierarchies ---

public closed record class CSharp15GateState;
public record class CSharp15Closed : CSharp15GateState;
public record class CSharp15Open(float Percent) : CSharp15GateState;

public static class CSharp15HierarchyExamples
{
    public static string Describe(CSharp15GateState state) => state switch
    {
        CSharp15Closed => "closed",
        CSharp15Open(var percent) => $"{percent}% open",
    };
}

// --- Extension indexers ---

public static class CSharp15SequenceIndexer
{
    extension(IEnumerable<int> sequence)
    {
        public int this[int index] => sequence.ElementAt(index);
    }
}

// --- Labeled break and continue ---

public static class CSharp15LabeledJumpExamples
{
    public static (int Row, int Column) FindFirstGoal(int[,] grid, int goal)
    {
        var result = (-1, -1);

        outer: for (var row = 0; row < grid.GetLength(0); row++)
        {
            for (var column = 0; column < grid.GetLength(1); column++)
            {
                if (grid[row, column] < 0)
                {
                    continue outer;
                }

                if (grid[row, column] == goal)
                {
                    result = (row, column);
                    break outer;
                }
            }
        }

        return result;
    }
}

// --- Non-virtual static interface members ---

public interface CSharp15Measurement
{
    public static double ToMeters(double centimeters) => centimeters / 100;
}

public static class CSharp15StaticInterfaceExamples
{
    public static double ConvertCentimeters(double centimeters) =>
        CSharp15Measurement.ToMeters(centimeters);
}
