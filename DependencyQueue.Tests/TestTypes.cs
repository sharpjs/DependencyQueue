using System;
using System.Threading;
using NUnit.Framework;

namespace DependencyQueue
{
    using static FormattableString;
    using static TestGlobals;

    static class TestGlobals
    {
        internal static StringComparer Comparer
            => StringComparer.OrdinalIgnoreCase;
    }

    interface IQueue : IDependencyQueue<Value> { }

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
            IDependencyQueue<Value> queue,
            Guid                    runId,
            int                     workerId,
            Data                    data,
            CancellationToken       cancellation = default)
            : base(queue, runId, workerId, data, cancellation)
        { }
    }

    class Builder : DependencyQueueEntryBuilder<Value>
    {
        internal Builder(DependencyQueue<Value> queue)
            : base(queue)
        { }
    }
}
