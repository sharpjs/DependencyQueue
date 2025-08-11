// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class AsyncMonitorTests
{
    [Test]
    public void Dispose_Managed()
    {
        var monitor = new TestMonitor();

        monitor.Dispose();
        monitor.Dispose(); // to test multiple disposes
    }

    [Test]
    public void Dispose_Unmanaged()
    {
        var monitor = new TestMonitor();

        monitor.SimulateUnmanagedDispose();
    }

    private class TestMonitor : AsyncMonitor
    {
        internal void SimulateUnmanagedDispose()
        {
            Dispose(managed: false);
            GC.SuppressFinalize(this);
        }
    }
}
