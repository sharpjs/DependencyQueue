// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Diagnostics;

namespace DependencyQueue;

[TestFixture]
public class DependencyQueueTests
{
    [Test]
    public void Construct_DefaultComparer()
    {
        using var queue = new Queue();

        queue           .ShouldBeValid();
        queue.Comparer  .ShouldBeSameAs(StringComparer.Ordinal);
        queue.Count     .ShouldBe(0);
        queue.Topics    .ShouldBeEmpty();
        queue.ReadyItems.ShouldBeEmpty();

        using var view = queue.Inspect();

        view.Queue            .ShouldBeSameAs(queue);
        view.Comparer         .ShouldBeSameAs(queue.Comparer);
        view.Count            .ShouldBe(0);
        view.Topics.Dictionary.ShouldBeSameAs(queue.Topics);
        view.ReadyItems.Queue .ShouldBeSameAs(queue.ReadyItems);

        view.Dispose();

        view.Queue   .ShouldBeSameAs(queue);
        view.Comparer.ShouldBeSameAs(queue.Comparer);

        Should.Throw<ObjectDisposedException>(() => view.Topics);
        Should.Throw<ObjectDisposedException>(() => view.ReadyItems);
    }

    [Test]
    public void Construct_ExplicitComparer()
    {
        var comparer = StringComparer.InvariantCultureIgnoreCase;

        using var queue = new Queue(comparer);

        queue           .ShouldBeValid();
        queue.Comparer  .ShouldBeSameAs(comparer);
        queue.Count     .ShouldBe(0);
        queue.Topics    .ShouldBeEmpty();
        queue.ReadyItems.ShouldBeEmpty();

        using var view = queue.InspectAsync().GetAwaiter().GetResult();

        view.Queue            .ShouldBeSameAs(queue);
        view.Comparer         .ShouldBeSameAs(comparer);
        view.Count            .ShouldBe(0);
        view.Topics.Dictionary.ShouldBeSameAs(queue.Topics);
        view.ReadyItems.Queue .ShouldBeSameAs(queue.ReadyItems);

        view.Dispose();

        view.Queue   .ShouldBeSameAs(queue);
        view.Comparer.ShouldBeSameAs(queue.Comparer);

        Should.Throw<ObjectDisposedException>(() => view.Topics);
        Should.Throw<ObjectDisposedException>(() => view.ReadyItems);
    }

    [Test]
    public void CreateBuilder()
    {
        using var queue = new Queue();

        var builder = queue.CreateBuilder();

        builder      .ShouldNotBeNull();
        builder.Queue.ShouldBeSameAs(queue);
    }

    [Test]
    public void Enqueue_NullName()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentNullException>(
            () => queue.Enqueue(null!, value: new())
        );

