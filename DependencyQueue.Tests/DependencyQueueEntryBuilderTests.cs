// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueEntryBuilderTests
{
    [Test]
    public void Construct_NullQueue()
    {
        var e = Should.Throw<ArgumentNullException>(
            () => new Builder(null!)
        );

        e.ParamName.ShouldBe("queue");
    }

    [Test]
    public void Construct_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        builder.CurrentEntry.ShouldBeNull();
        builder.Queue       .ShouldBeSameAs(queue);
    }

    [Test]
    public void NewEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        var value = new Value();
        var entry = builder
            .NewEntry("x", value)
            .CurrentEntry;

        entry         .ShouldNotBeNull();
        entry.Name    .ShouldBeSameAs("x");
        entry.Value   .ShouldBeSameAs(value);
        entry.Requires.ShouldBeEmpty();
        entry.Provides.ShouldBe(["x"]);
    }

    [Test]
    public void AddProvides_ParamsArray_NoCurrentEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddProvides("a", "b")
        );
    }

    [Test]
    public void AddProvides_IEnumerable_NoCurrentEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddProvides((IEnumerable<string>) ["a", "b"])
        );
    }

    [Test]
    public void AddProvides_ParamsArray_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        var entry = builder
            .NewEntry("x", value: new())
            .AddProvides("a", "b")
            .CurrentEntry;

        entry         .ShouldNotBeNull();
        entry.Provides.ShouldBe(["x", "a", "b"]);
    }

    [Test]
    public void AddProvides_IEnumerable_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        var entry = builder
            .NewEntry("x", value: new())
            .AddProvides((IEnumerable<string>) ["a", "b"])
            .CurrentEntry;

        entry         .ShouldNotBeNull();
        entry.Provides.ShouldBe(["x", "a", "b"]);
    }

    [Test]
    public void AddRequires_ParamsArray_NoCurrentEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddRequires("a", "b")
        );
    }

    [Test]
    public void AddRequires_IEnumerable_NoCurrentEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddRequires((IEnumerable<string>) ["a", "b"])
        );
    }

    [Test]
    public void AddRequires_ParamsArray_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        var entry = builder
            .NewEntry("x", value: new())
            .AddRequires("a", "b")
            .CurrentEntry;

        entry         .ShouldNotBeNull();
        entry.Requires.ShouldBe(["a", "b"]);
    }

    [Test]
    public void AddRequires_IEnumerable_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        var entry = builder
            .NewEntry("x", value: new())
            .AddRequires((IEnumerable<string>) ["a", "b"])
            .CurrentEntry;

        entry         .ShouldNotBeNull();
        entry.Requires.ShouldBe(["a", "b"]);
    }

    [Test]
    public void Enqueue_NoCurrentEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.Enqueue()
        );
    }

    [Test]
    public void Enqueue_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        builder
            .NewEntry("x", value: new())
            .Enqueue();

        builder.CurrentEntry.ShouldBeNull();

        var entry = queue.ReadyEntries.ShouldHaveSingleItem();
        entry         .ShouldNotBeNull();
        entry.Name    .ShouldBe("x");
        entry.Provides.ShouldBe(["x"]);
        entry.Requires.ShouldBeEmpty();

        var topic = queue.Topics.Values.ShouldHaveSingleItem();
        topic.Name      .ShouldBe("x");
        topic.ProvidedBy.ShouldBe([entry]);
        topic.RequiredBy.ShouldBeEmpty();
    }

    [Test]
    public void Enqueue_WithOutParameter_NoCurrentEntry()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.Enqueue(out _)
        );
    }

    [Test]
    public void Enqueue_WithOutParameter__Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

        builder
            .NewEntry("x", value: new())
            .Enqueue(out var entry);

        builder.CurrentEntry.ShouldBeNull();

        entry         .ShouldNotBeNull();
        entry.Name    .ShouldBe("x");
        entry.Provides.ShouldBe(["x"]);
        entry.Requires.ShouldBeEmpty();

        queue.ReadyEntries.ShouldBe([entry]);

        var topic = queue.Topics.Values.ShouldHaveSingleItem();
        topic.Name      .ShouldBe("x");
        topic.ProvidedBy.ShouldBe([entry]);
        topic.RequiredBy.ShouldBeEmpty();
    }
}
