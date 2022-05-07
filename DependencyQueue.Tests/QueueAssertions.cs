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

using FluentAssertions.Primitives;

namespace DependencyQueue;

internal class QueueAssertions : ReferenceTypeAssertions<Queue, QueueAssertions>
{
    public QueueAssertions(Queue subject)
        : base(subject) { }

    protected override string Identifier
        => "queue";

    public void BeValid()
    {
        Subject.Validate().Should().BeEmpty();
    }

    public void HaveReadyEntries(params Entry[] entries)
    {
        using var view = Subject.Inspect();

        view.ReadyEntries.Select(v => v.Entry)
            .Should().Equal(entries);
    }

    public void HaveTopicCount(int expected)
    {
        using var view = Subject.Inspect();

        view.Topics.Count.Should().Be(expected);
    }

    public void HaveTopic(
        string   name,
        Entry[]? providedBy = null,
        Entry[]? requiredBy = null)
    {
        using var view = Subject.Inspect();

        var topics = view.Topics;
        topics.Keys.Should().Contain(name);

        var topic = topics[name];
        topic.Name.Should().Be(name);

        topic.ProvidedBy.Select(v => v.Entry)
            .Should().BeEquivalentTo(providedBy ?? Array.Empty<Entry>());

        topic.RequiredBy.Select(v => v.Entry)
            .Should().BeEquivalentTo(requiredBy ?? Array.Empty<Entry>());
    }
}
