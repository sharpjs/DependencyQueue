// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class ExampleTests
{
    [Test]
    public void Example_Simple()
    {
        var burgerAssembler = new Step();
        var fridgeRaider    = new Step();
        var griller         = new Step();
        var toaster         = new Step();

        // Create a queue
        using var queue = new DependencyQueue<Step>();

        // Create a builder for queue items
        var builder = queue.CreateItemBuilder();

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
#if ENABLE_NOISY_TESTS
            Console.WriteLine($"Executing: {item.Name}");
#endif
            // Execute the burger-making step
            item.Value.Execute();

            // Tell the queue that the step is done
            queue.Complete(item);
        }
    }

    [Test]
    public async Task Example_AsyncParallel()
    {
        var burgerAssembler = new Step();
        var fridgeRaider    = new Step();
        var griller         = new Step();
        var toaster         = new Step();

        // Create a queue
        using var queue = new DependencyQueue<Step>();

        // Create a builder for queue items
        var builder = queue.CreateItemBuilder();

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

        // Now build the burger with parallelism
        await Task.WhenAll(
            WorkAsync(queue, 1, CancellationToken.None),
            WorkAsync(queue, 2, CancellationToken.None),
            WorkAsync(queue, 3, CancellationToken.None)
        );
    }

    async Task WorkAsync(DependencyQueue<Step> queue, int n, CancellationToken cancellation)
    {
        // This yield causes the worker to hop onto another thread
        // so that the caller can continue creating more workers
        await Task.Yield();

        while (await queue.DequeueAsync(cancellation) is { } item)
        {
#if ENABLE_NOISY_TESTS
            Console.WriteLine($"Worker {n} executing: {item.Name}");
#endif
            // Execute the burger-making step
            await item.Value.ExecuteAsync(cancellation);

            // Tell the queue that the step is done
            queue.Complete(item);
        }
    }

    private class Step
    {
        public void Execute() { }

        public Task ExecuteAsync(CancellationToken cancellation)
            => Task.Delay(5);
    }

    private class InvalidBurgerException : Exception
    {
        public InvalidBurgerException(IReadOnlyList<DependencyQueueError> errors) { }
    }
}
