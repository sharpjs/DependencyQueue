// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Error     = DependencyQueueError;
using ErrorType = DependencyQueueErrorType;

[TestFixture]
public class DependencyQueueCycleErrorTests
{
    [Test]
    public void Create_NullRequiringItem()
    {
        Invoking(() => Error.Cycle(null!, new Topic("b")))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "requiringItem");
    }

    [Test]
    public void Create_NullRequiredTopic()
    {
        Invoking(() => Error.Cycle(new Item("a"), null!))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "requiredTopic");
    }

    [Test]
    public void Type_Get()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.Type.Should().Be(ErrorType.Cycle);
    }

    [Test]
    public void RequiringItem_Get()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.RequiringItem.Should().BeSameAs(item);
    }

    [Test]
    public void RequiredTopic_Get()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.RequiredTopic.Should().BeSameAs(topic);
    }

    [Test]
    [SetCulture("")] // invariant culture
    public void ToStringMethod()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.ToString().Should().Be(
            "The item 'a' cannot require topic 'b' because " +
            "an item providing that topic already requires item 'a'. " +
            "The dependency graph does not permit cycles."
        );
    }
}
