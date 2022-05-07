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
