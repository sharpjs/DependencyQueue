namespace DependencyQueue
{
    /// <summary>
    ///   An object that exposes a read-only view.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of the read-only view.
    /// </typeparam>
    public interface IHasView<T>
    {
        /// <summary>
        ///   Gets a read-only view of the object.
        /// </summary>
        /// <param name="via">
        ///   An opaque parameter for view creation.
        /// </param>
        T GetView(object via);
    }
}
