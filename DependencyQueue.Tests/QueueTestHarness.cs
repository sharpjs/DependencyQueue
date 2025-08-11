// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

internal abstract class QueueTestHarness : TestHarnessBase
{
    public Context      Context  { get; }

    public Mock<IQueue> Queue    { get; }
    public Guid         RunId    { get; }
    public int          WorkerId { get; }
    public Data         Data     { get; }

    protected QueueTestHarness()
    {
        Queue    = Mocks.Create<IQueue>();
        RunId    = Guid.NewGuid();
        WorkerId = Random.Next(1, 100);
        Data     = new();

        Context = new(Queue.Object, RunId, WorkerId, Data, Cancellation.Token);
    }
}
