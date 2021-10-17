using System;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static FluentActions;

    using Error     = DependencyQueueError;
    using ErrorType = DependencyQueueErrorType;

    [TestFixture]
    public class DependencyQueueUnprovidedTopicErrorTests
    {
        [Test]
        public void Create_NullRequiredTopic()
        {
            Invoking(() => Error.UnprovidedTopic<Topic>(null!))
                .Should().Throw<ArgumentNullException>()
                .Where(e => e.ParamName == "topic");
        }

        [Test]
        public void Type_Get()
        {
            var topic = new Topic("a");
            var error = Error.UnprovidedTopic(topic);

            error.Type.Should().Be(ErrorType.UnprovidedTopic);
        }

        [Test]
        public void Topic_Get()
        {
            var topic = new Topic("a");
            var error = Error.UnprovidedTopic(topic);

            error.Topic.Should().BeSameAs(topic);
        }

        [Test]
        [SetCulture("")] // invariant culture
        public void ToStringMethod()
        {
            var topic = new Topic("a");
            var error = Error.UnprovidedTopic(topic);

            error.ToString().Should().Be(
                "The topic 'a' is required but not provided."
            );
        }
    }
}
