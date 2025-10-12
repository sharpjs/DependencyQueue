// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Error     = DependencyQueueError;
using ErrorType = DependencyQueueErrorType;

[TestFixture]
public class DependencyQueueUnprovidedTopicErrorTests
{
    [Test]
    public void Create_NullRequiredTopic()
    {
        Should.Throw<ArgumentNullException>(
            () => Error.UnprovidedTopic<Value>(null!)
        ).ParamName.ShouldBe("topic");
    }

    [Test]
    public void Type_Get()
    {
        var topic = new Topic("a");
        var error = Error.UnprovidedTopic(topic);

        error.Type.ShouldBe(ErrorType.UnprovidedTopic);
    }

    [Test]
    public void Topic_Get()
    {
        var topic = new Topic("a");
        var error = Error.UnprovidedTopic(topic);

        error.Topic.ShouldBeSameAs(topic);
    }

    [Test]
    [SetCulture("")] // invariant culture
    public void ToStringMethod()
    {
        var topic = new Topic("a");
        var error = Error.UnprovidedTopic(topic);

        error.ToString().ShouldBe(
            "The topic 'a' is required but not provided."
        );
    }
}
