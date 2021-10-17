using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using Error     = DependencyQueueCycleError<Value>;
    using ErrorType = DependencyQueueErrorType;

    [TestFixture]
    public class DependencyQueueCycleErrorTests
    {
        [Test]
        public void Type_Get()
        {
            var entry = new Entry("a");
            var topic = new Topic("b");
            var error = new Error(entry, topic);

            error.Type.Should().Be(ErrorType.Cycle);
        }

        [Test]
        public void RequiringEntry_Get()
        {
            var entry = new Entry("a");
            var topic = new Topic("b");
            var error = new Error(entry, topic);

            error.RequiringEntry.Should().BeSameAs(entry);
        }

        [Test]
        public void RequiredTopic_Get()
        {
            var entry = new Entry("a");
            var topic = new Topic("b");
            var error = new Error(entry, topic);

            error.RequiredTopic.Should().BeSameAs(topic);
        }

        [Test]
        [SetCulture("")] // invariant culture
        public void ToStringMethod()
        {
            var entry = new Entry("a");
            var topic = new Topic("b");
            var error = new Error(entry, topic);

            error.ToString().Should().Be(
                "The entry 'a' cannot require topic 'b' because " +
                "an entry providing that topic already requires entry 'a'. " +
                "The dependency graph does not permit cycles."
            );
        }
    }
}
