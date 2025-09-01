// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;

namespace DependencyQueue;

[TestFixture]
public class PredicateQueueTests
{
    [Test]
    public void Construct_Default()
    {
        var queue = new PredicateQueue<string>();

        queue.Count.ShouldBe(0);
        queue      .ShouldBeEmpty();
    }

    [Test]
    public void Construct_Enumerable()
    {
        var queue = new PredicateQueue<string?>(["a", null, "b"]);

        queue.Count.ShouldBe(3);
        queue      .ShouldBe(["a", null, "b"]);
    }

    [Test]
    public void Construct_Enumerable_Null()
    {
        Should.Throw<ArgumentNullException>(() =>
        {
            _ = new PredicateQueue<string>(null!);
        });
    }

    [Test]
    public void Enqueue_Single()
    {
        var queue = new PredicateQueue<string>();

        queue.Enqueue("a");

        queue.Count.ShouldBe(1);
        queue      .ShouldHaveSingleItem("a");
    }

    [Test]
    public void Enqueue_Multiple()
    {
        var queue = new PredicateQueue<string?>();

        queue.Enqueue("a");
        queue.Enqueue(null);
        queue.Enqueue("b");

        queue.Count.ShouldBe(3);
        queue      .ShouldBe(["a", null, "b"]);
    }

    [Test]
    public void Peek_Empty()
    {
        var queue = new PredicateQueue<string>();

        Should.Throw<InvalidOperationException>(queue.Peek);
    }

    [Test]
    public void Peek_NotEmpty()
    {
        var queue = new PredicateQueue<string>(["a", "b"]);

        queue.Peek().ShouldBe("a");

        queue.Count.ShouldBe(2);
        queue      .ShouldBe(["a", "b"]);
    }

    [Test]
    public void TryPeek_Empty()
    {
        var queue = new PredicateQueue<string>();

        var result = queue.TryPeek(out var item);

        result.ShouldBeFalse();
        item  .ShouldBeNull();
    }

    [Test]
    public void TryPeek_NotEmpty()
    {
        var queue = new PredicateQueue<string>(["a", "b"]);

        var result = queue.TryPeek(out var item);

        result.ShouldBeTrue();
        item  .ShouldBe("a");

        queue.Count.ShouldBe(2);
        queue      .ShouldBe(["a", "b"]);
    }

    [Test]
    public void TryDequeue_Empty()
    {
        var queue = new PredicateQueue<string>();

        var result = queue.TryDequeue(s => s, _ => true, out var item);

        result.ShouldBeFalse();
        item  .ShouldBeNull();
    }

    [Test]
    public void TryDequeue_Match()
    {
        var queue = new PredicateQueue<string?>(["a", null, "b", "c"]);

        var result = queue.TryDequeue(s => s?[0], c => c >= 'b', out var item);

        result.ShouldBeTrue();
        item  .ShouldBe("b");

        queue.Count.ShouldBe(3);
        queue      .ShouldBe(["a", null, "c"]);

        result = queue.TryDequeue(s => s, s => s is null, out item);

        result.ShouldBeTrue();
        item  .ShouldBe(null);

        queue.Count.ShouldBe(2);
        queue      .ShouldBe(["a", "c"]);
    }

    [Test]
    public void TryDequeue_NoMatch()
    {
        var queue = new PredicateQueue<string>(["a", "b", "c"]);

        var result = queue.TryDequeue(s => s[0], char.IsDigit, out var item);

        result.ShouldBeFalse();
        item  .ShouldBeNull();

        queue.Count.ShouldBe(3);
        queue      .ShouldBe(["a", "b", "c"]);
    }

    [Test]
    public void TryDequeue_ThenEnqueue()
    {
        var queue = new PredicateQueue<string>(["a", "b", "c"]);
        
        // Remove some items to create free slots
        queue.TryDequeue(s => s, s => s == "c", out _);
        queue.TryDequeue(s => s, s => s == "a", out _);

        // New items should reuse free slots
        queue.Enqueue("d");
        queue.Enqueue("e");
        
        queue.Count.ShouldBe(3);
        queue      .ShouldBe(["b", "d", "e"]);
    }

