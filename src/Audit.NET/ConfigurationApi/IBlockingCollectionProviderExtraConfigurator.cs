namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a way to configure extra settings for the in-memory blocking collection provider.
    /// </summary>
    public interface IBlockingCollectionProviderExtraConfigurator
    {
        /// <summary>
        /// Specifies the capacity of the internal collection.
        /// </summary>
        /// <param name="capacity">The bounded size of the collection. By default, it will use an unbounded capacity.</param>
        /// <returns></returns>
        IBlockingCollectionProviderExtraConfigurator WithCapacity(int capacity);
    }
}
