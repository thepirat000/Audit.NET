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
            Assert.AreEqual("Endpoint", x.EndpointBuilder?.Invoke(null));
            Assert.AreEqual("AuthKey", x.AuthKeyBuilder?.Invoke(null));
            Assert.AreEqual("Database", x.DatabaseBuilder?.Invoke(null));
            Assert.AreEqual("Container", x.ContainerBuilder?.Invoke(null));
            Assert.AreEqual("Id", x.IdBuilder?.Invoke(null));
#if NETCOREAPP3_1 || NET6_0_OR_GREATER
                var opt = new Microsoft.Azure.Cosmos.CosmosClientOptions();
                x.CosmosClientOptionsAction?.Invoke(opt);
                Assert.AreEqual(123, opt.MaxRequestsPerTcpConnection);
#else
            Assert.AreEqual(Microsoft.Azure.Documents.Client.Protocol.Tcp, x.ConnectionPolicyBuilder.Invoke().ConnectionProtocol);
#endif
        }
    }
}
