// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueTopicTests
{
    [Test]
    public void Construct_NullName()
    {
        Invoking(() => new Topic(null!))
            .Should()
            .Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "name");
    }

    [Test]
    public void Construct_EmptyName()
    {
        Invoking(() => new Topic(""))
            .Should()
            .ThrowExactly<ArgumentException>()
            .Where(e => e.ParamName == "name");
    }

    [Test]
    public void CreateView()
    {
        var topic = new Topic("a");

        using var h = new ViewTestHarness(topic);

        h.View.Topic.Should().BeSameAs(topic);

        h.Dispose();

        h.View.Topic.Should().BeSameAs(topic);
    }

    [Test]
    public void Name_Get()
    {
        var name  = "a";
        var topic = new Topic(name);

        topic.Name.Should().BeSameAs(name);

        using var h = new ViewTestHarness(topic);

        h.View.Name.Should().BeSameAs(name);

        h.Dispose();

        h.View.Name.Should().BeSameAs(name);
    }

    [Test]
    public void ProvidedBy_Get_Initial()
    {
        var topic = new Topic("a");

        topic.ProvidedBy.Should().BeEmpty();

        using var h = new ViewTestHarness(topic);

        h.View.ProvidedBy.Should().BeEmpty();

        h.Dispose();

        h.View.Invoking(v => v.ProvidedBy).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void RequiredBy_Get_Initial()
    {
        var topic = new Topic("a");

        topic.RequiredBy.Should().BeEmpty();

        using var h = new ViewTestHarness(topic);

        h.View.RequiredBy.Should().BeEmpty();

        h.Dispose();

        h.View.Invoking(v => v.RequiredBy).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void ProvidedBy_Get_Populated()
    {
        var topic = new Topic("a");

        var entries = new[]
        {
            new Entry("a0", new()),
            new Entry("a1", new()),
            new Entry("a2", new())
        };

        topic.ProvidedBy.AddRange(entries);

        using var h = new ViewTestHarness(topic);

        h.View.ProvidedBy.Select(v => v.Entry).Should().Equal(entries);

        h.Dispose();

        h.View.Invoking(v => v.ProvidedBy).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void RequiredBy_Get_Populated()
    {
        var topic = new Topic("a");

        var entries = new[]
        {
            new Entry("a0", new()),
            new Entry("a1", new()),
            new Entry("a2", new())
        };

        topic.RequiredBy.AddRange(entries);

        using var h = new ViewTestHarness(topic);

        h.View.RequiredBy.Select(v => v.Entry).Should().Equal(entries);

        h.Dispose();

        h.View.Invoking(v => v.RequiredBy).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void ToString_Empty()
    {
        var topic = new Topic("a");

        new Topic("a").ToString().Should().Be("a");

        using var h = new ViewTestHarness(topic);

        h.View.ToString().Should().Be(
            "a (ProvidedBy: none; RequiredBy: none)"
        );

        h.Dispose();

        h.View.Invoking(v => v.ToString()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void ToString_Populated()
    {
        var topic = new Topic("a");

        topic.ProvidedBy.AddRange(new[]
        {
            new Entry("b", new())
        });

        topic.RequiredBy.AddRange(new[]
        {
            new Entry("c", new()),
            new Entry("d", new())
        });

        new Topic("a").ToString().Should().Be("a");

        using var h = new ViewTestHarness(topic);

        h.View.ToString().Should().Be(
            "a (ProvidedBy: b; RequiredBy: c, d)"
        );

        h.Dispose();

        h.View.Invoking(v => v.ToString()).Should().Throw<ObjectDisposedException>();
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
