// C# 14 Feature Examples (.NET 10)
// Demonstrates: extension members, partial constructors/events,
//   user-defined compound assignment, null-conditional assignment,
//   field keyword, unbound generic nameof

using System;
using System.Collections.Generic;

// --- Extension Members ---
// New syntax: extension properties, static extensions, operator extensions

public static class IntExtensions
{
    // Instance extensions
    extension(int n)
    {
        public bool IsEven() => n % 2 == 0;
        public bool IsOdd() => n % 2 != 0;
    }

    // Static extensions
    extension(int)
    {
        public static int Random(int min, int max) => new Random().Next(min, max);
    }
}

// --- Partial Constructors / Events ---

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

// --- User-defined Compound Assignment ---

public sealed class Counter
{
    public int Value { get; private set; }

    public void operator +=(int increment) => Value += increment;
    public void operator -=(int decrement) => Value -= decrement;
}

// --- Null-conditional Assignment ---

public sealed class Customer
{
    public Order? CurrentOrder { get; set; }
}

public sealed class Order { }

public static class NullConditionalAssignmentExample
{
    public static void Assign(Customer? customer)
    {
        // C# 14: ?. on left-hand side of assignment
        customer?.CurrentOrder = new Order();
    }
}

// --- field keyword ---
// Compiler synthesizes backing field; no manual declaration needed.

public class Product
{
    public string Name
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }

    public decimal Price
    {
        get;
        set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }
}

// --- Unbound generic nameof ---

public static class NameofExamples
{
    // C# 14: nameof on unbound generic types
    public static string ListName => nameof(List<>);           // "List"
    public static string CountName => nameof(List<>.Count);    // "Count"
}
