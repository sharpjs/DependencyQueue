// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueItemTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(
            () => new Item(null!)
        ).ParamName.ShouldBe("name");
    }

    [Test]
    public void Construct_EmptyName()
    {
        Should.Throw<ArgumentException>(
             () => new Item("")
         ).ParamName.ShouldBe("name");
    }

    [Test]
    public void Construct_NullComparer()
    {
        Should.Throw<ArgumentNullException>(
            () => new Item("x", new(), null!)
        ).ParamName.ShouldBe("comparer");
    }

    [Test]
    public void CreateView()
    {
        var item = new Item("x");

        using var h = new ViewTestHarness(item);

        h.View.Item.ShouldBeSameAs(item);

        h.Dispose();

        h.View.Item.ShouldBeSameAs(item);
    }

    [Test]
    public void Name_Get()
    {
        var name = "x";
        var item = new Item(name);

        item.Name.ShouldBeSameAs(name);

        using var h = new ViewTestHarness(item);

        h.View.Name.ShouldBeSameAs(name);

        h.Dispose();

        h.View.Name.ShouldBeSameAs(name);
    }

    [Test]
    public void Value_Get()
    {
        var value = new Value();
        var item  = new Item("x", value);

        item.Value.ShouldBeSameAs(value);

        using var h = new ViewTestHarness(item);

        h.View.Value.ShouldBeSameAs(value);

        h.Dispose();

        h.View.Value.ShouldBeSameAs(value);
    }

    [Test]
    public void Provides_Get()
    {
        var item = new Item("x");

        item.Provides.ShouldNotBeNull().ShouldBe(["x"]);

        using var h = new ViewTestHarness(item);

        h.View.Provides/*.ShouldNotBeNull()*/.ShouldBe(["x"]); // value type cannot be null

        h.Dispose();

        Should.Throw<ObjectDisposedException>(() => h.View.Provides);
    }

    [Test]
    public void Requires_Get()
    {
        var item = new Item("x");

        item.Requires.ShouldNotBeNull().ShouldBeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Requires/*.ShouldNotBeNull()*/.ShouldBeEmpty(); // value type cannot be null

        h.Dispose();

        Should.Throw<ObjectDisposedException>(() => h.View.Requires);
    }

    [Test]
    public void AddProvides_NullNameCollection()
    {
        Should.Throw<ArgumentNullException>(
            () => new Item("x").AddProvides(null!)
        ).ParamName.ShouldBe("names");
    }

    [Test]
    public void AddProvides_NullName()
    {
        Should.Throw<ArgumentException>(
            () => new Item("x").AddProvides([null!])
        ).ParamName.ShouldBe("names");
    }

    [Test]
    public void AddProvides_EmptyName()
    {
        Should.Throw<ArgumentException>(
            () => new Item("x").AddProvides([""])
        ).ParamName.ShouldBe("names");
    }

    [Test]
    public void AddProvides_Ok()
    {
        var item = new Item("b");

        item.AddProvides(["A", "C"]);

        item.Provides.ShouldBe(["A", "b", "C"]);
        item.Requires.ShouldBeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["A", "b", "C"]);
        h.View.Requires.ShouldBeEmpty();
    }

    [Test]
    public void AddProvides_Duplicate()
    {
        var item = new Item("a");

        item.AddProvides(["A", "a", "A"]);

        item.Provides.ShouldBe(["a"]);
        item.Requires.ShouldBeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["a"]);
        h.View.Requires.ShouldBeEmpty();
    }

    [Test]
    public void AddProvides_Required()
    {
        var item = new Item("b");

        item.AddRequires(["a"]);
        item.AddProvides(["A"]);

        item.Provides.ShouldBe(["A", "b"]);
        item.Requires.ShouldBeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["A", "b"]);
        h.View.Requires.ShouldBeEmpty();
    }

    [Test]
    public void AddRequires_NullNameCollection()
    {
        Should.Throw<ArgumentNullException>(
            () => new Item("x").AddRequires(null!)
        ).ParamName.ShouldBe("names");
    }

    [Test]
    public void AddRequires_NullName()
    {
        Should.Throw<ArgumentException>(
            () => new Item("x").AddRequires([null!])
        ).ParamName.ShouldBe("names");
    }

    [Test]
    public void AddRequires_EmptyName()
    {
        Should.Throw<ArgumentException>(
            () => new Item("x").AddRequires([""])
        ).ParamName.ShouldBe("names");
    }

    [Test]
    public void AddRequires_Ok()
    {
        var item = new Item("x");

        item.AddRequires(["A", "b", "C"]);

        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBe(["A", "b", "C"]);

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["x"]);
        h.View.Requires.ShouldBe(["A", "b", "C"]);
    }

    [Test]
    public void AddRequires_Duplicate()
    {
        var item = new Item("x");

        item.AddRequires(["a", "A"]);

        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBe(["a"]);

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["x"]);
        h.View.Requires.ShouldBe(["a"]);
    }

    [Test]
    public void AddRequires_Provided()
    {
        var item = new Item("x");

        item.AddProvides(["A"]);
        item.AddRequires(["a"]);

        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBe(["a"]);

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["x"]);
        h.View.Requires.ShouldBe(["a"]);
    }

    [Test]
    public void AddRequires_OwnName()
    {
        var item = new Item("a");

        item.AddRequires(["A"]);

        item.Provides.ShouldBe(["a"]);
        item.Requires.ShouldBeEmpty(); // NOTE: x did *not* become required

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["a"]);
        h.View.Requires.ShouldBeEmpty(); // NOTE: x did *not* become required
    }

    [Test]
    public void RemoveRequires_NullName()
    {
        Should.Throw<ArgumentNullException>(
            () => new Item("x").RemoveRequires(null!)
        ).ParamName.ShouldBe("name");
    }

    [Test]
    public void RemoveRequires_EmptyName()
    {
        Should.Throw<ArgumentException>(
            () => new Item("x").RemoveRequires("")
        ).ParamName.ShouldBe("name");
    }

    [Test]
    public void RemoveRequires_Ok()
    {
        var item = new Item("x");

        item.AddRequires(["a"]);
        item.RemoveRequires("A");

        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["x"]);
        h.View.Requires.ShouldBeEmpty();
    }

    [Test]
    public void RemoveRequires_Duplicate()
    {
        var item = new Item("x");

        item.AddRequires(["a"]);
        item.RemoveRequires("A");
        item.RemoveRequires("A");

        item.Provides.ShouldBe(["x"]);
        item.Requires.ShouldBeEmpty();

        using var h = new ViewTestHarness(item);

        h.View.Provides.ShouldBe(["x"]);
        h.View.Requires.ShouldBeEmpty();
    }

    [Test]
    public void ToString_Ok()
    {
        var item = new Item("a");

        item.AddProvides(["b", "c"]);
        item.AddRequires(["x", "y"]);

        item.ToString().ShouldBe(string.Concat(
            "a {", item.Value.ToString(), "}"
        ));

        using var h = new ViewTestHarness(item);

        h.View.ToString().ShouldBe(string.Concat(
            "a (Provides: a, b, c; Requires: x, y) {", item.Value.ToString(), "}"
        ));

        h.Dispose();

        Should.Throw<ObjectDisposedException>(h.View.ToString);
    }

    [Test]
    public void ToString_NullValue()
    {
        var item = new Item("a", null!);

        item.ToString().ShouldBe("a {null}");

        using var h = new ViewTestHarness(item);

        h.View.ToString().ShouldBe("a (Provides: a; Requires: none) {null}");

        h.Dispose();

        Should.Throw<ObjectDisposedException>(h.View.ToString);
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
