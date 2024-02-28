namespace Audit.Core.ConfigurationApi
{
    public class BlockingCollectionProviderExtraConfigurator : IBlockingCollectionProviderExtraConfigurator
    {
        internal int? _capacity;

        public IBlockingCollectionProviderExtraConfigurator WithCapacity(int capacity)
        {
            _capacity = capacity;
            return this;
        }
    }
}
