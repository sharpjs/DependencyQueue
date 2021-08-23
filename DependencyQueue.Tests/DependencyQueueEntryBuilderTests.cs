using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using Entry        = DependencyQueueEntry        <object>;
    using EntryBuilder = DependencyQueueEntryBuilder <object>;

    [TestFixture]
    public class DependencyQueueEntryBuilderTests
    {
        #if FALSE

        [Test]
        public void NewEntry()
        {
            var name  = "a";
            var value = new object();

            var entry = new EntryBuilder()
                .NewEntry(name, value)
                .AcceptEntry();

            entry.Name .Should().BeSameAs(name);
            entry.Value.Should().BeSameAs(value);
        }

        [Test]
        public void Value_Get()
        {
            var value = new object();

            var entry = new Entry("a", value);

            entry.Value.Should().BeSameAs(value);
        }

        #endif
    }
}
