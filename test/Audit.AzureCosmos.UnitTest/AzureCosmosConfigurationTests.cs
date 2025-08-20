using NUnit.Framework;

namespace Audit.AzureCosmos.UnitTest
{
    [TestFixture]
    public class AzureCosmosConfigurationTests
    {
        [Test]
        public void Test_AzureCosmos_FluentApi()
        {
            var x = new AzureCosmos.Providers.AzureCosmosDataProvider(_ => _
                .Endpoint(ev => "Endpoint")
                .AuthKey(ev => "AuthKey")
                .Container(ev => "Container")
                .Database(ev => "Database")
                .ClientOptions(o => o.MaxRequestsPerTcpConnection = 123)
                .WithId(ev => "Id"));
            Assert.That(x.Endpoint.GetValue(null), Is.EqualTo("Endpoint"));
            Assert.That(x.AuthKey.GetValue(null), Is.EqualTo("AuthKey"));
            Assert.That(x.Database.GetValue(null), Is.EqualTo("Database"));
            Assert.That(x.Container.GetValue(null), Is.EqualTo("Container"));
            Assert.That(x.IdBuilder?.Invoke(null), Is.EqualTo("Id"));
            var opt = new Microsoft.Azure.Cosmos.CosmosClientOptions();
            
            x.CosmosClientOptionsAction?.Invoke(opt);

            Assert.That(opt.MaxRequestsPerTcpConnection, Is.EqualTo(123));
        }
    }
}
