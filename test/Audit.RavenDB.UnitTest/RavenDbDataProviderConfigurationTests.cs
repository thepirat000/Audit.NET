using Audit.Core;
using Audit.NET.RavenDB.Providers;
using Moq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Raven.Client.Documents;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Audit.JsonNewtonsoftAdapter;

namespace Audit.RavenDB.UnitTest
{
    [TestFixture]
    public class RavenDbDataProviderConfigurationTests
    {
        private const string ravenServerUrl = "http://localhost:8080";

        [Test]
        public void Test_RavenDbDataProvider_DocStore_FluentApi()
        {
            var testUrl = "http://test";
            var ds = new Mock<IDocumentStore>();
            ds.Setup(p => p.Urls).Returns(new[] { testUrl });

            var provider1 = new RavenDbDataProvider(_ => _
                .UseDocumentStore(ds.Object));
            var provider2 = new RavenDbDataProvider(_ => _
                .UseDocumentStore(ds.Object));

            Assert.IsNotNull(provider1.DocumentStore);
            Assert.IsNotNull(provider2.DocumentStore);
            Assert.AreEqual(testUrl, provider1.DocumentStore?.Urls[0]);
        }

        [Test]
        public void Test_RavenDbDataProvider_Settings_FluentApi()
        {
            var dbName = "test";

            var provider = new RavenDbDataProvider(_ => _
                .WithSettings(store => store
                    .DatabaseDefault("defaultDb")
                    .Certificate(null)
                    .Urls(ravenServerUrl)
                    .Database(_ => dbName)));

            Assert.IsNotNull(provider.DocumentStore);

            Assert.AreEqual(ravenServerUrl, provider.DocumentStore?.Urls[0]);
            Assert.AreEqual("defaultDb", provider.DocumentStore?.Database);
            Assert.AreEqual(dbName, provider.GetDatabaseName(null));
            Assert.AreEqual(null, provider.DocumentStore.Certificate);
        }

        [Test]
        public void Test_RavenDbDataProvider_Settings_NoSerialization_FluentApi()
        {
            var dbName = "test";

            var provider = new RavenDbDataProvider(_ => _
                .WithSettings(store => store
                    .DatabaseDefault("defaultDb")
                    .Urls(ravenServerUrl)
                    .Database(_ => dbName)));

            Assert.IsNotNull(provider.DocumentStore);

            Assert.AreEqual(ravenServerUrl, provider.DocumentStore?.Urls[0]);
            Assert.AreEqual("defaultDb", provider.DocumentStore?.Database);
            Assert.AreEqual(dbName, provider.GetDatabaseName(null));
            Assert.AreEqual(null, provider.DocumentStore.Certificate);
            Assert.IsInstanceOf<AuditContractResolver>((provider.DocumentStore.Conventions.Serialization as NewtonsoftJsonSerializationConventions)?.JsonContractResolver);
        }
    }
}