using System;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyQueue
{
    internal interface IDependencyQueue<T>
    {
        StringComparer Comparer { get; }

        void Enqueue(DependencyQueueEntry<T> entry);

        DependencyQueueEntry<T>? TryDequeue(
            Func<T, bool>? predicate = null
        );

        Task<DependencyQueueEntry<T>?> TryDequeueAsync(
            Func<T, bool>?    predicate    = null,
            CancellationToken cancellation = default
        );

        void Complete(DependencyQueueEntry<T> entry);

        void SetEnding();
    }
}
