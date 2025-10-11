// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

static class TestGlobals
{
    internal static StringComparer Comparer
        => StringComparer.OrdinalIgnoreCase;

    internal static T[] Items<T>(params T[] items)
        => items;
}

class Value
{
    public override string ToString()
        => Invariant($"Value{GetHashCode():X4}");
}

class Data
{
    public override string ToString()
        => Invariant($"Data{GetHashCode():X4}");
}

class Queue : DependencyQueue<Value>
{
    internal Queue(StringComparer? comparer = null)
        : base(comparer)
    { }

    internal void SimulateUnmanagedDispose()
    {
        Dispose(managed: false);
        GC.SuppressFinalize(this);
    }
}

class Topic : DependencyQueueTopic<Value>
{
    internal Topic(string name)
        : base(name)
    { }
}

class Item : DependencyQueueItem<Value>
{
    internal Item()
        : base(GenerateName(), new(), Comparer)
    { }

    internal Item(string name)
        : base(name, new(), Comparer)
    { }

    internal Item(string name, Value value)
        : base(name, value, Comparer)
    { }

    internal Item(string name, Value value, StringComparer comparer)
        : base(name, value, comparer)
    { }

    private static string GenerateName()
        => TestContext.CurrentContext.Random.GetString(6);
}

class Builder : DependencyQueueBuilder<Value>
{
    internal Builder(Queue queue)
        : base(queue)
    { }
}