    [Test]
    public void TryDequeue_NullConverter()
    {
        var queue = new PredicateQueue<string>(["a"]);

        Should.Throw<ArgumentNullException>(() =>
        {
            queue.TryDequeue<string>(null!, _ => true, out _);
        });
    }

    [Test]
    public void TryDequeue_NullPredicate()
    {
        var queue = new PredicateQueue<string>(["a"]);

        Should.Throw<ArgumentNullException>(() =>
        {
            queue.TryDequeue(s => s, null!, out _);
        });
    }

    [Test]
    public void GetEnumerator_Empty()
    {
        var queue = new PredicateQueue<string>();

        using var enumerator = queue.GetEnumerator();

        enumerator.MoveNext().ShouldBeFalse();
    }

    [Test]
    public void GetEnumerator_NonEmpty()
    {
        var queue = new PredicateQueue<string?>(["a", null, "b"]);

        using var enumerator = queue.GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("a");
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe(null);
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("b");
        enumerator.MoveNext().ShouldBeFalse();
        enumerator.MoveNext().ShouldBeFalse(); // still false
    }

    [Test]
    public void GetEnumerator_ExplicitGeneric()
    {
        var queue = new PredicateQueue<string?>(["a", null, "b"]);

        using var enumerator = ((IEnumerable<string?>) queue).GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("a");
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe(null);
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("b");
        enumerator.MoveNext().ShouldBeFalse();
    }

    [Test]
    public void GetEnumerator_ExplicitNonGeneric()
    {
        var queue = new PredicateQueue<string?>(["a", null, "b"]);

        var enumerator = ((IEnumerable) queue).GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("a");
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe(null);
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("b");
        enumerator.MoveNext().ShouldBeFalse();
    }
}

[TestFixture]
public class PredicateQueueEnumeratorTests
{
    [Test]
    public void Current_BeforeFirst()
    {
        var queue = new PredicateQueue<string>(["a"]);

        using var enumerator = queue.GetEnumerator();

        Should.Throw<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    [Test]
    public void Current_AfterLast()
    {
        var queue = new PredicateQueue<string>(["a"]);

        using var enumerator = queue.GetEnumerator();
        
        enumerator.MoveNext();
        enumerator.MoveNext(); // Move past end
        
        Should.Throw<InvalidOperationException>(() => { _ = enumerator.Current; });
    }

    [Test]
    public void Current_NonGeneric_Ok()
    {
        var queue = new PredicateQueue<string>(["a"]);

        using var enumerator = queue.GetEnumerator();

        enumerator.MoveNext();

        ((IEnumerator) enumerator).Current.ShouldBe("a");
    }

    [Test]
    public void Current_NonGeneric_BeforeFirst()
    {
        var queue = new PredicateQueue<string>(["a"]);

        using var enumerator = queue.GetEnumerator();

        Should.Throw<InvalidOperationException>(() => { _ = ((IEnumerator) enumerator).Current; });
    }

    [Test]
    public void Current_NonGeneric_AfterLast()
    {
        var queue = new PredicateQueue<string>(["a"]);

        using var enumerator = queue.GetEnumerator();
        
        enumerator.MoveNext();
        enumerator.MoveNext(); // Move past end
        
        Should.Throw<InvalidOperationException>(() => { _ = ((IEnumerator) enumerator).Current; });
    }

    [Test]
    public void Reset()
    {
        var queue = new PredicateQueue<string>(["a", "b"]);

        using var enumerator = queue.GetEnumerator();

        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.Reset();
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("a");
        enumerator.MoveNext().ShouldBeTrue(); enumerator.Current.ShouldBe("b");
        enumerator.MoveNext().ShouldBeFalse();
    }

    [Test]
    public void Dispose_Multiple()
    {
        var queue = new PredicateQueue<string>(["a"]);

        using var enumerator = queue.GetEnumerator();

        enumerator.MoveNext();
        enumerator.Dispose();
        // disposed again here
    }
}
