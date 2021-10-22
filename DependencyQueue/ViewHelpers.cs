using System.Collections;

namespace DependencyQueue
{
    internal static class ViewHelpers
    {
        public static void Reset<T>(ref T enumerator)
            where T : struct, IEnumerator
            => enumerator.Reset();
    }
}
