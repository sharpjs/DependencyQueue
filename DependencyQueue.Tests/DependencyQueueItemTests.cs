// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueItemTests
{
    [Test]
    public void Construct_NullName()
    {
        Invoking(() => new Item(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "name");
    }

    [Test]
    public void Construct_EmptyName()
    {
        Invoking(() => new Item(""))
            .Should().ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "name");
    }

    [Test]
    public void Construct_NullComparer()
    {
        Invoking(() => new Item("x", new(), null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "comparer");
    }

    [Test]
    public void CreateView()
    {
        var item = new Item("x");

        using var h = new ViewTestHarness(item);

        h.View.Item.Should().BeSameAs(item);

        h.Dispose();

        h.View.Item.Should().BeSameAs(item);
    }

    [Test]
    public void Name_Get()
    {
        var name  = "x";
        var item = new Item(name);

        item.Name.Should().BeSameAs(name);

        using var h = new ViewTestHarness(item);

        h.View.Name.Should().BeSameAs(name);

        h.Dispose();

        h.View.Name.Should().BeSameAs(name);
    }

    [Test]
    public void Value_Get()
    {
        var value = new Value();
        var item = new Item("x", value);

        item.Value.Should().BeSameAs(value);

        using var h = new ViewTestHarness(item);

        h.View.Value.Should().BeSameAs(value);

        h.Dispose();

        h.View.Value.Should().BeSameAs(value);
    }

    [Test]
    public void Provides_Get()
    {
        var item = new Item("x");

        item.Provides.Should().NotBeNull().And.BeEquivalentTo("x");

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().NotBeNull().And.BeEquivalentTo("x");

        h.Dispose();

        h.View.Invoking(v => v.Provides).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Requires_Get()
    {
        var item = new Item("x");

        item.Requires.Should().NotBeNull().And.BeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Requires.Should().NotBeNull().And.BeEmpty();

        h.Dispose();

        h.View.Invoking(v => v.Requires).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void AddProvides_NullNameCollection()
    {
        new Item("x")
            .Invoking(e => e.AddProvides(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "names");
    }

    [Test]
    public void AddProvides_NullName()
    {
        new Item("x")
            .Invoking(e => e.AddProvides(new[] { null as string }!))
            .Should().ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "names");
    }

    [Test]
    public void AddProvides_EmptyName()
    {
        new Item("x")
            .Invoking(e => e.AddProvides(new[] { "" }))
            .Should().ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "names");
    }

    [Test]
    public void AddProvides_Ok()
    {
        var item = new Item("b");

        item.AddProvides(new[] { "A", "C" });

        item.Provides.Should().BeEquivalentTo("A", "b", "C");
        item.Requires.Should().BeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("A", "b", "C");
        h.View.Requires.Should().BeEmpty();
    }

    [Test]
    public void AddProvides_Duplicate()
    {
        var item = new Item("a");

        item.AddProvides(new[] { "A", "a", "A" });

        item.Provides.Should().BeEquivalentTo("a");
        item.Requires.Should().BeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("a");
        h.View.Requires.Should().BeEmpty();
    }

    [Test]
    public void AddProvides_Required()
    {
        var item = new Item("b");

        item.AddRequires(new[] { "a" });
        item.AddProvides(new[] { "A" });

        item.Provides.Should().BeEquivalentTo("A", "b");
        item.Requires.Should().BeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("A", "b");
        h.View.Requires.Should().BeEmpty();
    }

    [Test]
    public void AddRequires_NullNameCollection()
    {
        new Item("x")
            .Invoking(e => e.AddRequires(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "names");
    }

    [Test]
    public void AddRequires_NullName()
    {
        new Item("x")
            .Invoking(e => e.AddRequires(new[] { null as string }!))
            .Should().ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "names");
    }

    [Test]
    public void AddRequires_EmptyName()
    {
        new Item("x")
            .Invoking(e => e.AddRequires(new[] { "" }))
            .Should().ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "names");
    }

    [Test]
    public void AddRequires_Ok()
    {
        var item = new Item("x");

        item.AddRequires(new[] { "A", "b", "C" });

        item.Provides.Should().BeEquivalentTo("x");
        item.Requires.Should().BeEquivalentTo("A", "b", "C");

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("x");
        h.View.Requires.Should().BeEquivalentTo("A", "b", "C");
    }

    [Test]
    public void AddRequires_Duplicate()
    {
        var item = new Item("x");

        item.AddRequires(new[] { "a", "A" });

        item.Provides.Should().BeEquivalentTo("x");
        item.Requires.Should().BeEquivalentTo("a");

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("x");
        h.View.Requires.Should().BeEquivalentTo("a");
    }

    [Test]
    public void AddRequires_Provided()
    {
        var item = new Item("x");

        item.AddProvides(new[] { "A" });
        item.AddRequires(new[] { "a" });

        item.Provides.Should().BeEquivalentTo("x");
        item.Requires.Should().BeEquivalentTo("a");

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("x");
        h.View.Requires.Should().BeEquivalentTo("a");
    }

    [Test]
    public void AddRequires_OwnName()
    {
        var item = new Item("a");

        item.AddRequires(new[] { "A" });

        item.Provides.Should().BeEquivalentTo("a");
        item.Requires.Should().BeEmpty(); // NOTE: x did *not* become required

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("a");
        h.View.Requires.Should().BeEmpty(); // NOTE: x did *not* become required
    }

    [Test]
    public void RemoveRequires_NullName()
    {
        new Item("x")
            .Invoking(e => e.RemoveRequires(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "name");
    }

    [Test]
    public void RemoveRequires_EmptyName()
    {
        new Item("x")
            .Invoking(e => e.RemoveRequires(""))
            .Should().ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "name");
    }

    [Test]
    public void RemoveRequires_Ok()
    {
        var item = new Item("x");

        item.AddRequires(new[] { "a" });
        item.RemoveRequires("A");

        item.Provides.Should().BeEquivalentTo("x");
        item.Requires.Should().BeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("x");
        h.View.Requires.Should().BeEmpty();
    }

    [Test]
    public void RemoveRequires_Duplicate()
    {
        var item = new Item("x");

        item.AddRequires(new[] { "a" });
        item.RemoveRequires("A");
        item.RemoveRequires("A");

        item.Provides.Should().BeEquivalentTo("x");
        item.Requires.Should().BeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.Should().BeEquivalentTo("x");
        h.View.Requires.Should().BeEmpty();
    }

    [Test]
    public void ToString_Ok()
    {
        var item = new Item("a");

        item.AddProvides(new[] { "b", "c" });
        item.AddRequires(new[] { "x", "y" });

        item.ToString().Should().Be(string.Concat(
            "a {", item.Value.ToString(), "}"
        ));

        using var h = new ViewTestHarness(item);

        h.View.ToString().Should().Be(string.Concat(
            "a (Provides: a, b, c; Requires: x, y) {", item.Value.ToString(), "}"
        ));

        h.Dispose();

        h.View.Invoking(h => h.ToString()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void ToString_NullValue()
    {
        var item = new Item("a", null!);

        item.ToString().Should().Be("a {null}");

        using var h = new ViewTestHarness(item);

        h.View.ToString().Should().Be("a (Provides: a; Requires: none) {null}");

        h.Dispose();

        h.View.Invoking(h => h.ToString()).Should().Throw<ObjectDisposedException>();
    }

    private class ViewTestHarness : ViewTestHarnessBase
    {
        public Item.View View { get; }

        public ViewTestHarness(Item item)
        {
            View = new Item.View(item, Lock);
        }
    }
}
