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
