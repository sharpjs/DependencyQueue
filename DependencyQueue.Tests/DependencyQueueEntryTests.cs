using System;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static FluentActions;

    [TestFixture]
    public class DependencyQueueEntryTests
    {
        [Test]
        public void Construct_NullName()
        {
            Invoking(() => new Entry(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "name");
        }

        [Test]
        public void Construct_EmptyName()
        {
            Invoking(() => new Entry(""))
                .Should().ThrowExactly<ArgumentException>()
                .Where(e => e.ParamName == "name");
        }

        [Test]
        public void Construct_NullComparer()
        {
            Invoking(() => new Entry("x", new(), null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "comparer");
        }

        [Test]
        public void CreateView()
        {
            var entry = new Entry("x");

            using var h = new ViewTestHarness(entry);

            h.View.Entry.Should().BeSameAs(entry);

            h.Dispose();

            h.View.Entry.Should().BeSameAs(entry);
        }

        [Test]
        public void Name_Get()
        {
            var name  = "x";
            var entry = new Entry(name);

            entry.Name.Should().BeSameAs(name);

            using var h = new ViewTestHarness(entry);

            h.View.Name.Should().BeSameAs(name);

            h.Dispose();

            h.View.Name.Should().BeSameAs(name);
        }

        [Test]
        public void Value_Get()
        {
            var value = new Value();
            var entry = new Entry("x", value);

            entry.Value.Should().BeSameAs(value);

            using var h = new ViewTestHarness(entry);

            h.View.Value.Should().BeSameAs(value);

            h.Dispose();

            h.View.Value.Should().BeSameAs(value);
        }

        [Test]
        public void Provides_Get()
        {
            var entry = new Entry("x");

            entry.Provides.Should().NotBeNull().And.BeEquivalentTo("x");

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().NotBeNull().And.BeEquivalentTo("x");

            h.Dispose();

            h.View.Invoking(v => v.Provides).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void Requires_Get()
        {
            var entry = new Entry("x");

            entry.Requires.Should().NotBeNull().And.BeEmpty();

            using var h = new ViewTestHarness(entry);

            h.View.Requires.Should().NotBeNull().And.BeEmpty();

            h.Dispose();

            h.View.Invoking(v => v.Requires).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void AddProvides_NullNameCollection()
        {
            new Entry("x")
                .Invoking(e => e.AddProvides(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "names");
        }

        [Test]
        public void AddProvides_NullName()
        {
            new Entry("x")
                .Invoking(e => e.AddProvides(new[] { null as string }!))
                .Should().ThrowExactly<ArgumentException>()
                .Where(e => e.ParamName == "names");
        }

        [Test]
        public void AddProvides_EmptyName()
        {
            new Entry("x")
                .Invoking(e => e.AddProvides(new[] { "" }))
                .Should().ThrowExactly<ArgumentException>()
                .Where(e => e.ParamName == "names");
        }

        [Test]
        public void AddProvides_Ok()
        {
            var entry = new Entry("b");

            entry.AddProvides(new[] { "A", "C" });

            entry.Provides.Should().BeEquivalentTo("A", "b", "C");
            entry.Requires.Should().BeEmpty();

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("A", "b", "C");
            h.View.Requires.Should().BeEmpty();
        }

        [Test]
        public void AddProvides_Duplicate()
        {
            var entry = new Entry("a");

            entry.AddProvides(new[] { "A", "a", "A" });

            entry.Provides.Should().BeEquivalentTo("a");
            entry.Requires.Should().BeEmpty();

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("a");
            h.View.Requires.Should().BeEmpty();
        }

        [Test]
        public void AddProvides_Required()
        {
            var entry = new Entry("b");

            entry.AddRequires(new[] { "a" });
            entry.AddProvides(new[] { "A" });

            entry.Provides.Should().BeEquivalentTo("A", "b");
            entry.Requires.Should().BeEmpty();

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("A", "b");
            h.View.Requires.Should().BeEmpty();
        }

        [Test]
        public void AddRequires_NullNameCollection()
        {
            new Entry("x")
                .Invoking(e => e.AddRequires(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "names");
        }

        [Test]
        public void AddRequires_NullName()
        {
            new Entry("x")
                .Invoking(e => e.AddRequires(new[] { null as string }!))
                .Should().ThrowExactly<ArgumentException>()
                .Where(e => e.ParamName == "names");
        }

        [Test]
        public void AddRequires_EmptyName()
        {
            new Entry("x")
                .Invoking(e => e.AddRequires(new[] { "" }))
                .Should().ThrowExactly<ArgumentException>()
                .Where(e => e.ParamName == "names");
        }

        [Test]
        public void AddRequires_Ok()
        {
            var entry = new Entry("x");

            entry.AddRequires(new[] { "A", "b", "C" });

            entry.Provides.Should().BeEquivalentTo("x");
            entry.Requires.Should().BeEquivalentTo("A", "b", "C");

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("x");
            h.View.Requires.Should().BeEquivalentTo("A", "b", "C");
        }

        [Test]
        public void AddRequires_Duplicate()
        {
            var entry = new Entry("x");

            entry.AddRequires(new[] { "a", "A" });

            entry.Provides.Should().BeEquivalentTo("x");
            entry.Requires.Should().BeEquivalentTo("a");

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("x");
            h.View.Requires.Should().BeEquivalentTo("a");
        }

        [Test]
        public void AddRequires_Provided()
        {
            var entry = new Entry("x");

            entry.AddProvides(new[] { "A" });
            entry.AddRequires(new[] { "a" });

            entry.Provides.Should().BeEquivalentTo("x");
            entry.Requires.Should().BeEquivalentTo("a");

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("x");
            h.View.Requires.Should().BeEquivalentTo("a");
        }

        [Test]
        public void AddRequires_OwnName()
        {
            var entry = new Entry("a");

            entry.AddRequires(new[] { "A" });

            entry.Provides.Should().BeEquivalentTo("a");
            entry.Requires.Should().BeEmpty(); // NOTE: x did *not* become required

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("a");
            h.View.Requires.Should().BeEmpty(); // NOTE: x did *not* become required
        }

        [Test]
        public void RemoveRequires_NullName()
        {
            new Entry("x")
                .Invoking(e => e.RemoveRequires(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "name");
        }

        [Test]
        public void RemoveRequires_EmptyName()
        {
            new Entry("x")
                .Invoking(e => e.RemoveRequires(""))
                .Should().ThrowExactly<ArgumentException>()
                .Where(e => e.ParamName == "name");
        }

        [Test]
        public void RemoveRequires_Ok()
        {
            var entry = new Entry("x");

            entry.AddRequires(new[] { "a" });
            entry.RemoveRequires("A");

            entry.Provides.Should().BeEquivalentTo("x");
            entry.Requires.Should().BeEmpty();

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("x");
            h.View.Requires.Should().BeEmpty();
        }

        [Test]
        public void RemoveRequires_Duplicate()
        {
            var entry = new Entry("x");

            entry.AddRequires(new[] { "a" });
            entry.RemoveRequires("A");
            entry.RemoveRequires("A");

            entry.Provides.Should().BeEquivalentTo("x");
            entry.Requires.Should().BeEmpty();

            using var h = new ViewTestHarness(entry);

            h.View.Provides.Should().BeEquivalentTo("x");
            h.View.Requires.Should().BeEmpty();
        }

        [Test]
        public void ToString_Ok()
        {
            var entry = new Entry("a");

            entry.AddProvides(new[] { "b", "c" });
            entry.AddRequires(new[] { "x", "y" });

            entry.ToString().Should().Be(string.Concat(
                "a {", entry.Value.ToString(), "}"
            ));

            using var h = new ViewTestHarness(entry);

            h.View.ToString().Should().Be(string.Concat(
                "a (Provides: a, b, c; Requires: x, y) {", entry.Value.ToString(), "}"
            ));

            h.Dispose();

            h.View.Invoking(h => h.ToString()).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void ToString_NullValue()
        {
            var entry = new Entry("a", null!);

            entry.ToString().Should().Be("a {null}");

            using var h = new ViewTestHarness(entry);

            h.View.ToString().Should().Be("a (Provides: a; Requires: none) {null}");

            h.Dispose();

            h.View.Invoking(h => h.ToString()).Should().Throw<ObjectDisposedException>();
        }

        private class ViewTestHarness : ViewTestHarnessBase
        {
            public Entry.View View { get; }

            public ViewTestHarness(Entry entry)
            {
                View = new Entry.View(entry, Lock);
            }
        }
    }
}
