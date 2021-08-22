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
    }
}
