// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueTopicTests
{
    [Test]
    public void Construct_NullName()
    {
        Should.Throw<ArgumentNullException>(
            () => new Topic(null!)
        ).ParamName.ShouldBe("name");
    }

    [Test]
    public void Construct_EmptyName()
    {
        Should.Throw<ArgumentException>(
            () => new Topic("")
        ).ParamName.ShouldBe("name");
    }

    [Test]
    public void CreateView()
    {
        var topic = new Topic("a");

        using var h = new ViewTestHarness(topic);

        h.View.Topic.ShouldBeSameAs(topic);

        h.Dispose();

        h.View.Topic.ShouldBeSameAs(topic);
    }

    [Test]
    public void Name_Get()
    {
        var name  = "a";
        var topic = new Topic(name);

        topic.Name.ShouldBeSameAs(name);

        using var h = new ViewTestHarness(topic);

        h.View.Name.ShouldBeSameAs(name);

        h.Dispose();

        h.View.Name.ShouldBeSameAs(name);
    }

    [Test]
    public void ProvidedBy_Get_Initial()
    {
        var topic = new Topic("a");

        topic.ProvidedBy.ShouldBeEmpty();

        using var h = new ViewTestHarness(topic);

        h.View.ProvidedBy.ShouldBeEmpty();

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.ProvidedBy
        );
    }

    [Test]
    public void RequiredBy_Get_Initial()
    {
        var topic = new Topic("a");

        topic.RequiredBy.ShouldBeEmpty();

        using var h = new ViewTestHarness(topic);

        h.View.RequiredBy.ShouldBeEmpty();

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.RequiredBy
        );
    }

    [Test]
    public void ProvidedBy_Get_Populated()
    {
        var topic = new Topic("a");

        var items = new[]
        {
            new Item("a0", new()),
            new Item("a1", new()),
            new Item("a2", new())
        };

        topic.ProvidedBy.AddRange(items);

        using var h = new ViewTestHarness(topic);

        h.View.ProvidedBy.Select(v => v.Item).ShouldBe(items);

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.ProvidedBy
        );
    }

    [Test]
    public void RequiredBy_Get_Populated()
    {
        var topic = new Topic("a");

        var items = new[]
        {
            new Item("a0", new()),
            new Item("a1", new()),
            new Item("a2", new())
        };

        topic.RequiredBy.AddRange(items);

        using var h = new ViewTestHarness(topic);

        h.View.RequiredBy.Select(v => v.Item).ShouldBe(items);

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.RequiredBy
        );
    }

    [Test]
    public void ToString_Empty()
    {
        var topic = new Topic("a");

        new Topic("a").ToString().ShouldBe("a");

        using var h = new ViewTestHarness(topic);

        h.View.ToString().ShouldBe(
            "a (ProvidedBy: none; RequiredBy: none)"
        );

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.ToString()
        );
    }

    [Test]
    public void ToString_Populated()
    {
        var topic = new Topic("a");

        topic.ProvidedBy.AddRange(new[]
        {
            new Item("b", new())
        });

        topic.RequiredBy.AddRange(new[]
        {
            new Item("c", new()),
            new Item("d", new())
        });

        new Topic("a").ToString().ShouldBe("a");

        using var h = new ViewTestHarness(topic);

        h.View.ToString().ShouldBe(
            "a (ProvidedBy: b; RequiredBy: c, d)"
        );

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.ToString()
        );
    }

    private class ViewTestHarness : ViewTestHarnessBase
    {
        public Topic.View View { get; }

        public ViewTestHarness(Topic topic)
        {
            View = new Topic.View(topic, Lock);
        }
    }
}
