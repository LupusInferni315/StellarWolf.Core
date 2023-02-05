namespace StellarWolf
{
    /// <summary>
    /// A base for weighted objects of non-weighted types.
    /// </summary>
    public interface IWeightedObject<T> : IWeighted
    {

        /// <summary>
        /// The value of the weighted object.
        /// </summary>
        T Value { get; }

    }
}
