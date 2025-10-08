# DependencyQueue

A dependency queue for .NET: a thread-safe, generic queue that dequeues
elements in dependency order.

## Status

[![Build](https://github.com/sharpjs/DependencyQueue/workflows/Build/badge.svg)](https://github.com/sharpjs/DependencyQueue/actions)
[![NuGet](https://img.shields.io/nuget/v/DependencyQueue.svg)](https://www.nuget.org/packages/DependencyQueue)
[![NuGet](https://img.shields.io/nuget/dt/DependencyQueue.svg)](https://www.nuget.org/packages/DependencyQueue)

- **Tested:**     100% coverage by automated tests.
- **Documented:** IntelliSense on everything.  Guide below.

## Installation

Install [this NuGet Package](https://www.nuget.org/packages/DependencyQueue)
in your project.

## Usage

DependencyQueue provides a specialized queue that works differently than a
typical FIFO (first in, first out) queue.  The queue accepts enqueued items in
any order but yields dequeued items in an order that respects dependencies: an
item is not dequeued until any items on which it depends have been dequeued.
If one must ascribe a catchy initialism to such a queue, a nice one is WIRDO —
**w**hatever **i**n, **r**everse **d**ependency **o**ut.

If an example would be helpful, skip to the [Examples](#examples) section.

### Creation

The queue class is the generic `DependencyQueue<T>`, which supports simple
creation via its constructor.

```csharp
using var queue = new DependencyQueue<Step>();
```

Because the queue class implements `IDisposable`, make sure to guarantee
eventual disposal of queue instances, via a `using` block or other means.

### Enqueueing Items

A `DependencyQueue<T>` instance accepts new items of type `T`.  Each item is
contained within an 'entry' structure which describes how the item relates
dependency-wise to the other items in the queue.  DependencyQueue provides two
ways to create and enqueue entries: `Enqueue()` and a builder object.  To
obtain a builder object, call `CreateEntryBuilder()`.

```csharp
var builder = queue.CreateEntryBuilder();
```

To begin a new entry, call `NewEntry()` on the builder, passing both a name for
the entry and the item the entry should contain.  To add the entry to the
queue, call `Enqueue()`.  The builder is then reusable for another entry.

```csharp
builder
    .NewEntry("MyItem", theItem)
    .Enqueue();
```

To indicate that an item requires some other item to be dequeued first, call
`AddRequires()` on the builder, passing one or more names of entries on which
the current entry will depend.

```csharp
builder
    .NewEntry("MyItem", theItem)
    .AddRequires("ThingINeedA", "ThingINeedB")  // accepts multiple names
    .AddRequires("ThingINeedC")                 // can use multiple times
    .Enqueue();
```

Sometimes an item is part of some larger whole, and it is useful to give that
whole a name so that other entries can depend on it.  At other times, it is
useful for an entry to have more the one name.  To add more names to the
current entry, call `AddProvides()` on the builder, passing one or more extra
names for the entry.

```csharp
builder
    .NewEntry("MyItem", theItem)
    .AddProvides("BigThingIAmPartOf", "MyAlias")  // accepts multiple names
    .AddProvides("AnotherAlias")                  // can use multiple times
    .Enqueue();
```

As an alternative to the builder pattern, DependencyQueue also provides an
`Enqueue()` method that can create and enqueue an entry in one call.  The
tradeoff is that all the information about the entry must be specified in that
one call.

```csharp
queue.Enqueue(
    name:     "MyItem",
    value:    theItem,
    requires: ["ThingINeedA", "ThingINeedB", "ThingINeedC"],
    provides: ["BigThingIAmPartOf", "MyAlias", "AnotherAlias"]
);
```

Names passed to `Enqueue()`, `NewEntry()`, `AddRequires()`, and `AddProvides()`
can be any non-null, non-empty strings.  **Duplicate names are allowed** and in
fact are often useful.  Each name defines a 'topic'.  Other than its name, a
topic is just a pair of lists:

- a list of which entries *provide* that topic via `NewEntry()` or `AddProvides()`, and
- a list of which entries *require* that topic via `AddRequires()`.

By default, topic names use case-sensitive ordinal comparison.  To use
different comparison rules, pass a `StringComparer` instance to the queue
constructor.

`NewEntry()` places no restriction on the item passed as its `value` argument.

### Dequeueing Items

To dequeue entries, call `Dequeue()` or `DequeueAsync()`.  Both methods yield
the next entry in the queue, or `null` if the queue is empty.

```csharp
var entry = queue.Dequeue();
```
```csharp
var entry = await queue.DequeueAsync(cancellation: cancellationToken);
```

The item is available in the `Value` property of the returned entry.

When the code that dequeued the entry is done with the entry, the code must
call `Complete()` to inform the queue.

```csharp
queue.Complete(entry);
```

`Dequeue()`, `DequeueAsync()`, and `Complete()` are thread-safe.  For full
thread safety information, see the [Thread Safety](#thread-safety) section
below.

The dequeue methods support an optional predicate parameter.  If the caller
provides a predicate, the queue tests each ready-to-dequeue item against the
predicate and yields the first entry for which the predicate returns `true`.
If the predicate does not return `true` for any ready-to-dequeue item, then
the dequeue method blocks until an item becomes available that does satisfy the
predicate.

```csharp
var entry = await queue.DequeueAsync(
    item => MyCustomPredicate(item),
    cancellationToken
);
```

### Validation

After enqueueing an item, a `DependencyQueue<T>` instance is in an unvalidated
state.  Subsequent invocation of `Dequeue()` or `DequeueAsync()` automatically
triggers validation of the queue as needed.  Both dequeue methods then throw
`InvalidDependencyQueueException` if the queue is found to be invalid.  The
`Errors` property of the exception details why the queue is invalid

To validate the queue explicitly without dequeuing any items, call
`Validate()`.

```csharp
var errors = queue.Validate();
```

If the queue is valid, `Validate()` returns an empty list of error objects.
Otherwise, the list describes the problems found.

The web of dependencies between entries — the 'dependency graph' — can be
invalid in two ways.

1. **A topic is required but not provided by any entry.**
   This can happen due to a typo in a topic name or because an expected entry
   was never enqueued.

2. **The dependency graph contains a cycle.**
   This occurs when an entry depends on itself, either directly or indirectly.

### Inspection

To peek into a queue, the `DependencyQueue<T>` class provides the `Inspect()`
and `InspectAsync()` methods.  These methods acquire an exclusive lock on the
queue and return a read-only view of the queue.  The view holds the exclusive
lock until disposed.

```csharp
using var view = queue.Inspect();

_ = view.ReadyEntries;                  // Entries ready to be dequeued
_ = view.ReadyEntries.First();          // First ready entry
_ = view.ReadyEntries.First().Provides; // Names of topics it provides
_ = view.ReadyEntries.First().Requires; // Names of topics it requires
_ = view.Topics;                        // Dictionary of topics keyed by name
_ = view.Topics["Foo"];                 // Topic "foo"
_ = view.Topics["Foo"].ProvidedBy;      // Entries that provide topic "Foo"
_ = view.Topics["Foo"].RequiredBy;      // Entries that require topic "Foo"
```

### Thread Safety

Most methods of `DependencyQueue<T>` are thread-safe.  Specifically:

- The `Enqueue()` method is thread-safe.

- The object returned by `CreateEntryBuilder()` is not thread-safe, but
  multiple threads can each use their own builder instance to enqueue items in
  parallel.

- The `Validate()` method is thread-safe.

- The dequeue methods (`Dequeue()`, `DequeueAsync()`, and `Complete()`)
  are thread-safe.

- The inspection methods (`Inspect()` and `InspectAsync()`) are thread-safe, as
  are the objects they return.

- The `Clear()` method is thread-safe.

- The `Dispose()` method is <strong>NOT</strong> thread-safe.

## Examples

### Basic Usage

Imagine a program that cooks a basic hamburger.  The program can add steps to a
dependency queue in any order, and the queue will yield back the steps in the
correct order to prepare a burger.

The values in the queue are `Step` objects.  For the sake of the example, it is
not important what a 'step' is.  Just imagine that the `Step` class has an
`Execute()` method that performs the step.

```csharp
// Create a queue
using var queue = new DependencyQueue<Step>();

// Create a builder for queue entries
var builder = queue.CreateEntryBuilder();

// Add entries in any order
// First, we know we have to assemble the burger
builder
    .NewEntry("Assembly", burgerAssembler)
    .AddRequires("GrilledPatty", "ToastedBun", "Condiments", "Sauce")
    .Enqueue();

// Gotta cook the patty
builder
    .NewEntry("Grilling", griller)
    .AddRequires("Patty")
    .AddProvides("GrilledPatty")
    .Enqueue();

// Gotta toast the bun, too
builder
    .NewEntry("Toasting", toaster)
    .AddRequires("Bun")
    .AddProvides("ToastedBun")
    .Enqueue();

// We have to get the ingredients somewhere
builder
    .NewEntry("Gathering", fridgeRaider)
    .AddProvides("Patty", "Bun", "Condiments", "Sauce")
    .Enqueue();

// Validate the queue
var errors = queue.Validate();
if (errors.Any())
    throw new InvalidBurgerException(errors);

// Now build the burger
while (queue.Dequeue() is { } entry)
{
    Console.WriteLine($"Executing: {entry.Name}");

    // Execute the burger-making step
    entry.Value.Execute();

    // Tell the queue that the step is done
    queue.Complete(entry);
}
```

Output:

```
Executing: Gathering
Executing: Toasting
Executing: Grilling
Executing: Assembly
```

### Asynchronous Code and Concurrency

DependencyQueue supports `async` code and concurrent processing.  Imagine that
the `Step` class from the example above has an `ExecuteAsync()` method that
performs the step asynchronously.

```csharp
async Task ProcessAsync(DependencyQueue<Step> queue, CancellationToken cancellation)
{
    // Spin up three workers and wait for them to finish
    await Task.WhenAll(
        WorkAsync(queue, 1, cancellation),
        WorkAsync(queue, 2, cancellation),
        WorkAsync(queue, 3, cancellation)
    );
}

async Task WorkAsync(DependencyQueue<Step> queue, int n, CancellationToken cancellation)
{
    // This yield causes the worker to hop onto another thread
    // so that the caller can continue creating more workers
    await Task.Yield();

    while (await queue.DequeueAsync(cancellation) is { } entry)
    {
        Console.WriteLine($"Worker {n} executing: {entry.Name}");

        // Execute the burger-making step
        await entry.Value.ExecuteAsync(cancellation);

        // Tell the queue that the step is done
        queue.Complete(entry);
    }
}
```

Output might be:

```
Worker 1 executing: Gathering
Worker 3 executing: Grilling
Worker 2 executing: Toasting
Worker 1 executing: Assembly
```

<!--
  Copyright Subatomix Research Inc.
  SPDX-License-Identifier: MIT
-->
