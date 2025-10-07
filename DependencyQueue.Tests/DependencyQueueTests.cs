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

        queue             .ShouldBeValid();
        queue.Comparer    .ShouldBeSameAs(StringComparer.Ordinal);
        queue.Topics      .ShouldBeEmpty();
        queue.ReadyEntries.ShouldBeEmpty();

        using var view = queue.Inspect();

        view.Queue             .ShouldBeSameAs(queue);
        view.Comparer          .ShouldBeSameAs(queue.Comparer);
        view.Topics.Dictionary .ShouldBeSameAs(queue.Topics);
        view.ReadyEntries.Queue.ShouldBeSameAs(queue.ReadyEntries);

        view.Dispose();

        view.Queue   .ShouldBeSameAs(queue);
        view.Comparer.ShouldBeSameAs(queue.Comparer);

        Should.Throw<ObjectDisposedException>(() => view.Topics);
        Should.Throw<ObjectDisposedException>(() => view.ReadyEntries);
    }

    [Test]
    public void Construct_ExplicitComparer()
    {
        var comparer = StringComparer.InvariantCultureIgnoreCase;

        using var queue = new Queue(comparer);

        queue             .ShouldBeValid();
        queue.Comparer    .ShouldBeSameAs(comparer);
        queue.Topics      .ShouldBeEmpty();
        queue.ReadyEntries.ShouldBeEmpty();

        using var view = queue.InspectAsync().GetAwaiter().GetResult();

        view.Queue             .ShouldBeSameAs(queue);
        view.Comparer          .ShouldBeSameAs(comparer);
        view.Topics.Dictionary .ShouldBeSameAs(queue.Topics);
        view.ReadyEntries.Queue.ShouldBeSameAs(queue.ReadyEntries);

        view.Dispose();

        view.Queue   .ShouldBeSameAs(queue);
        view.Comparer.ShouldBeSameAs(queue.Comparer);

        Should.Throw<ObjectDisposedException>(() => view.Topics);
        Should.Throw<ObjectDisposedException>(() => view.ReadyEntries);
    }

    [Test]
    public void CreateEntryBuilder()
    {
        using var queue = new Queue();

        var builder = queue.CreateEntryBuilder();

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
    public void Enqueue_IndependentEntry()
    {
        using var queue = new Queue();

        var value = new Value();
        var entry = queue.Enqueue("a", value, provides: ["b", "b"]); // duplicate is ignored

        entry.Name    .ShouldBe("a");
        entry.Value   .ShouldBeSameAs(value);
        entry.Provides.ShouldBe(["a", "b"]); // name is always provided
        entry.Requires.ShouldBeEmpty();

        queue.ShouldHaveReadyEntries(entry);

        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("a", providedBy: [entry]);
        queue.ShouldHaveTopic("b", providedBy: [entry]);
    }

    [Test]
    public void Enqueue_DependentEntry()
    {
        using var queue = new Queue();

        var value = new Value();
        var entry = queue.Enqueue("a", value, requires: ["b", "b"]); // duplicate is ignored

        entry.Name    .ShouldBe("a");
        entry.Value   .ShouldBeSameAs(value);
        entry.Provides.ShouldBe(["a"]); // name is always provided
        entry.Requires.ShouldBe(["b"]);

        queue.ShouldNotHaveReadyEntries();

        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("a", providedBy: [entry]);
        queue.ShouldHaveTopic("b", requiredBy: [entry]);
    }

    [Test]
    public void Enqueue_InterdependentEntityNetwork()
    {
        using var queue = new Queue();

        var entryA  = queue.Enqueue("a",  value: new(), requires: ["b"]);
        var entryB0 = queue.Enqueue("b0", value: new(), provides: ["b"]);
        var entryB1 = queue.Enqueue("b1", value: new(), provides: ["b"]);

        queue.ShouldHaveReadyEntries(entryB0, entryB1);
        queue.ShouldHaveTopicCount(4);
        queue.ShouldHaveTopic("a",  providedBy: [entryA]);
        queue.ShouldHaveTopic("b",  providedBy: [entryB0, entryB1], requiredBy: [entryA]);
        queue.ShouldHaveTopic("b0", providedBy: [entryB0]);
        queue.ShouldHaveTopic("b1", providedBy: [entryB1]);
    }

    [Test]
    public void Enqueue_DuplicateEntry()
    {
        using var queue = new Queue();

        var entryA0 = queue.Enqueue("a", value: new());
        var entryA1 = queue.Enqueue("a", value: new());

        entryA0.ShouldNotBeSameAs(entryA1);

        queue.ShouldHaveReadyEntries(entryA0, entryA1);
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entryA0, entryA1]);
    }

    [Test]
    public void Enqueue_Ending()
    {
        using var queue = new Queue();

        queue.SetEnding();

        Should.Throw<InvalidOperationException>(
            () => queue.Enqueue("a", value: new())
        );
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

        var entry = queue.Enqueue("a", value: new(), requires: ["b"]);

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

        var entryA = queue.Enqueue("a", value: new(), requires: ["b"]);
        var entryB = queue.Enqueue("b", value: new(), requires: ["a"]);

        var errors = queue.Validate();

        errors.Count.ShouldBe(1);
        errors[0]
            .ShouldBeOfType<DependencyQueueCycleError<Value>>()
            .AssignTo(out var error);

        error.RequiringEntry    .ShouldBeSameAs(entryB);
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

        var entryA = queue.Enqueue("a", value: new());
        var entryB = queue.Enqueue("b", value: new(), provides: ["a"], requires: ["c"]);
        var entryC = queue.Enqueue("c", value: new(),                  requires: ["a"]);

        var errors = queue.Validate();

        errors.Count.ShouldBe(1);
        errors[0]
            .ShouldBeOfType<DependencyQueueCycleError<Value>>()
            .AssignTo(out var error);

        error.RequiringEntry    .ShouldBeSameAs(entryC);
        error.RequiredTopic.Name.ShouldBe("a");
    }

    [Test]
    public void Validate_Ending()
    {
        using var queue = new Queue();

        queue.SetEnding();

        // TODO: Error?
        // Allowed but not very useful
        queue.Validate().ShouldBeEmpty();
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
    public void Dequeue_NotValidated()
    {
        using var queue = new Queue();

        Should.Throw<InvalidOperationException>(
            () => queue.Dequeue()
        );
    }

    [Test]
    public void Dequeue_Empty()
    {
        using var queue = new Queue();

        queue.ShouldBeValid();

        queue.Dequeue().ShouldBeNull();
    }

    [Test]
    public void Dequeue_Ending()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();
        queue.SetEnding();

        queue.Dequeue().ShouldBeNull();

        queue.ShouldHaveReadyEntries(entry);
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entry]);
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
    public void Dequeue_Ok()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();

        queue.ShouldHaveReadyEntries([entry]);
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entry]);

        queue.Dequeue().ShouldBeSameAs(entry);

        queue.ShouldNotHaveReadyEntries();                  // removed when dequeued
        queue.ShouldHaveTopicCount(1);                      // remains until completed
        queue.ShouldHaveTopic("a", providedBy: [entry]);    // remains until completed
    }

    [Test]
    public void Dequeue_WaitForRequiredEntries()
    {
        using var queue = new Queue();

        var entryA  = queue.Enqueue("a",  value: new(), requires: ["b", "c"]);
        var entryB0 = queue.Enqueue("b0", value: new(), provides: ["b"]);
        var entryB1 = queue.Enqueue("b1", value: new(), provides: ["b"]);
        var entryC  = queue.Enqueue("c",  value: new());

        queue.ShouldBeValid();

        queue.Dequeue().ShouldBeSameAs(entryB0);
        queue.Dequeue().ShouldBeSameAs(entryB1);
        queue.Dequeue().ShouldBeSameAs(entryC);

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(5);
        queue.ShouldHaveTopic("a",  providedBy: [entryA]);
        queue.ShouldHaveTopic("b",  providedBy: [entryB0, entryB1], requiredBy: [entryA]);
        queue.ShouldHaveTopic("b0", providedBy: [entryB0]);
        queue.ShouldHaveTopic("b1", providedBy: [entryB1]);
        queue.ShouldHaveTopic("c",  providedBy: [entryC], requiredBy: [entryA]);

        var stopwatch     = new Stopwatch();
        var dequeuedEntry = null as object;

        void Dequeue()
        {
            dequeuedEntry = queue.Dequeue();
            stopwatch.Stop();
        }

        void CompleteEntryB0()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(450));
            queue.Complete(entryB0);
        }

        void CompleteEntryB1()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(650));
            queue.Complete(entryB1);
        }

        void CompleteEntryC()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            queue.Complete(entryC);
        }

        stopwatch.Start();
        Parallel.Invoke(Dequeue, CompleteEntryB0, CompleteEntryB1, CompleteEntryC);
        //stopwatch.Stop(); is done by Dequeue, above

        dequeuedEntry    .ShouldBeSameAs(entryA);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(600));

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entryA]);
    }

    [Test]
    public void Dequeue_WithPredicate()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();

        var testedValues = new ConcurrentQueue<Value>();

        bool ReturnTrueOnSecondInvocation(Value value)
        {
            var isFirstInvocation = testedValues.IsEmpty;
            testedValues.Enqueue(value);
            return !isFirstInvocation;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dequeuedEntry = queue.Dequeue(ReturnTrueOnSecondInvocation);
        stopwatch.Stop();

        dequeuedEntry    .ShouldBeSameAs(entry);
        testedValues     .ShouldBe([entry.Value, entry.Value], ignoreOrder: true);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(950));

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entry]);
    }

    [Test]
    public void Dequeue_Exhausted()
    {
        using var queue = new Queue();

        var entryA = queue.Enqueue("a", value: new(), requires: ["b"]);
        var entryB = queue.Enqueue("b", value: new());

        queue.ShouldBeValid();

        queue.Dequeue().ShouldBeSameAs(entryB);

        var stopwatch       = new Stopwatch();
        var dequeuedEntries = new ConcurrentBag<object?>();

        void Dequeue()
        {
            var dequeuedEntry = queue.Dequeue();
            dequeuedEntries.Add(dequeuedEntry);
            if (dequeuedEntry is not null)
                queue.Complete(dequeuedEntry);
        }

        void CompleteEntryB()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(125));
            queue.Complete(entryB);
        }

        stopwatch.Start();
        Parallel.Invoke(Dequeue, Dequeue, CompleteEntryB);
        stopwatch.Stop();

        dequeuedEntries  .ShouldBe([entryA, null], ignoreOrder: true);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(75));

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(0);
    }

    [Test]
    public async Task DequeueAsync_NotValidated()
    {
        using var queue = new Queue();

        await Should.ThrowAsync<InvalidOperationException>(
            () => queue.DequeueAsync()
        );
    }

    [Test]
    public async Task DequeueAsync_Initial()
    {
        using var queue = new Queue();

        queue.ShouldBeValid();

        (await queue.DequeueAsync()).ShouldBeNull();
    }

    [Test]
    public async Task DequeueAsync_Ending()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();
        queue.SetEnding();

        (await queue.DequeueAsync()).ShouldBeNull();

        queue.ShouldHaveReadyEntries(entry);
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entry]);
    }

    [Test]
    public async Task DequeueAsync_Disposed()
    {
        var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(
            () => queue.DequeueAsync()
        );
    }

    [Test]
    public async Task DequeueAsync_Ok()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();

        (await queue.DequeueAsync()).ShouldBeSameAs(entry);

        queue.ShouldNotHaveReadyEntries();                  // removed when dequeued
        queue.ShouldHaveTopicCount(1);                      // remains until completed
        queue.ShouldHaveTopic("a", providedBy: [entry]);    // remains until completed
    }

    [Test]
    public async Task DequeueAsync_WaitForRequiredEntries()
    {
        using var queue = new Queue();

        var entryA  = queue.Enqueue("a",  value: new(), requires: ["b", "c"]);
        var entryB0 = queue.Enqueue("b0", value: new(), provides: ["b"]);
        var entryB1 = queue.Enqueue("b1", value: new(), provides: ["b"]);
        var entryC  = queue.Enqueue("c",  value: new());

        queue.ShouldBeValid();

        (await queue.DequeueAsync()).ShouldBeSameAs(entryB0);
        (await queue.DequeueAsync()).ShouldBeSameAs(entryB1);
        (await queue.DequeueAsync()).ShouldBeSameAs(entryC);

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(5);

        queue.ShouldHaveTopic("a",  providedBy: [entryA]);
        queue.ShouldHaveTopic("b",  providedBy: [entryB0, entryB1], requiredBy: [entryA]);
        queue.ShouldHaveTopic("b0", providedBy: [entryB0]);
        queue.ShouldHaveTopic("b1", providedBy: [entryB1]);
        queue.ShouldHaveTopic("c",  providedBy: [entryC], requiredBy: [entryA]);

        var stopwatch     = new Stopwatch();
        var dequeuedEntry = null as object;

        async Task DequeueAsync()
        {
            dequeuedEntry = await queue.DequeueAsync();
            stopwatch.Stop();
        }

        async Task CompleteEntryB0Async()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(450));
            queue.Complete(entryB0);
        }

        async Task CompleteEntryB1Async()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(650));
            queue.Complete(entryB1);
        }

        async Task CompleteEntryCAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            queue.Complete(entryC);
        }

        stopwatch.Start();
        await Task.WhenAll(
            Task.Run(DequeueAsync),
            Task.Run(CompleteEntryB0Async),
            Task.Run(CompleteEntryB1Async),
            Task.Run(CompleteEntryCAsync)
        );
        //stopwatch.Stop(); is done by DequeueAsync, above

        dequeuedEntry    .ShouldBeSameAs(entryA);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(600));

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entryA]);
    }

    [Test]
    public async Task DequeueAsync_WithPredicate()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();

        var testedValues = new ConcurrentQueue<Value>();

        bool ReturnTrueOnSecondInvocation(Value value)
        {
            var isFirstInvocation = testedValues.IsEmpty;
            testedValues.Enqueue(value);
            return !isFirstInvocation;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dequeuedEntry = await queue.DequeueAsync(ReturnTrueOnSecondInvocation);
        stopwatch.Stop();

        dequeuedEntry         .ShouldBeSameAs(entry);
        testedValues.ToArray().ShouldBe([entry.Value, entry.Value]);
        stopwatch.Elapsed     .ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(900));

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(1);
        queue.ShouldHaveTopic("a", providedBy: [entry]);
    }

    [Test]
    public async Task DequeueAsync_Exhausted()
    {
        using var queue = new Queue();

        var entryA = queue.Enqueue("a", value: new(), requires: ["b"]);
        var entryB = queue.Enqueue("b", value: new());

        queue.ShouldBeValid();

        (await queue.DequeueAsync()).ShouldBeSameAs(entryB);

        var stopwatch       = new Stopwatch();
        var dequeuedEntries = new ConcurrentBag<object?>();

        async Task DequeueAsync()
        {
            var dequeuedEntry = await queue.DequeueAsync();
            dequeuedEntries.Add(dequeuedEntry);
            if (dequeuedEntry is not null)
                queue.Complete(dequeuedEntry);
        }

        async Task CompleteEntryBAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(125));
            queue.Complete(entryB);
        }

        stopwatch.Start();
        await Task.WhenAll(
            Task.Run(DequeueAsync),
            Task.Run(DequeueAsync),
            Task.Run(CompleteEntryBAsync)
        );
        stopwatch.Stop();

        dequeuedEntries  .ShouldBe([entryA, null], ignoreOrder: true);
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(75));

        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(0);
    }

    [Test]
    public void Complete_NullEntry()
    {
        using var queue = new Queue();

        var e = Should.Throw<ArgumentNullException>(
            () => queue.Complete(null!)
        );

        e.ParamName.ShouldBe("entry");
    }

    [Test]
    public void Complete_Ending()
    {
        using var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();
        queue.Dequeue().ShouldBeSameAs(entry);
        queue.SetEnding();

        // Allowed
        queue.Complete(entry);
    }

    [Test]
    public void Complete_Disposed()
    {
        var queue = new Queue();

        var entry = queue.Enqueue("a", value: new());

        queue.ShouldBeValid();
        queue.Dequeue().ShouldBeSameAs(entry);
        queue.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => queue.Complete(entry)
        );
    }

    [Test]
    public void Complete_NotDequeued()
    {
        using var queue = new Queue();

        //                                            would be invalid; a is unprovided
        //                                            vvvvvvvvvvvvvvv
        var entryX = queue.Enqueue("x", value: new(), requires: ["a"], provides: ["b"]);
        var entryY = queue.Enqueue("y", value: new(), requires: ["b"], provides: ["c"]);

        // Complete() works even if the queue is invalid or has not been validated
        //queue.ShouldBeValid();

        // Complete() works even if the entry has not been dequeued
        //queue.Dequeue().ShouldBeSameAs(entry);

        // Before
        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(5);
        queue.ShouldHaveTopic("a", requiredBy: [entryX]                      );
        queue.ShouldHaveTopic("b", requiredBy: [entryY], providedBy: [entryX]);
        queue.ShouldHaveTopic("c",                       providedBy: [entryY]);
        queue.ShouldHaveTopic("x",                       providedBy: [entryX]);
        queue.ShouldHaveTopic("y",                       providedBy: [entryY]);

        // Complete an entry that has been enqueued but not dequeued
        queue.Complete(entryX);
        // - Removes topic a because nothing else provides or requires it.
        // - Removes topics b and x because their only provider (x) completed.
        // - Makes y ready because its only requirement (b) is now provided

        // After
        queue.ShouldHaveReadyEntries(entryY);
        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("c", providedBy: [entryY]);
        queue.ShouldHaveTopic("y", providedBy: [entryY]);
    }

    [Test]
    public void Complete_NotEnqueued()
    {
        using var queue = new Queue();
        using var other = new Queue(); // used only to create an entry

        // Not enqueued in 'queue'
        var entryX = other.Enqueue("x", value: new(), requires: ["a"], provides: ["b"]);

        // Enqueued in 'queue'
        var entryY = queue.Enqueue("y", value: new(), requires: ["b"], provides: ["c"]);

        // Before
        queue.ShouldNotHaveReadyEntries();
        queue.ShouldHaveTopicCount(3);
        queue.ShouldHaveTopic("b", requiredBy: [entryY]);
        queue.ShouldHaveTopic("c", providedBy: [entryY]);
        queue.ShouldHaveTopic("y", providedBy: [entryY]);

        // Complete an entry that has not been enqueued in this queue
        queue.Complete(entryX);
        // - Removes topic b because its only provider (x, never enqueued) completed.
        // - Makes y ready because its only requirement (b) is now provided.

        // After
        queue.ShouldHaveReadyEntries(entryY);
        queue.ShouldHaveTopicCount(2);
        queue.ShouldHaveTopic("c", providedBy: [entryY]);
        queue.ShouldHaveTopic("y", providedBy: [entryY]);
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
