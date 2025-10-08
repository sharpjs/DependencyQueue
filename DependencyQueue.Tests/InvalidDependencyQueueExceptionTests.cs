// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class InvalidDependencyQueueExceptionTests
{
    [Test]
    public void Construct_NullErrors()
    {
        Should.Throw<ArgumentNullException>(
            () => new InvalidDependencyQueueException(null!)
        );
    }

    [Test]
    public void Errors_Get()
    {
        var topicA = new DependencyQueueTopic<Value>("a");
        var topicB = new DependencyQueueTopic<Value>("b");
        var entryC = new DependencyQueueEntry<Value>("c", value: new(), StringComparer.Ordinal);

        var errors = new DependencyQueueError[]
        {
            new DependencyQueueUnprovidedTopicError<Value>(topicA),
            new DependencyQueueCycleError<Value>(entryC, topicB)
        };

        var exception = new InvalidDependencyQueueException(errors);

        exception.Errors.ShouldBeSameAs(errors);
    }
}
