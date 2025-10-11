# DependencyQueue

A dependency queue for .NET: a thread-safe generic queue that dequeues elements
in dependency order.

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
If one must have a catchy acronym for such a queue, a possibility is WIRDO —
**w**hatever **i**n, **r**everse **d**ependency **o**ut.

If an example would be helpful, skip to the [Examples](#examples) section.

### Creation

The queue class is the generic `DependencyQueue<T>`, which supports simple
creation via its constructor.

```csharp
using var queue = new DependencyQueue<Step>();
```

Because the queue class implements `IDisposable`, make sure to guarantee
eventual disposal of queue instances via a `using` block or other means.

### Enqueueing Items

A `DependencyQueue<T>` instance accepts element values of type `T`.  Each
element value is wrapped in a `DependencyQueueItem<T>` object — an *item* —
which also tracks how that item relates dependency-wise to the other items in
the queue.  DependencyQueue provides two ways to create and enqueue (add)
items: an `Enqueue()` method and a builder pattern.

#### Enqueueing Items With a Builder

To obtain a builder object, call `CreateBuilder()`.

```csharp
var builder = queue.CreateBuilder();
```

The builder uses a fluent interface to build and enqueue an item incrementally.
Each method returns the builder itself, enabling the developer to chain
multiple method calls together.

To begin a new item, call `NewItem()` on the builder, passing both a name for
the item and a value to store in the item.  To add the item to the queue, call
`Enqueue()` on the builder.  The builder then becomes reusable for another
item.

```csharp
builder
    .NewItem("MyItem", theItem)
    .Enqueue();
```

To indicate that an item requires some other item to be dequeued first, call
`AddRequires()` on the builder, passing one or more names on which the current
item will depend.

```csharp
builder
    .NewItem("MyItem", theItem)
    .AddRequires("ThingINeedA", "ThingINeedB")  // accepts multiple names
    .AddRequires("ThingINeedC")                 // can use multiple times
    .Enqueue();
```

Sometimes an item is part of some larger whole, and it is useful to give that
whole a name so that other items can depend on it.  At other times, it is
useful for an item to have more than one name.  To add more names to the
current item, call `AddProvides()` on the builder, passing one or more extra
names for the item.

```csharp
builder
    .NewItem("MyItem", theItem)
    .AddProvides("BigThingIAmPartOf", "MyAlias")  // accepts multiple names
    .AddProvides("AnotherAlias")                  // can use multiple times
    .Enqueue();
```

`CreateBuilder()` is thread-safe, but the builder object it returns is not.  To
build and enqueue items in parallel, create a separate builder for each thread.

#### Enqueueing Items With the Enqueue Method

As an alternative to the builder pattern, DependencyQueue also provides an
`Enqueue()` method that can create and enqueue an item in one call.  The
tradeoff is that all of the information about the item must be specified in
that one call.

```csharp
queue.Enqueue(
    name:     "MyItem",
    value:    theItem,
    requires: ["ThingINeedA", "ThingINeedB", "ThingINeedC"],
    provides: ["BigThingIAmPartOf", "MyAlias", "AnotherAlias"]
);
```

`Enqueue()` is thread-safe.

#### Other Details About Enqueueing

`Enqueue()` and `NewItem()` place no restriction on the element value passed as
the `value` argument.

Names passed to `Enqueue()`, `NewItem()`, `AddRequires()`, and `AddProvides()`
can be any non-null, non-empty strings.  **Duplicate names are allowed** and in
fact are often useful.

Each name defines a *topic*.  For each topic, a `DependencyQueue<T>` tracks:

- which items *provide* that topic, and
- which items *require* that topic.

Each item always provides the topic defined by the item's own name.

By default, topic names use case-sensitive ordinal comparison.  To use
different comparison rules, pass a `StringComparer` instance to the queue
constructor.

### Dequeueing Items

To dequeue (remove) an item, call `Dequeue()` or `DequeueAsync()`.  Both
methods yield the next item in the queue, or `null` if the queue is empty.

```csharp
var item = queue.Dequeue();
```
```csharp
var item = await queue.DequeueAsync(cancellation: cancellationToken);
```

The element value is available from the `Value` property of the returned item.

When the code that dequeued the item is done with the item, the code must
call `Complete()` to inform the queue.

```csharp
queue.Complete(item);
```

Once an item is completed, any other items that depended on it become available
to be dequeued if those items have no other outstanding dependencies.

The dequeue methods support an optional predicate parameter.  If the caller
provides a predicate, the queue tests the `Value` of each ready-to-dequeue item
against the predicate and yields the first item for which the predicate returns
`true`.  If the predicate does not return `true` for any ready-to-dequeue item,
then the dequeue method waits until an item becomes available that does satisfy
the predicate.

```csharp
var item = await queue.DequeueAsync(
    value => MyCustomPredicate(value),
    cancellationToken
);
```

To remove all items from a queue, call `Clear()`.

```csharp
queue.Clear();
```

`Dequeue()`, `DequeueAsync()`, `Complete()`, and `Clear()` are thread-safe.

### Validation

A `DependencyQueue<T>` can be invalid.  Specifically, the web of dependencies
between items — the *dependency graph* — can be invalid in two ways:

1. **A topic is required but not provided by any item.**
   This can happen due to a typo in a topic name or because an expected item
   was never enqueued.

2. **The dependency graph contains a cycle.**
   This occurs when an item requires itself, either directly or indirectly.

If a queue is invalid, `Dequeue()` and `DequeueAsync()` will throw an
`InvalidDependencyQueueException`.  The exception's `Errors` property lists the
reasons why the queue is invalid.

To validate a queue explicitly without dequeuing any items, call
`Validate()`, which returns a list of errors found in the queue.  if the list
is empty, then the queue is valid.  `Validate()` is thread-safe.

```csharp
var errors = queue.Validate();

foreach (var error in errors)
{
    switch (error)
    {
        case DependencyQueueUnprovidedTopicError<Step> unprovided:
            // Available properties:
            _ = unprovided.Topic;       // The topic required but not provided
            break;

        case DependencyQueueCycleError<Step> cycle:
            // Available properties:
            _ = cycle.RequiringItem;    // The item that caused the cycle
            _ = cycle.RequiredTopic;    // What it required, causing the cycle
            break;
    }
}
```

### Inspection

To peek into a queue, the `DependencyQueue<T>` class provides the `Inspect()`
and `InspectAsync()` methods.  These methods acquire an exclusive lock on the
queue and return a read-only view of the queue.  Because the view holds the exclusive lock until disposed, queue inspection is thread-safe.

```csharp
using var view = queue.Inspect();

_ = view.ReadyItems;                  // Items ready to be dequeued
_ = view.ReadyItems.First();          // First ready item
_ = view.ReadyItems.First().Provides; // Names of topics it provides
_ = view.ReadyItems.First().Requires; // Names of topics it requires
_ = view.Topics;                      // Dictionary of topics keyed by name
_ = view.Topics["Foo"];               // Topic "foo"
_ = view.Topics["Foo"].ProvidedBy;    // Items that provide topic "Foo"
_ = view.Topics["Foo"].RequiredBy;    // Items that require topic "Foo"
```

### Thread Safety

Most members of `DependencyQueue<T>` are thread-safe.  Specifically:

- The `Comparer` and `Count` properties are thread-safe.

- The `Enqueue()` method is thread-safe.

- ⚠ The `CreateBuilder()` method is thread-safe, but the object it returns is
  **not** thread-safe.  Multiple threads can each use their own builder
  instance to enqueue items in parallel.

- The dequeue methods (`Dequeue()`, `DequeueAsync()`, and `Complete()`)
  are thread-safe.

- The `Validate()` method is thread-safe.

- The `Clear()` method is thread-safe.

- The inspection methods (`Inspect()` and `InspectAsync()`) are thread-safe.
  The view objects they return also are thread-safe.

- ⚠ The `Dispose()` method is <strong>not</strong> thread-safe.

## Examples

### Basic Usage

Imagine a program that cooks a basic hamburger.  The program can add steps to a
dependency queue in any order, and the queue will yield back the steps in the
correct order to prepare a burger.

The values in the queue are `Step` objects.  For the sake of the example, it is
not important what a step is.  Just imagine that the `Step` class has an
`Execute()` method that performs the step.

```csharp
// Create a queue
using var queue = new DependencyQueue<Step>();

// Create a builder for queue items
var builder = queue.CreateBuilder();

// Add items in any order
// First, we know we have to assemble the burger
builder
    .NewItem("Assembly", burgerAssembler)
    .AddRequires("GrilledPatty", "ToastedBun", "Condiments", "Sauce")
    .Enqueue();

// Gotta cook the patty
builder
    .NewItem("Grilling", griller)
    .AddRequires("Patty")
    .AddProvides("GrilledPatty")
    .Enqueue();

// Gotta toast the bun, too
builder
    .NewItem("Toasting", toaster)
    .AddRequires("Bun")
    .AddProvides("ToastedBun")
    .Enqueue();

// We have to get the ingredients somewhere
builder
    .NewItem("Gathering", fridgeRaider)
    .AddProvides("Patty", "Bun", "Condiments", "Sauce")
    .Enqueue();

// Validate the queue
var errors = queue.Validate();
if (errors.Any())
    throw new InvalidBurgerException(errors);

// Now build the burger
while (queue.Dequeue() is { } item)
{
    Console.WriteLine($"Executing: {item.Name}");

    // Execute the burger-making step
    item.Value.Execute();

    // Tell the queue that the step is done
    queue.Complete(item);
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

    while (await queue.DequeueAsync(cancellation) is { } item)
    {
        Console.WriteLine($"Worker {n} executing: {item.Name}");

        // Execute the burger-making step
        await item.Value.ExecuteAsync(cancellation);

        // Tell the queue that the step is done
        queue.Complete(item);
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
