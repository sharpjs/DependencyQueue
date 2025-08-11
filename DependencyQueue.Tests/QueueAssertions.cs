// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

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
