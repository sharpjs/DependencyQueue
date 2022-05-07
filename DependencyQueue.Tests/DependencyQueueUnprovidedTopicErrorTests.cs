/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

namespace DependencyQueue;

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
