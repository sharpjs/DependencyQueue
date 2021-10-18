using System;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyQueue
{
    using Void = ValueTuple;

    /// <summary>
    ///   An exclusive lock primitive analogous to <see cref="Monitor"/> that
    ///   supports both synchronous and asynchronous operations.
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
        ///   the object.
        /// </summary>
        /// <remarks>
        ///   This method is the equivalent of
        ///   <see cref="Monitor.Enter(object)"/>.
        /// </remarks>
        public AsyncMonitorLock Acquire()
        {
            _lock.Wait();
            return new(this);
        }

        /// <summary>
        ///   Waits asynchronously to acquire an exclusive lock on the object.
        /// </summary>
        /// <param name="cancellation">
        ///   The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   This method is the asynchronous analog of
        ///   <see cref="Monitor.Enter(object)"/>.
        /// </remarks>
        public async Task<AsyncMonitorLock> AcquireAsync(CancellationToken cancellation = default)
        {
            await _lock.WaitAsync(cancellation);
            return new(this);
        }

        /// <summary>
        ///   Releases an exclusive lock on the object.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     ⚠ <strong>Warning:</strong>
        ///     This method must be called from a scope in which the current
        ///     thread holds an exclusive lock on the object.
        ///   </para>
        ///   <para>
        ///     This method is the asynchronous analog of
        ///     <see cref="Monitor.Exit(object)"/>.
        ///   </para>
        /// </remarks>
        public void Release()
        {
            _lock.Release();
        }

        /// <summary>
        ///   Releases an exclusive lock on the object and blocks the current
        ///   thread until it reacquires the lock.  The current thread will
        ///   begin to reacquire the lock when signaled by <see cref="PulseAll"/>
        ///   or when the specified timeout elapses.
        /// </summary>
        /// <param name="msTimeout">
        ///   The timeout interval in milliseconds.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     ⚠ <strong>Warning:</strong>
        ///     This method must be called from a scope in which the current
        ///     thread holds an exclusive lock on the object.
        ///   </para>
        ///   <para>
        ///     This method is the equivalent of
        ///     <see cref="Monitor.Wait(object, int)"/>.
        ///   </para>
        /// </remarks>
        public void ReleaseUntilPulse(int msTimeout)
        {
            var timeoutTask = Task.Delay(msTimeout);
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
        ///   Releases an exclusive lock on the object and blocks the current
        ///   thread until it reacquires the lock.  The current thread will
        ///   begin to reacquire the lock when signaled by <see cref="PulseAll"/>
        ///   or when the specified timeout elapses.
        /// </summary>
        /// <param name="msTimeout">
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
        ///     thread holds an exclusive lock on the object.
        ///   </para>
        ///   <para>
        ///     This method is the asynchronous analog of
        ///     <see cref="Monitor.Wait(object, int)"/>.
        ///   </para>
        /// </remarks>
        public async Task ReleaseUntilPulseAsync(int msTimeout, CancellationToken cancellation = default)
        {
            var timeoutTask = Task.Delay(msTimeout, cancellation);
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
        ///   Causes threads waiting in <see cref="ReleaseUntilPulse(int)"/>
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
        ///   Releases resources used by the object.
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
        ///   resources used by the object.  Invoked by <see cref="Dispose()"/>.
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
    }
}
