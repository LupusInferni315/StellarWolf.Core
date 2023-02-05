namespace StellarWolf
{
    /// <summary>
    /// A base for weighted objects.
    /// </summary>
    public interface IWeighted
    {
        /// <summary>
        /// The weight of the object.
        /// </summary>
        int Weight { get; }
    }
}
