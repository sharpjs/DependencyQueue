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

using Void = ValueTuple;

/// <summary>
///   An exclusively lockable primitive, analogous to <see cref="Monitor"/>,
///   that supports both synchronous and asynchronous operations.
/// </summary>
internal class AsyncMonitor : IDisposable
{
    // The exclusively lockable thing itself
    private readonly SemaphoreSlim _locker;

    // A fake task to implement pulse
    private TaskCompletionSource<Void> _pulser;

    /// <summary>
    ///   Initializes a new <see cref="AsyncMonitor"/> instance.
    /// </summary>
    internal AsyncMonitor()
    {
        _locker = new(initialCount: 1, maxCount: 1);
        _pulser = new();
    }

    /// <summary>
    ///   Blocks the current thread until it acquires an exclusive lock on the
    ///   monitor.
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
        _locker.Wait();
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
    ///   completes, its <see cref="Task{T}.Result"/> property is set to a
    ///   disposable object representing the lock held on the monitor.
    ///   Disposing the object releases the lock.
    /// </returns>
    /// <remarks>
    ///   This method is the asynchronous analog of
    ///   <see cref="Monitor.Enter(object)"/>.
    /// </remarks>
    public async Task<Lock> AcquireAsync(CancellationToken cancellation = default)
    {
        await _locker.WaitAsync(cancellation);
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
    ///     This method is the equivalent of
    ///     <see cref="Monitor.Exit(object)"/>.
    ///   </para>
    /// </remarks>
    private void Release()
    {
        _locker.Release();
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

        _locker.Release();
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
                reacquired = _locker.Wait(0);
            }
            while (!reacquired);
        }
        finally
        {
            // Timeout or exception => wait for reacquisition
            if (!reacquired)
                _locker.Wait();
        }
    }

    /// <summary>
    ///   Releases an exclusive lock on the monitor and waits asynchronously to
    ///   reacquire the lock.  Lock reacquisition begins when signaled by
    ///   <see cref="PulseAll"/> or when the specified timeout elapses.
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
    ///     This method must be called from a scope in which the current thread
    ///     holds an exclusive lock on the monitor.
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

        _locker.Release();
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
                reacquired = _locker.Wait(0, CancellationToken.None);
            }
            while (!reacquired);
        }
        finally
        {
            // Timeout or exception => wait for reacquisition
            if (!reacquired)
                await _locker.WaitAsync(cancellation);
        }
    }

    /// <summary>
    ///   Activates all execution contexts waiting for a pulse signal, so that
    ///   one of them can acquire an exclusive lock on the monitor.
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

        _locker.Dispose();
    }

    /// <summary>
    ///   Represents an exclusive lock held against an
    ///   <see cref="AsyncMonitor"/>.
    /// </summary>
    internal class Lock : IDisposable
    {
        private const string TypeName
            = nameof(AsyncMonitor) + "." + nameof(Lock);

        private readonly AsyncMonitor _monitor;
        private int                   _disposeCount;

        internal Lock(AsyncMonitor monitor)
        {
            _monitor = monitor;
        }

        /// <inheritdoc cref="AsyncMonitor.ReleaseUntilPulse(int)"/>
        public void ReleaseUntilPulse(int timeoutMs)
        {
            RequireNotDisposed();
            _monitor.ReleaseUntilPulse(timeoutMs);
        }

        /// <inheritdoc cref="AsyncMonitor.ReleaseUntilPulseAsync(int, CancellationToken)"/>
        public Task ReleaseUntilPulseAsync(int timeoutMs, CancellationToken cancellation = default)
        {
            RequireNotDisposed();
            return _monitor.ReleaseUntilPulseAsync(timeoutMs, cancellation);
        }

        /// <summary>
        ///   Throws an <see cref="ObjectDisposedException"/> if the object is
        ///   disposed.  Otherwise, this method does nothing.
        /// </summary>
        public void RequireNotDisposed()
        {
            if (_disposeCount != 0)
                throw Errors.ObjectDisposed(TypeName);
        }

        /// <summary>
        ///   Releases the exclusive lock.
        /// </summary>
        /// <remarks>
        ///   It is safe to invoke this method multiple times.  Only the first
        ///   invocation has an effect.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeCount, 1) == 0)
                _monitor.Release();
        }
    }
}
