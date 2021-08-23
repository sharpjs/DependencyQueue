using System;

namespace DependencyQueue
{
    internal static class Errors
    {
        internal static Exception ArgumentNull(string name)
            => new ArgumentNullException(name);

        internal static Exception ArgumentEmpty(string name)
            => new ArgumentException("Argument cannot be empty.", name);

        internal static Exception ArgumentContainsNull(string name)
            => new ArgumentException("Argument cannot contain a null item.", name);

        internal static Exception ArgumentContainsEmpty(string name)
            => new ArgumentException("Argument cannot contain an mepty item.", name);

        internal static Exception NoCurrentEntry()
            => new InvalidOperationException(
                "The builder does not have a current entry.  " +
                "Use the NewEntry method to begin building an entry."
            );
    }
}
