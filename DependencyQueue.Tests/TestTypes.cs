/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

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

interface IQueue : IDependencyQueue<Value> { }

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

class Entry : DependencyQueueEntry<Value>
{
    internal Entry()
        : base(GenerateName(), new(), Comparer)
    { }

    internal Entry(string name)
        : base(name, new(), Comparer)
    { }

    internal Entry(string name, Value value)
        : base(name, value, Comparer)
    { }

    internal Entry(string name, Value value, StringComparer comparer)
        : base(name, value, comparer)
    { }

    private static string GenerateName()
        => TestContext.CurrentContext.Random.GetString(6);
}

class Context : DependencyQueueContext<Value, Data>
{
    internal Context(
        IQueue            queue,
        Guid              runId,
        int               workerId,
        Data              data,
        CancellationToken cancellation = default)
        : base(queue, runId, workerId, data, cancellation)
    { }
}

class Builder : DependencyQueueEntryBuilder<Value>
{
    internal Builder(IQueue queue)
        : base(queue)
    { }
}
