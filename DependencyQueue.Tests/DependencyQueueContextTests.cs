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

using static FluentActions;

[TestFixture]
public class DependencyQueueContextTests
{
    [Test]
    public void Construct_NullQueue()
    {
        Invoking(() => new Context(null!, Guid.NewGuid(), 1, new()))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "queue");
    }

    [Test]
    public void Construct_InvalidWorkerId()
    {
        var queue = Mock.Of<IQueue>();

        Invoking(() => new Context(queue, Guid.NewGuid(), 0, new()))
            .Should().ThrowExactly<ArgumentOutOfRangeException>()
            .Where(e => e.ParamName == "workerId");
    }

    [Test]
    public void RunId_Get()
    {
        using var h = new TestHarness();

        h.Context.RunId.Should().Be(h.RunId);
    }

    [Test]
    public void WorkerId_Get()
    {
        using var h = new TestHarness();

        h.Context.WorkerId.Should().Be(h.WorkerId);
    }

    [Test]
    public void Data_Get()
    {
        using var h = new TestHarness();

        h.Context.Data.Should().BeSameAs(h.Data);
    }

    [Test]
    public void CancellationToken_Get()
    {
        using var h = new TestHarness();

        h.Context.CancellationToken.Should().Be(h.Cancellation.Token);
    }

    [Test]
    public void GetNextEntry_First()
    {
        using var h = new TestHarness();

        var entry = new Entry("x", new());

        h.Queue
            .Setup(q => q.TryDequeue(null))
            .Returns(entry)
            .Verifiable();

        var result = h.Context.GetNextEntry();

        result.Should().BeSameAs(entry);
    }

    [Test]
    public void GetNextEntry_Next()
    {
        using var h = new TestHarness();

        var entry0 = new Entry("a", new());
        var entry1 = new Entry("b", new());

        h.Queue
            .Setup(q => q.TryDequeue(null))
            .Returns(entry0);

        h.Context.GetNextEntry();

        var s = new MockSequence();
        h.Queue.Reset();
        h.Queue.InSequence(s)
            .Setup(q => q.Complete(entry0))
            .Verifiable();
        h.Queue.InSequence(s)
            .Setup(q => q.TryDequeue(null))
            .Returns(entry1)
            .Verifiable();

        var result = h.Context.GetNextEntry();

        result.Should().BeSameAs(entry1);
    }

    [Test]
    public async Task GetNextEntryAsync_FirstAsync()
    {
        using var h = new TestHarness();

        var entry = new Entry("x", new());

        h.Queue
            .Setup(q => q.TryDequeueAsync(null, h.Cancellation.Token))
            .ReturnsAsync(entry)
            .Verifiable();

        var result = await h.Context.GetNextEntryAsync();

        result.Should().BeSameAs(entry);
    }

    [Test]
    public async Task GetNextEntryAsync_NextAsync()
    {
        using var h = new TestHarness();

        var entry0 = new Entry("a", new());
        var entry1 = new Entry("b", new());

        h.Queue
            .Setup(q => q.TryDequeueAsync(null, h.Cancellation.Token))
            .ReturnsAsync(entry0);

        await h.Context.GetNextEntryAsync();

        var s = new MockSequence();
        h.Queue.Reset();
        h.Queue.InSequence(s)
            .Setup(q => q.Complete(entry0))
            .Verifiable();
        h.Queue.InSequence(s)
            .Setup(q => q.TryDequeueAsync(null, h.Cancellation.Token))
            .ReturnsAsync(entry1)
            .Verifiable();

        var result = await h.Context.GetNextEntryAsync();

        result.Should().BeSameAs(entry1);
    }

    [Test]
    public void SetEnding()
    {
        using var h = new TestHarness();

        h.Queue
            .Setup(q => q.SetEnding())
            .Verifiable();

        h.Context.SetEnding();
    }

    private class TestHarness : QueueTestHarness { }
}
