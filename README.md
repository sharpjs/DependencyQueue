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

If an example would be helpful, skip to the Example section below.

### Creation

The queue class is the generic `DependencyQueue<T>`, which supports simple
creation via a constructor.

```csharp
using var queue = new DependencyQueue<Step>();
```

Because the queue class implements `IDisposable`, make sure to guarantee
eventual disposal of queue instances via `using` or otherwise.

### Enqueueing Items

A `DependencyQueue<T>` instance accepts new items of type `T`.  Each item is
contained within an 'entry' structure which describes how the item relates
dependency-wise to the other items in the queue.  DependencyQueue uses a
builder pattern to create and enqueue entries.

To obtain a builder object, call `CreateEntryBuilder()`.

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
    .AddRequires("ThingINeedA", "ThingINeedB")
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
    .AddProvides("BigThingIAmPartOf", "MyAlias")
    .Enqueue();
```

The names passed to `NewEntry()`, `AddRequires()`, and `AddProvides()` accept
any names that are neither null nor empty.  **Duplicate names are allowed** and
in fact are often useful.  Each name defines a 'topic'.  Other than its name, a
topic is just a pair of lists:

- a list of which entries *provide* that topic via `NewEntry()` or `AddProvides()`, and
- a list of which entries *require* that topic via `AddRequires()`.

By default, topic names use case-sensitive ordinal comparison.  To use
different comparison rules, pass a `StringComparer` instance to the queue
constructor.

`NewEntry()` places no restriction on the item passed as its `value` argument.

### Validation

Upon construction or after enqueueing an item, a `DependencyQueue<T>` instance
is in an unvalidated state.  Before any items can be dequeued, the queue must
be validated.  Do that by calling `Validate()`.

```csharp
var errors = queue.Validate();
```

If the queue is valid, `Validate()` returns an empty list of error objects.
Otherwiws, the list describes the problems found.

The web of dependencies between entries — the 'dependency graph' — can be
invalid in two ways.

1. **A topic is required but not provided by any entry.**
   This can happen due to a typo in a topic name or because an expected entry
   was never enqueued.

2. **The dependency graph contains a cycle.**
   This occurs when an entry depends on itself, either directly or indirectly.

### Dequeueing Items

There are several ways to dequeue items from a `DependencyQueue<T>` instance.
All of them require the queue to be valid.

The simplest way is to call `TryDequeue()` or `TryDequeueAsync()`, which return
the next entry in the queue or null if the queue is empty.

```csharp
var entry = queue.TryDequeue();
```
```csharp
await var entry = queue.TryDequeueAsync(cancellation: cancellationToken);
```

The item is available in the `Value` property of the returned entry.

When the code that dequeued the entry is done with the entry, the code must
call `Complete()` to inform the queue.

```csharp
queue.Complete(entry);
```

`TryDequeue()`, `TryDequeueAsync()`, and `Complete()` are thread-safe.

Because dequeueing and completing entries is often done in a loop and in
parallel, DependencyQueue provides `Run()` and `RunAsync()` methods that
simplify that common pattern.

```csharp
queue.Run(
    context =>
    {
        while (context.GetNextEntry() is { } entry)
        {
            // Process the entry
        }
    },
    arbitrarySharedData, // made available as context.Data
    parallelism: 4
);
```

```csharp
await queue.RunAsync(
    async context =>
    {
        while (await context.GetNextEntryAsync() is { } entry)
        {
            // Process the entry asynchronously
        }
    },
    arbitrarySharedData, // made available as context.Data
    parallelism: 4,
    cancellationToken
);
```

All dequeue methods support an optional predicate parameter.  If the caller
provides a predicate, the queue tests each ready-to-dequeue item against the
predicate and yields the first entry for which the predicate returns `true`.
If the predicate does not return `true` for any ready-to-dequeue item, then
the dequeue method blocks until an item becomes available that does satisfy the
predicate.

```csharp
await var entry = queue.TryDequeueAsync(
    item => MyCustomPredicate(item),
    cancellationToken
);
```

### Inspection

To peek into a queue, the `DependencyQueue<T>` class provides the `Inspect()`
method.  The method acquires an exclusive lock on the queue and returns a
read-only view of the queue that holds the lock until the view is disposed.

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

### States

A `DependencyQueue<T>` instance has four possible states:

- **Unvalidated:**
  - The queue has not been validated or was found to be invalid.
  - Items can be enqueued.
  - Dequeue methods will throw `InvalidOperationException`.
  - Call `Validate()`  to transition to the **Valid**    state.
  - Call `SetEnding()` to transition to the **Ending**   state.
  - Call `Dispose()`   to transition to the **Disposed** state.
- **Valid:**
  - The queue was found to be valid.
  - Items can be dequeued.
  - Enqueuing a new item transitions back to the **Unvalidated** state.
  - Call `SetEnding()` to transition to the **Ending**   state.
  - Call `Dispose()`   to transition to the **Disposed** state.
- **Ending:**
  - Queue processing is ending early.
  - Items can be enqueued, but will be ignored.
  - Dequeue methods will return `null` immediately.
  - Call `Dispose()`   to transition to the **Disposed** state.
- **Disposed:**
  - The queue has been disposed and is no longer unsable.
  - Most methods will throw `ObjectDisposedException`.

### Thread Safety

- The object returned by `CreateEntryBuilder()` is not thread-safe, but
  multiple threads can each use their own builder instance to enqueue items in
  parallel.

- `Validate()` is thread-safe.

- All dequeue methods (`TryDequeue()`, `TryDequeueAsync()`, `Run()`,
  `RunAsync()`, and `Complete()`) are thread-safe.

- `SetEnding()` is thread-safe.

- `Dispose()` is not thread-safe.

## Example

Let's imagine a program that cooks a basic hamburger.  The program can add
steps to a dependency queue in any order, and the queue will yield back the
steps in the correct order to prepare a burger.

The things in the queue are `Step` objects.  For the sake of the example, it's
not important what that is.  Just imagine it has an `Execute()` method that
does the thing.

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

// And we have to get the ingredients somewhere
builder
    .NewEntry("Gathering", fridgeRaider)
    .AddProvides("Patty", "Bun", "Condiments", "Sauce")
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

// Validate the queue
var errors = queue.Validate();
if (errors.Any())
    throw new InvalidBurgerException(errors);

// Now build the burger
while (queue.TryDequeue() is { } entry)
{
    Console.WriteLine("Executing: " + entry.Name);
    entry.Value.Execute();
    queue.Complete(entry);
}
```

Output might be:

```
Executing: Gathering
Executing: Toasting
Executing: Grilling
Executing: Assembly
```

<!--
  Copyright Subatomix Research Inc.
  SPDX-License-Identifier: MIT
-->
