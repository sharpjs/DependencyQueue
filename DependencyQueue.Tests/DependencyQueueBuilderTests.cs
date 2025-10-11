// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueBuilderTests
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

        var builder = queue.CreateBuilder();

        builder.CurrentItem.ShouldBeNull();
        builder.Queue      .ShouldBeSameAs(queue);
    }

    [Test]
    public void NewItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        var value = new Value();
        var item = builder
            .NewItem("x", value)
            .CurrentItem;

        item         .ShouldNotBeNull();
        item.Name    .ShouldBeSameAs("x");
        item.Value   .ShouldBeSameAs(value);
        item.Requires.ShouldBeEmpty();
        item.Provides.ShouldBe(["x"]);
    }

    [Test]
    public void AddProvides_ParamsArray_NoCurrentItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddProvides("a", "b")
        );
    }

    [Test]
    public void AddProvides_IEnumerable_NoCurrentItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddProvides((IEnumerable<string>) ["a", "b"])
        );
    }

    [Test]
    public void AddProvides_ParamsArray_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        var item = builder
            .NewItem("x", value: new())
            .AddProvides("a", "b")
            .CurrentItem;

        item         .ShouldNotBeNull();
        item.Provides.ShouldBe(["x", "a", "b"]);
    }

    [Test]
    public void AddProvides_IEnumerable_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        var item = builder
            .NewItem("x", value: new())
            .AddProvides((IEnumerable<string>) ["a", "b"])
            .CurrentItem;

        item         .ShouldNotBeNull();
        item.Provides.ShouldBe(["x", "a", "b"]);
    }

    [Test]
    public void AddRequires_ParamsArray_NoCurrentItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddRequires("a", "b")
        );
    }

    [Test]
    public void AddRequires_IEnumerable_NoCurrentItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.AddRequires((IEnumerable<string>) ["a", "b"])
        );
    }

    [Test]
    public void AddRequires_ParamsArray_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        var item = builder
            .NewItem("x", value: new())
            .AddRequires("a", "b")
            .CurrentItem;

        item         .ShouldNotBeNull();
        item.Requires.ShouldBe(["a", "b"]);
    }

    [Test]
    public void AddRequires_IEnumerable_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        var item = builder
            .NewItem("x", value: new())
            .AddRequires((IEnumerable<string>) ["a", "b"])
            .CurrentItem;

        item         .ShouldNotBeNull();
        item.Requires.ShouldBe(["a", "b"]);
    }

    [Test]
    public void Enqueue_NoCurrentItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.Enqueue()
        );
    }

    [Test]
    public void Enqueue_Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        builder
            .NewItem("x", value: new())
            .Enqueue();

        builder.CurrentItem.ShouldBeNull();

        var item = queue.ReadyItems.ShouldHaveSingleItem();
        item         .ShouldNotBeNull();
        item.Name    .ShouldBe("x");
        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBeEmpty();

        var topic = queue.Topics.Values.ShouldHaveSingleItem();
        topic.Name      .ShouldBe("x");
        topic.ProvidedBy.ShouldBe([item]);
        topic.RequiredBy.ShouldBeEmpty();
    }

    [Test]
    public void Enqueue_WithOutParameter_NoCurrentItem()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        Should.Throw<InvalidOperationException>(
            () => builder.Enqueue(out _)
        );
    }

    [Test]
    public void Enqueue_WithOutParameter__Ok()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        builder
            .NewItem("x", value: new())
            .Enqueue(out var item);

        builder.CurrentItem.ShouldBeNull();

        item         .ShouldNotBeNull();
        item.Name    .ShouldBe("x");
        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBeEmpty();

        queue.ReadyItems.ShouldBe([item]);

        var topic = queue.Topics.Values.ShouldHaveSingleItem();
        topic.Name      .ShouldBe("x");
        topic.ProvidedBy.ShouldBe([item]);
        topic.RequiredBy.ShouldBeEmpty();
    }
}
