using System;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyQueue
{
    internal readonly struct AsyncMonitorLock : IDisposable
    {
        private readonly AsyncMonitor _monitor;

        internal AsyncMonitorLock(AsyncMonitor monitor)
            => _monitor = monitor;

        /// <inheritdoc cref="AsyncMonitor.ReleaseUntilPulse(int)"/>
        public void ReleaseUntilPulse(int msTimeout)
            => _monitor.ReleaseUntilPulse(msTimeout);

        /// <inheritdoc cref="AsyncMonitor.ReleaseUntilPulseAsync(int, CancellationToken)"/>
        public Task ReleaseUntilPulseAsync(int msTimeout, CancellationToken cancellation = default)
            => _monitor.ReleaseUntilPulseAsync(msTimeout, cancellation);

        /// <inheritdoc cref="AsyncMonitor.Release"/>
        void IDisposable.Dispose()
            => _monitor.Release();
    }
}
