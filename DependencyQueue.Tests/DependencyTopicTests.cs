using System;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static FluentActions;

    [TestFixture]
    public class DependencyTopicTests
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
        public void Name_Get()
        {
            var name = "a";

            new Topic(name).Name.Should().BeSameAs(name);
        }

        [Test]
        public void ProvidedBy_Get_Initial()
        {
            var topic = new Topic("a");

            topic.        ProvidedBy.Should().BeEmpty();
            topic.InternalProvidedBy.Should().BeEmpty();
        }

        [Test]
        public void RequiredBy_Get_Initial()
        {
            var topic = new Topic("a");

            topic.        RequiredBy.Should().BeEmpty();
            topic.InternalRequiredBy.Should().BeEmpty();
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

            topic.InternalProvidedBy.AddRange(entries);

            topic.ProvidedBy.Should().Equal(entries);
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

            topic.InternalRequiredBy.AddRange(entries);

            topic.RequiredBy.Should().Equal(entries);
        }

        [Test]
        public void ToString_Empty()
        {
            new Topic("a").ToString().Should().Be(
                "a"
                //"a (ProvidedBy: none; RequiredBy: none)"
            );
        }

        [Test]
        public void ToString_Populated()
        {
            var topic = new Topic("a");

            topic.InternalProvidedBy.AddRange(new[]
            {
                new Entry("b", new())
            });

            topic.InternalRequiredBy.AddRange(new[]
            {
                new Entry("c", new()),
                new Entry("d", new())
            });

            topic.ToString().Should().Be(
                "a"
                //"a (ProvidedBy: b; RequiredBy: c, d)"
            );
        }
    }
}
