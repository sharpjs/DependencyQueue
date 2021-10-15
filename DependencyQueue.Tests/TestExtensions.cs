namespace DependencyQueue
{
    internal static class TestExtensions
    {
        internal static QueueAssertions Should(this Queue queue)
            => new(queue);
    }
}
