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
        Should.Throw<ArgumentNullException>(
            () => Error.Cycle(null!, new Topic("b"))
        ).ParamName.ShouldBe("requiringItem");
    }

    [Test]
    public void Create_NullRequiredTopic()
    {
        Should.Throw<ArgumentNullException>(
            () => Error.Cycle(new Item("a"), null!)
        ).ParamName.ShouldBe("requiredTopic");
    }

    [Test]
    public void Type_Get()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.Type.ShouldBe(ErrorType.Cycle);
    }

    [Test]
    public void RequiringItem_Get()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.RequiringItem.ShouldBeSameAs(item);
    }

    [Test]
    public void RequiredTopic_Get()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.RequiredTopic.ShouldBeSameAs(topic);
    }

    [Test]
    [SetCulture("")] // invariant culture
    public void ToStringMethod()
    {
        var item  = new Item ("a");
        var topic = new Topic("b");
        var error = Error.Cycle(item, topic);

        error.ToString().ShouldBe(
            "The item 'a' cannot require topic 'b' because " +
            "an item providing that topic already requires item 'a'. " +
            "The dependency graph does not permit cycles."
        );
    }
}
