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
#if NETCOREAPP3_1 || NET6_0_OR_GREATER
                    .ClientOptions(o => o.MaxRequestsPerTcpConnection = 123)
#else
                .ConnectionPolicy(new Microsoft.Azure.Documents.Client.ConnectionPolicy() { ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp })
#endif
                .WithId(ev => "Id"));
            Assert.That(x.EndpointBuilder?.Invoke(null), Is.EqualTo("Endpoint"));
            Assert.That(x.AuthKeyBuilder?.Invoke(null), Is.EqualTo("AuthKey"));
            Assert.That(x.DatabaseBuilder?.Invoke(null), Is.EqualTo("Database"));
            Assert.That(x.ContainerBuilder?.Invoke(null), Is.EqualTo("Container"));
            Assert.That(x.IdBuilder?.Invoke(null), Is.EqualTo("Id"));
#if NETCOREAPP3_1 || NET6_0_OR_GREATER
                var opt = new Microsoft.Azure.Cosmos.CosmosClientOptions();
                x.CosmosClientOptionsAction?.Invoke(opt);
            Assert.That(opt.MaxRequestsPerTcpConnection, Is.EqualTo(123));
#else
            Assert.That(x.ConnectionPolicyBuilder.Invoke().ConnectionProtocol, Is.EqualTo(Microsoft.Azure.Documents.Client.Protocol.Tcp));
#endif
        }
    }
}
