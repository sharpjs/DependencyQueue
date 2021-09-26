using System;
using FluentAssertions;

namespace DependencyQueue
{
    internal static class TestExtensions
    {
        internal static void ShouldHaveTopic(
            this Queue queue,
            string     name,
            Entry[]?   providedBy = null,
            Entry[]?   requiredBy = null)
        {
            var topics = queue.Topics;
            topics.Keys.Should().Contain(name);

            var topic = topics[name];
            topic.Name      .Should().Be(name);
            topic.ProvidedBy.Should().BeEquivalentTo(providedBy ?? Array.Empty<Entry>());
            topic.RequiredBy.Should().BeEquivalentTo(requiredBy ?? Array.Empty<Entry>());
        }
    }
}
