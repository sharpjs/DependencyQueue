using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using Error = DependencyQueueUnprovidedTopicError<Value>;
    using ErrorType = DependencyQueueErrorType;

    [TestFixture]
    public class DependencyQueueUnprovidedTopicErrorTests
    {
        [Test]
        public void Type_Get()
        {
            var topic = new Topic("a");
            var error = new Error(topic);

            error.Type.Should().Be(ErrorType.UnprovidedTopic);
        }

        [Test]
        public void Topic_Get()
        {
            var topic = new Topic("a");
            var error = new Error(topic);

            error.Topic.Should().BeSameAs(topic);
        }

        [Test]
        [SetCulture("")] // invariant culture
        public void ToStringMethod()
        {
            var topic = new Topic("a");
            var error = new Error(topic);

            error.ToString().Should().Be(
                "The topic 'a' is required but not provided."
            );
        }
    }
}
