using Audit.Core;
using Audit.RavenDB.Providers;
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

            Assert.That(provider1.DocumentStore, Is.Not.Null);
            Assert.That(provider2.DocumentStore, Is.Not.Null);
            Assert.That(provider1.DocumentStore?.Urls[0], Is.EqualTo(testUrl));
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

            Assert.That(provider.DocumentStore, Is.Not.Null);

            Assert.That(provider.DocumentStore?.Urls[0], Is.EqualTo(ravenServerUrl));
            Assert.That(provider.DocumentStore?.Database, Is.EqualTo("defaultDb"));
            Assert.That(provider.GetDatabaseName(null), Is.EqualTo(dbName));
            Assert.That(provider.DocumentStore.Certificate, Is.EqualTo(null));
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

            Assert.That(provider.DocumentStore, Is.Not.Null);

            Assert.That(provider.DocumentStore?.Urls[0], Is.EqualTo(ravenServerUrl));
            Assert.That(provider.DocumentStore?.Database, Is.EqualTo("defaultDb"));
            Assert.That(provider.GetDatabaseName(null), Is.EqualTo(dbName));
            Assert.That(provider.DocumentStore.Certificate, Is.EqualTo(null));
            Assert.IsInstanceOf<AuditContractResolver>((provider.DocumentStore.Conventions.Serialization as NewtonsoftJsonSerializationConventions)?.JsonContractResolver);
        }
    }
}