        e.ParamName.ShouldBe("name");
    }

    [Test]
    public void Enqueue_EmptyName()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentException>(
            () => queue.Enqueue("", value: new())
        );

        e.ParamName.ShouldBe("name");
    }

    [Test]
    public void Enqueue_NullInProvides()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentException>(
            () => queue.Enqueue("a", value: new(), provides: [null!])
        );

        e.ParamName.ShouldBe("provides");
    }

    [Test]
    public void Enqueue_EmptyInProvides()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentException>(
            () => queue.Enqueue("a", value: new(), provides: [""])
        );

        e.ParamName.ShouldBe("provides");
    }

    [Test]
    public void Enqueue_NullInRequires()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentException>(
            () => queue.Enqueue("a", value: new(), requires: [null!])
        );

        e.ParamName.ShouldBe("requires");
    }

    [Test]
    public void Enqueue_EmptyInRequires()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentException>(
            () => queue.Enqueue("a", value: new(), requires: [""])
        );

        e.ParamName.ShouldBe("requires");
    }

    [Test]
    public void Enqueue_NullItem()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentNullException>(
            () => queue.Enqueue(null!)
        );

        e.ParamName.ShouldBe("item");
    }

    [Test]
    public void Enqueue_IndependentItem()
    {
        using var queue = new Queue();

        var value = new Value();
        var item = queue.Enqueue("a", value, provides: ["b", "b"]); // duplicate is ignored

        item.Name    .ShouldBe("a");
        item.Value   .ShouldBeSameAs(value);
        item.Provides.ShouldBe(["a", "b"]); // name is always provided
        item.Requires.ShouldBeEmpty();

        queue.Count.ShouldBe(1);
        queue.ShouldHaveReadyItems(item);
        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("a", providedBy: [item]);
        queue.ShouldHaveTopic("b", providedBy: [item]);
    }

    [Test]
    public void Enqueue_DependentItem()
    {
        using var queue = new Queue();

        var value = new Value();
        var item = queue.Enqueue("a", value, requires: ["b", "b"]); // duplicate is ignored

        item.Name    .ShouldBe("a");
        item.Value   .ShouldBeSameAs(value);
        item.Provides.ShouldBe(["a"]); // name is always provided
        item.Requires.ShouldBe(["b"]);

        queue.Count.ShouldBe(1);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("a", providedBy: [item]);
        queue.ShouldHaveTopic("b", requiredBy: [item]);
    }

    [Test]
    public void Enqueue_InterdependentEntityNetwork()
    {
        using var queue = new Queue();

        var itemA  = queue.Enqueue("a",  value: new(), requires: ["b"]);
        var itemB0 = queue.Enqueue("b0", value: new(), provides: ["b"]);
        var itemB1 = queue.Enqueue("b1", value: new(), provides: ["b"]);

        queue.Count.ShouldBe(3);
        queue.ShouldHaveReadyItems(itemB0, itemB1);
        queue.ShouldHaveTopicCount(4);
        queue.ShouldHaveTopic("a",  providedBy: [itemA]);
        queue.ShouldHaveTopic("b",  providedBy: [itemB0, itemB1], requiredBy: [itemA]);
        queue.ShouldHaveTopic("b0", providedBy: [itemB0]);
        queue.ShouldHaveTopic("b1", providedBy: [itemB1]);
    }

    [Test]
    public void Enqueue_DuplicateItem()
    {
        using var queue = new Queue();

        var itemA0 = queue.Enqueue("a", value: new());
        var itemA1 = queue.Enqueue("a", value: new());

        itemA0.ShouldNotBeSameAs(itemA1);

        queue.Count.ShouldBe(2);
        queue.ShouldHaveReadyItems(itemA0, itemA1);
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [itemA0, itemA1]);
    }

    [Test]
    public void Enqueue_Disposed()
    {
        var queue = new Queue();

        queue.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => queue.Enqueue("a", value: new())
        );
    }

    [Test]
    public void Validate_Empty()
    {
        using var queue = new Queue();

        queue.Validate().ShouldBeEmpty();
    }

    [Test]
    public void Validate_TopicRequiredButNotProvided()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new(), requires: ["b"]);

        var errors = queue.Validate();

        errors.Count.ShouldBe(1);
        errors[0]
            .ShouldBeOfType<DependencyQueueUnprovidedTopicError<Value>>()
            .AssignTo(out var error);

        error.Topic.Name.ShouldBe("b");
    }

    [Test]
    public void Validate_Cycle_Direct()
    {
        //  [A]─→a─→[B]─→b
        //   ↑           │<error
        //   ╰───────────╯

        using var queue = new Queue();

        var itemA = queue.Enqueue("a", value: new(), requires: ["b"]);
        var itemB = queue.Enqueue("b", value: new(), requires: ["a"]);

        var errors = queue.Validate();

        errors.Count.ShouldBe(1);
        errors[0]
            .ShouldBeOfType<DependencyQueueCycleError<Value>>()
            .AssignTo(out var error);

        error.RequiringItem    .ShouldBeSameAs(itemB);
        error.RequiredTopic.Name.ShouldBe("a");
    }

    [Test]
    public void Validate_Cycle_Indirect()
    {
        //   a←─[A]←─────╮
        //       |       │<error
        //       ↓       │
        //  [B]─→b─→[C]─→c

        using var queue = new Queue();

        var itemA = queue.Enqueue("a", value: new());
        var itemB = queue.Enqueue("b", value: new(), provides: ["a"], requires: ["c"]);
        var itemC = queue.Enqueue("c", value: new(),                  requires: ["a"]);

        var errors = queue.Validate();

        errors.Count.ShouldBe(1);
        errors[0]
            .ShouldBeOfType<DependencyQueueCycleError<Value>>()
            .AssignTo(out var error);

        error.RequiringItem    .ShouldBeSameAs(itemC);
        error.RequiredTopic.Name.ShouldBe("a");
    }

    [Test]
    public void Validate_Disposed()
    {
        var queue = new Queue();

        queue.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => queue.Validate()
        );
    }

    [Test]
    public void Dequeue_Empty()
    {
        using var queue = new Queue();

        queue.Dequeue().ShouldBeNull();
        queue.Count.ShouldBe(0);
    }

    [Test]
    public void Dequeue_Disposed()
    {
        var queue = new Queue();

        queue.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => queue.Dequeue()
        );
    }

    [Test]
    public void Dequeue_Invalid()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new(), requires: ["b"]);

        var e = Should.Throw<InvalidDependencyQueueException>(
            () => queue.Dequeue()
        );

        e.Errors
            .ShouldHaveSingleItem()
            .ShouldBeOfType<DependencyQueueUnprovidedTopicError<Value>>()
            .Topic.Name.ShouldBe("b");
    }

    [Test]
    public void Dequeue_Ok()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();
        queue.Count.ShouldBe(1);
        queue.ShouldHaveReadyItems([item]);
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [item]);

        queue.Dequeue().ShouldBeSameAs(item);

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();                // removed when dequeued
        queue.ShouldHaveTopicCount(1);                  // remains until completed
        queue.ShouldHaveTopic("a", providedBy: [item]); // remains until completed
    }

    [Test]
    public void Dequeue_WaitForRequiredItems()
    {
        using var queue = new Queue();

        var itemA  = queue.Enqueue("a",  value: new(), requires: ["b", "c"]);
        var itemB0 = queue.Enqueue("b0", value: new(), provides: ["b"]);
        var itemB1 = queue.Enqueue("b1", value: new(), provides: ["b"]);
        var itemC  = queue.Enqueue("c",  value: new());

        queue.Dequeue().ShouldBeSameAs(itemB0);
        queue.Dequeue().ShouldBeSameAs(itemB1);
        queue.Dequeue().ShouldBeSameAs(itemC);

        queue.Count.ShouldBe(1);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(5);
        queue.ShouldHaveTopic("a",  providedBy: [itemA]);
        queue.ShouldHaveTopic("b",  providedBy: [itemB0, itemB1], requiredBy: [itemA]);
        queue.ShouldHaveTopic("b0", providedBy: [itemB0]);
        queue.ShouldHaveTopic("b1", providedBy: [itemB1]);
        queue.ShouldHaveTopic("c",  providedBy: [itemC], requiredBy: [itemA]);

        var stopwatch    = new Stopwatch();
        var dequeuedItem = null as object;

        void Dequeue()
        {
            dequeuedItem = queue.Dequeue();
            stopwatch.Stop();
        }

        void CompleteItemB0()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(450));
            queue.Complete(itemB0);
        }

        void CompleteItemB1()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(650));
            queue.Complete(itemB1);
        }

        void CompleteItemC()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            queue.Complete(itemC);
        }

        stopwatch.Start();
        Parallel.Invoke(Dequeue, CompleteItemB0, CompleteItemB1, CompleteItemC);
        //stopwatch.Stop(); is done by Dequeue, above

        dequeuedItem     .ShouldBeSameAs(itemA);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(600));

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [itemA]);
    }

    [Test]
    public void Dequeue_WithPredicate()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        var testedValues = new ConcurrentQueue<Value>();

        bool ReturnTrueOnSecondInvocation(Value value)
        {
            var isFirstInvocation = testedValues.IsEmpty;
            testedValues.Enqueue(value);
            return !isFirstInvocation;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dequeuedItem = queue.Dequeue(ReturnTrueOnSecondInvocation);
        stopwatch.Stop();

        dequeuedItem     .ShouldBeSameAs(item);
        testedValues     .ShouldBe([item.Value, item.Value], ignoreOrder: true);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(950));

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [item]);
    }

    [Test]
    public void Dequeue_Exhausted()
    {
        using var queue = new Queue();

        var itemA = queue.Enqueue("a", value: new(), requires: ["b"]);
        var itemB = queue.Enqueue("b", value: new());

        queue.Dequeue().ShouldBeSameAs(itemB);

        var stopwatch     = new Stopwatch();
        var dequeuedItems = new ConcurrentBag<object?>();

        void Dequeue()
        {
            var dequeuedItem = queue.Dequeue();
            dequeuedItems.Add(dequeuedItem);
            if (dequeuedItem is not null)
                queue.Complete(dequeuedItem);
        }

        void CompleteItemB()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(125));
            queue.Complete(itemB);
        }

        stopwatch.Start();
        Parallel.Invoke(Dequeue, Dequeue, CompleteItemB);
        stopwatch.Stop();

        dequeuedItems    .ShouldBe([itemA, null], ignoreOrder: true);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(75));

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(0);
    }

    [Test]
    public async Task DequeueAsync_Initial()
    {
        using var queue = new Queue();

        (await queue.DequeueAsync()).ShouldBeNull();
    }

    [Test]
    public async Task DequeueAsync_Disposed()
    {
        var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        queue.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(
            () => queue.DequeueAsync()
        );
    }

    [Test]
    public async Task DequeueAsync_Invalid()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new(), requires: ["b"]);

        var e = await Should.ThrowAsync<InvalidDependencyQueueException>(
            () => queue.DequeueAsync()
        );

        e.Errors
            .ShouldHaveSingleItem()
            .ShouldBeOfType<DependencyQueueUnprovidedTopicError<Value>>()
            .Topic.Name.ShouldBe("b");
    }

    [Test]
    public async Task DequeueAsync_Ok()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        (await queue.DequeueAsync()).ShouldBeSameAs(item);

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();                // removed when dequeued
        queue.ShouldHaveTopicCount(1);                  // remains until completed
        queue.ShouldHaveTopic("a", providedBy: [item]); // remains until completed
    }

    [Test]
    public async Task DequeueAsync_WaitForRequiredItems()
    {
        using var queue = new Queue();

        var itemA  = queue.Enqueue("a",  value: new(), requires: ["b", "c"]);
        var itemB0 = queue.Enqueue("b0", value: new(), provides: ["b"]);
        var itemB1 = queue.Enqueue("b1", value: new(), provides: ["b"]);
        var itemC  = queue.Enqueue("c",  value: new());

        (await queue.DequeueAsync()).ShouldBeSameAs(itemB0);
        (await queue.DequeueAsync()).ShouldBeSameAs(itemB1);
        (await queue.DequeueAsync()).ShouldBeSameAs(itemC);

        queue.Count.ShouldBe(1);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(5);
        queue.ShouldHaveTopic("a",  providedBy: [itemA]);
        queue.ShouldHaveTopic("b",  providedBy: [itemB0, itemB1], requiredBy: [itemA]);
        queue.ShouldHaveTopic("b0", providedBy: [itemB0]);
        queue.ShouldHaveTopic("b1", providedBy: [itemB1]);
        queue.ShouldHaveTopic("c",  providedBy: [itemC], requiredBy: [itemA]);

        var stopwatch    = new Stopwatch();
        var dequeuedItem = null as object;

        async Task DequeueAsync()
        {
            dequeuedItem = await queue.DequeueAsync();
            stopwatch.Stop();
        }

        async Task CompleteItemB0Async()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(450));
            queue.Complete(itemB0);
        }

        async Task CompleteItemB1Async()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(650));
            queue.Complete(itemB1);
        }

        async Task CompleteItemCAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            queue.Complete(itemC);
        }

        stopwatch.Start();
        await Task.WhenAll(
            Task.Run(DequeueAsync),
            Task.Run(CompleteItemB0Async),
            Task.Run(CompleteItemB1Async),
            Task.Run(CompleteItemCAsync)
        );
        //stopwatch.Stop(); is done by DequeueAsync, above

        dequeuedItem     .ShouldBeSameAs(itemA);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(600));

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [itemA]);
    }

    [Test]
    public async Task DequeueAsync_WithPredicate()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        var testedValues = new ConcurrentQueue<Value>();

        bool ReturnTrueOnSecondInvocation(Value value)
        {
            var isFirstInvocation = testedValues.IsEmpty;
            testedValues.Enqueue(value);
            return !isFirstInvocation;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dequeuedItem = await queue.DequeueAsync(ReturnTrueOnSecondInvocation);
        stopwatch.Stop();

        dequeuedItem          .ShouldBeSameAs(item);
        testedValues.ToArray().ShouldBe([item.Value, item.Value]);
        stopwatch.Elapsed     .ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900));

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [item]);
    }

    [Test]
    public async Task DequeueAsync_Exhausted()
    {
        using var queue = new Queue();

        var itemA = queue.Enqueue("a", value: new(), requires: ["b"]);
        var itemB = queue.Enqueue("b", value: new());

        (await queue.DequeueAsync()).ShouldBeSameAs(itemB);

        var stopwatch     = new Stopwatch();
        var dequeuedItems = new ConcurrentBag<object?>();

        async Task DequeueAsync()
        {
            var dequeuedItem = await queue.DequeueAsync();
            dequeuedItems.Add(dequeuedItem);
            if (dequeuedItem is not null)
                queue.Complete(dequeuedItem);
        }

        async Task CompleteItemBAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(125));
            queue.Complete(itemB);
        }

        stopwatch.Start();
        await Task.WhenAll(
            Task.Run(DequeueAsync),
            Task.Run(DequeueAsync),
            Task.Run(CompleteItemBAsync)
        );
        stopwatch.Stop();

        dequeuedItems    .ShouldBe([itemA, null], ignoreOrder: true);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(75));

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(0);
    }

    [Test]
    public void Complete_NullItem()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentNullException>(
            () => queue.Complete(null!)
        );

        e.ParamName.ShouldBe("item");
    }

    [Test]
    public void Complete_Disposed()
    {
        var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        queue.Dequeue().ShouldBeSameAs(item);
        queue.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => queue.Complete(item)
        );
    }

    [Test]
    public void Complete_NotDequeued()
    {
        using var queue = new Queue();

        //                                               provided by another
        //                                          required by another    ↓
        //                       would be invalid; a is unprovided    ↓    ↓
        //                                                       ↓    ↓    ↓
        var itemX = queue.Enqueue("x", value: new(), requires: ["a", "j", "k"], provides: ["b"]);
        var itemY = queue.Enqueue("y", value: new(), requires: ["b", "j"],      provides: ["c", "k"]);

        // Complete() works even if the queue is invalid or has not been validated
        //queue.ShouldBeValid();

        // Complete() works even if the item has not been dequeued
        //queue.Dequeue().ShouldBeSameAs(item);

        // Before
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(7);
        queue.ShouldHaveTopic("a", requiredBy: [itemX]                      );
        queue.ShouldHaveTopic("b", requiredBy: [itemY],        providedBy: [itemX]);
        queue.ShouldHaveTopic("c",                             providedBy: [itemY]);
        queue.ShouldHaveTopic("j", requiredBy: [itemX, itemY]                     );
        queue.ShouldHaveTopic("k", requiredBy: [itemX],        providedBy: [itemY]);
        queue.ShouldHaveTopic("x",                             providedBy: [itemX]);
        queue.ShouldHaveTopic("y",                             providedBy: [itemY]);

        // Complete an item that has been enqueued but not dequeued
        queue.Complete(itemX);
        // - Removes topic a because nothing else provides or requires it.
        // - Removes topics b and x because their only provider (x) completed.
        // - Does not remove topic j because it is still required by y
        // - Does not remove topic k because it is still provided by y
        // - Makes y ready because its only requirement (b) is now provided

        // After
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(4);
        queue.ShouldHaveTopic("c",                      providedBy: [itemY]);
        queue.ShouldHaveTopic("j", requiredBy: [itemY]                     );
        queue.ShouldHaveTopic("k",                      providedBy: [itemY]);
        queue.ShouldHaveTopic("y",                      providedBy: [itemY]);
    }

    [Test]
    public void Complete_NotEnqueued()
    {
        using var queue = new Queue();
        using var other = new Queue(); // used only to create an item

        // Not enqueued in 'queue'
        var itemX = other.Enqueue("x", value: new(), requires: ["a"], provides: ["b"]);

        // Enqueued in 'queue'
        var itemY = queue.Enqueue("y", value: new(), requires: ["b"], provides: ["c"]);

        // Before
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(3);
        queue.ShouldHaveTopic("b", requiredBy: [itemY]);
        queue.ShouldHaveTopic("c", providedBy: [itemY]);
        queue.ShouldHaveTopic("y", providedBy: [itemY]);

        // Complete an item that has not been enqueued in this queue
        queue.Complete(itemX);
        // - Removes topic b because its only provider (x, never enqueued) completed.
        // - Makes y ready because its only requirement (b) is now provided.

        // After
        queue.ShouldHaveReadyItems(itemY);
        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("c", providedBy: [itemY]);
        queue.ShouldHaveTopic("y", providedBy: [itemY]);
    }

    [Test]
    public void Clear_Ok()
    {
        using var queue = new Queue();

        var item = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();
        queue.Clear();

        queue.Count.ShouldBe(0);
        queue.ShouldNotHaveReadyItems();
        queue.ShouldHaveTopicCount(0);
        queue.Dequeue().ShouldBeNull();
    }

    [Test]
    public void Clear_Disposed()
    {
        var queue = new Queue();

        queue.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => queue.Clear()
        );
    }

    [Test]
    public void Dispose_Managed()
    {
        using var queue = new Queue();

        queue.Dispose();
        queue.Dispose(); // to test multiple disposes
    }

    [Test]
    public void Dispose_Unmanaged()
    {
        using var queue = new Queue();

        queue.SimulateUnmanagedDispose();
    }
}
