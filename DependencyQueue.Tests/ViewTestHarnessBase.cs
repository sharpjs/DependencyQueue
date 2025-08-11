// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

internal abstract class ViewTestHarnessBase : IDisposable
{
    private readonly AsyncMonitor      _monitor;
    private readonly AsyncMonitor.Lock _lock;

    protected ViewTestHarnessBase()
    {
        _monitor = new();
        _lock    = _monitor.Acquire();
    }

    protected AsyncMonitor.Lock Lock => _lock;

    public void Dispose()
    {
        _lock   .Dispose();
        _monitor.Dispose();
    }
}
