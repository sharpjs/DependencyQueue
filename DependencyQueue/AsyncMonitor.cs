using System;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyQueue
{
    using Void = ValueTuple;

    /// <summary>
    ///   An exclusively lockable primitive, analogous to <see cref="Monitor"/>,
    ///   that supports both synchronous and asynchronous operations.
    /// </summary>
    internal class AsyncMonitor : IDisposable
    {
        // The exclusive lock itself
        private readonly SemaphoreSlim _lock;

        // A fake task to implement pulse
        private TaskCompletionSource<Void> _pulser;

        /// <summary>
        ///   Initializes a new <see cref="AsyncMonitor"/> instance.
        /// </summary>
        internal AsyncMonitor()
        {
            _lock   = new(initialCount: 1, maxCount: 1);
            _pulser = new();
        }

        /// <summary>
        ///   Blocks the current thread until it acquires an exclusive lock on
        ///   the monitor.
        /// </summary>
        /// <returns>
        ///   A disposable object representing the lock held on the monitor.
        ///   Disposing the object releases the lock.
        /// </returns>
        /// <remarks>
        ///   This method is the equivalent of
        ///   <see cref="Monitor.Enter(object)"/>.
        /// </remarks>
        public Lock Acquire()
        {
            _lock.Wait();
            return new(this);
        }

        /// <summary>
        ///   Waits asynchronously to acquire an exclusive lock on the monitor.
        /// </summary>
        /// <param name="cancellation">
        ///   The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  When the task
        ///   completes, its <see cref="Task{T}.Result"/> property is set to
        ///   a disposable object representing the lock held on the monitor.
        ///   Disposing the object releases the lock.
        /// </returns>
        /// <remarks>
        ///   This method is the asynchronous analog of
        ///   <see cref="Monitor.Enter(object)"/>.
        /// </remarks>
        public async Task<Lock> AcquireAsync(CancellationToken cancellation = default)
        {
            await _lock.WaitAsync(cancellation);
            return new(this);
        }

        /// <summary>
        ///   Releases an exclusive lock on the monitor.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     ⚠ <strong>Warning:</strong>
        ///     This method must be called from a scope in which the current
        ///     thread holds an exclusive lock on the monitor.
        ///   </para>
        ///   <para>
        ///     This method is the asynchronous analog of
        ///     <see cref="Monitor.Exit(object)"/>.
        ///   </para>
        /// </remarks>
        private void Release()
        {
            _lock.Release();
        }

        /// <summary>
        ///   Releases an exclusive lock on the monitor and blocks the current
        ///   thread until it reacquires the lock.  Lock reacquisition begins
        ///   when signaled by <see cref="PulseAll"/> or when the specified
        ///   timeout elapses.
        /// </summary>
        /// <param name="timeoutMs">
        ///   The timeout interval in milliseconds.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     ⚠ <strong>Warning:</strong>
        ///     This method must be called from a scope in which the current
        ///     thread holds an exclusive lock on the monitor.
        ///   </para>
        ///   <para>
        ///     This method is the equivalent of
        ///     <see cref="Monitor.Wait(object, int)"/>.
        ///   </para>
        /// </remarks>
        private void ReleaseUntilPulse(int timeoutMs)
        {
            var timeoutTask = Task.Delay(timeoutMs);
            var reacquired  = false;

            _lock.Release();
            try
            {
                do
                {
                    // Wait for pulse or timeout
                    var index = Task.WaitAny(timeoutTask, _pulser.Task);

                    // Check for timeout
                    if (index == 0)
                        break;

                    // Pulsed => reacquire immediately if possible; otherwise loop
                    reacquired = _lock.Wait(0);
                }
                while (!reacquired);
            }
            finally
            {
                // Timeout or exception => wait for reacquisition
                if (!reacquired)
                    _lock.Wait();
            }
        }

        /// <summary>
        ///   Releases an exclusive lock on the monitor and waits asynchronously
        ///   to reacquire the lock.  Lock reacquisition begins when signaled
        ///   by <see cref="PulseAll"/> or when the specified timeout elapses.
        /// </summary>
        /// <param name="timeoutMs">
        ///   The timeout interval in milliseconds.
        /// </param>
        /// <param name="cancellation">
        ///   The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     ⚠ <strong>Warning:</strong>
        ///     This method must be called from a scope in which the current
        ///     thread holds an exclusive lock on the monitor.
        ///   </para>
        ///   <para>
        ///     This method is the asynchronous analog of
        ///     <see cref="Monitor.Wait(object, int)"/>.
        ///   </para>
        /// </remarks>
        private async Task ReleaseUntilPulseAsync(int timeoutMs, CancellationToken cancellation = default)
        {
            var timeoutTask = Task.Delay(timeoutMs, cancellation);
            var reacquired  = false;

            _lock.Release();
            try
            {
                do
                {
                    // Wait for pulse or timeout
                    var task = await Task.WhenAny(timeoutTask, _pulser.Task);

                    // Check for timeout
                    if (task == timeoutTask)
                        break;

                    // Pulsed => reacquire immediately if possible; otherwise loop
                    reacquired = _lock.Wait(0);
                }
                while (!reacquired);
            }
            finally
            {
                // Timeout or exception => wait for reacquisition
                if (!reacquired)
                    await _lock.WaitAsync(cancellation);
            }
        }

        /// <summary>
        ///   Activates all execution contexts waiting for a pulse signal, so
        ///   that one of them can acquire an exclusive lock on the monitor.
        /// </summary>
        /// <remarks>
        ///   This method is the equivalent of
        ///   <see cref="Monitor.PulseAll(object)"/>.
        /// </remarks>
        public void PulseAll()
        {
            var signal = Interlocked.Exchange(ref _pulser, new());

            signal.SetResult(default);
        }

        /// <summary>
        ///   Releases resources used by the monitor.
        /// </summary>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   This method is not thread-safe.  Do not invoke this method
        ///   concurrently with other members of this instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(managed: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Releases the unmanaged resources and, optionally, the managed
        ///   resources used by the monitor.  Invoked by <see cref="Dispose()"/>.
        ///   Derived classes should override this method to extend disposal
        ///   behavior.
        /// </summary>
        /// <param name="managed">
        ///   <see langword="true"/>
        ///     to release both managed and unmanaged resources;
        ///   <see langword="false"/>
        ///     to release only unmanaged resources.
        /// </param>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   This method is not thread-safe.  Do not invoke this method
        ///   concurrently with other members of this instance.
        /// </remarks>
        protected virtual void Dispose(bool managed)
        {
            if (!managed)
                return;

            _lock.Dispose();
        }

        /// <summary>
        ///   Represents an exclusive lock held against an
        ///   <see cref="AsyncMonitor"/>.
        /// </summary>
        internal readonly struct Lock : IDisposable
        {
            private readonly AsyncMonitor _monitor;

            internal Lock(AsyncMonitor monitor)
                => _monitor = monitor;

            /// <inheritdoc cref="AsyncMonitor.ReleaseUntilPulse(int)"/>
            public void ReleaseUntilPulse(int timeoutMs)
                => _monitor.ReleaseUntilPulse(timeoutMs);

            /// <inheritdoc cref="AsyncMonitor.ReleaseUntilPulseAsync(int, CancellationToken)"/>
            public Task ReleaseUntilPulseAsync(int timeoutMs, CancellationToken cancellation = default)
                => _monitor.ReleaseUntilPulseAsync(timeoutMs, cancellation);

            /// <inheritdoc cref="AsyncMonitor.Release"/>
            void IDisposable.Dispose()
                => _monitor.Release();
        }
    }
}
