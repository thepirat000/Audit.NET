namespace Audit.Core.ConfigurationApi
{
    public class BlockingCollectionProviderConfigurator : IBlockingCollectionProviderConfigurator
    {
        /// <summary>
        /// 0: Queue, 1: Stack, 2: Bag
        /// </summary>
        internal int _collectionType = 0;

        internal BlockingCollectionProviderExtraConfigurator _extra = new BlockingCollectionProviderExtraConfigurator();

        public IBlockingCollectionProviderExtraConfigurator AsQueue()
        {
            _collectionType = 0;
            _extra = new BlockingCollectionProviderExtraConfigurator();
            return _extra;
        }
        public IBlockingCollectionProviderExtraConfigurator AsStack()
        {
            _collectionType = 1;
            _extra = new BlockingCollectionProviderExtraConfigurator();
            return _extra;
        }

        public IBlockingCollectionProviderExtraConfigurator AsBag()
        {
            _collectionType = 2;
            _extra = new BlockingCollectionProviderExtraConfigurator();
            return _extra;
        }
    }
}
