using System;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace DependencyQueue
{
    internal class QueueAssertions : ReferenceTypeAssertions<Queue, QueueAssertions>
    {
        public QueueAssertions(Queue subject)
            : base(subject) { }

        protected override string Identifier
            => "queue";

        internal void BeValid()
        {
            Subject.Validate().Should().BeEmpty();
        }

        internal void HaveTopic(
            string   name,
            Entry[]? providedBy = null,
            Entry[]? requiredBy = null)
        {
            var topics = Subject.Topics;
            topics.Keys.Should().Contain(name);

            var topic = topics[name];
            topic.Name      .Should().Be(name);
            topic.ProvidedBy.Should().BeEquivalentTo(providedBy ?? Array.Empty<Entry>());
            topic.RequiredBy.Should().BeEquivalentTo(requiredBy ?? Array.Empty<Entry>());
        }
    }
}
