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

Install [this NuGet Package](https://www.nuget.org/packages/DependencyQueue) in your project.

## Usage

Let's imagine a program that cooks a basic hamburger.  The program can add
steps to a dependency queue in any order, and the queue will yield back the
steps in the correct order to prepare a burger.

```csharp
// Create a queue
using var queue = new DependencyQueue<Step>();

// Create a builder for queue entries
var builder = queue.CreateEntryBuilder();

// Add entries in any order
builder
    .NewEntry("Assembly", burgerAssembler)
    .AddRequires("GrilledPatty", "ToastedBun", "Lettuce", "Tomato", "Onion", "Sauce")
    .Enqueue();
builder
    .NewEntry("Gathering", fridgeRaider)
    .AddProvides("Patty", "Bun", "Lettuce", "Tomato", "Onion")
    .Enqueue();
builder
    .NewEntry("Grilling", griller)
    .AddRequires("Patty")
    .AddProvides("GrilledPatty")
    .Enqueue();
builder
    .NewEntry("Toasting", toaster)
    .AddRequires("Bun")
    .AddProvides("ToastedBun")
    .Enqueue();

// Validate the queue
var errors = queue.Validate();
if (errors.Any())
    throw new InvalidBurgerException();

// Now build the burger
while (queue.TryDequeue() is Step step)
    step.Execute();
```

TODO: Expand

### Queue Runs

TODO: Describe

```csharp
await queue.RunAsync(
    async (x, d) => …,
    parallelism: 4,
    cancellationToken
);
```

<!--
  Copyright Subatomix Research Inc.
  SPDX-License-Identifier: MIT
-->
