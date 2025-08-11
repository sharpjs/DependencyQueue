// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Error     = DependencyQueueError;
using ErrorType = DependencyQueueErrorType;

[TestFixture]
public class DependencyQueueCycleErrorTests
{
    [Test]
    public void Create_NullRequiringEntry()
    {
        Invoking(() => Error.Cycle(null!, new Topic("b")))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "requiringEntry");
    }

    [Test]
    public void Create_NullRequiredTopic()
    {
        Invoking(() => Error.Cycle(new Entry("a"), null!))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "requiredTopic");
    }

    [Test]
    public void Type_Get()
    {
        var entry = new Entry("a");
        var topic = new Topic("b");
        var error = Error.Cycle(entry, topic);

        error.Type.Should().Be(ErrorType.Cycle);
    }

    [Test]
    public void RequiringEntry_Get()
    {
        var entry = new Entry("a");
        var topic = new Topic("b");
        var error = Error.Cycle(entry, topic);

        error.RequiringEntry.Should().BeSameAs(entry);
    }

    [Test]
    public void RequiredTopic_Get()
    {
        var entry = new Entry("a");
        var topic = new Topic("b");
        var error = Error.Cycle(entry, topic);

        error.RequiredTopic.Should().BeSameAs(topic);
    }

    [Test]
    [SetCulture("")] // invariant culture
    public void ToStringMethod()
    {
        var entry = new Entry("a");
        var topic = new Topic("b");
        var error = Error.Cycle(entry, topic);

        error.ToString().Should().Be(
            "The entry 'a' cannot require topic 'b' because " +
            "an entry providing that topic already requires entry 'a'. " +
            "The dependency graph does not permit cycles."
        );
    }
}
