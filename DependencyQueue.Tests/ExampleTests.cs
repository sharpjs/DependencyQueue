// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

[TestFixture]
public class ExampleTests
{
    [Test]
    public void TestExample()
    {
        var burgerAssembler = new Step();
        var fridgeRaider    = new Step();
        var griller         = new Step();
        var toaster         = new Step();

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
            // Commented out to reduce test output noise
            //Console.WriteLine("Executing: " + entry.Name);
            entry.Value.Execute();
            queue.Complete(entry);
        }
    }

    private class Step
    {
        public void Execute() { }
    }

    private class InvalidBurgerException : Exception
    {
        public InvalidBurgerException(IReadOnlyList<DependencyQueueError> errors) { }
    }
}
