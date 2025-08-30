using Audit.Core;
using Audit.Core.ConfigurationApi;
using Audit.Core.Providers;

using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class ConfiguratorTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void JsonAdapter()
        {
            var cfg = new Configurator();
            var adapter = new JsonAdapter();

            cfg.JsonAdapter(adapter);

            Assert.That(Configuration.JsonAdapter, Is.SameAs(adapter));
        }

        [Test]
        public void JsonAdapterGeneric()
        {
            var cfg = new Configurator();

            cfg.JsonAdapter<JsonAdapter>();

            Assert.That(Configuration.JsonAdapter, Is.TypeOf<JsonAdapter>());
        }

        [Test]
        public void UseInMemoryBlockingCollectionProvider()
        {
            var cfg = new Configurator();
            
            cfg.UseInMemoryBlockingCollectionProvider();
            
            Assert.That(Configuration.DataProvider, Is.TypeOf<BlockingCollectionDataProvider>());
        }

        [Test]
        public void UseInMemoryBlockingCollectionProvider_Config()
        {
            var cfg = new Configurator();

            cfg.UseInMemoryBlockingCollectionProvider(x => x.AsBag());

            Assert.That(Configuration.DataProvider, Is.TypeOf<BlockingCollectionDataProvider>());
        }

        [Test]
        public void UseInMemoryBlockingCollectionProvider_OutParam()
        {
            var cfg = new Configurator();

            cfg.UseInMemoryBlockingCollectionProvider(out var bc);

            Assert.That(Configuration.DataProvider, Is.TypeOf<BlockingCollectionDataProvider>());
            Assert.That(bc, Is.Not.Null);
        }

        [Test]
        public void UseInMemoryBlockingCollectionProvider_Config_OutParam()
        {
            var cfg = new Configurator();

            cfg.UseInMemoryBlockingCollectionProvider(x => x.AsBag(), out var bc);

            Assert.That(Configuration.DataProvider, Is.TypeOf<BlockingCollectionDataProvider>());
            Assert.That(bc, Is.Not.Null);
        }
    }
}
