
namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for an in-memory blocking collection provider.
    /// </summary>
    public interface IBlockingCollectionProviderConfigurator
    {
        /// <summary>
        /// Uses a Concurrent Queue as the internal FIFO collection. 
        /// </summary>
        IBlockingCollectionProviderExtraConfigurator AsQueue();
        /// <summary>
        /// Uses a Concurrent Stack as the internal LIFO collection. 
        /// </summary>
        IBlockingCollectionProviderExtraConfigurator AsStack();
        /// <summary>
        /// Uses a Concurrent Bag as the internal collection. 
        /// </summary>
        IBlockingCollectionProviderExtraConfigurator AsBag();
    }
}
