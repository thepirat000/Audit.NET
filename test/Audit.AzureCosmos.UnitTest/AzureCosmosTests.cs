using Audit.Core;
using Audit.IntegrationTest;

using Microsoft.Azure.Cosmos;

using NUnit.Framework;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.AzureCosmos.UnitTest
{

    [TestFixture]
    public class AzureCosmosTests
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var cosmosClient = new CosmosClient(AzureSettings.AzureDocDbUrl, AzureSettings.AzureDocDbAuthKey, new CosmosClientOptions()
            {
                HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }),
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true
            });
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("Audit", ThroughputProperties.CreateAutoscaleThroughput(10), null, CancellationToken.None);
            var container = await  database.Database.CreateContainerIfNotExistsAsync("AuditTest", "/EventType");
        }

        [Test]
        [Category("Integration")]
        [Category("Azure")]
        [Category("AzureCosmos")]
        public void TestAzureCosmos_CustomId()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = AzureSettings.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = AzureSettings.AzureDocDbAuthKey,
                IdBuilder = _ => id,
                CosmosClientOptionsAction = options =>
                {
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    options.ConnectionMode = ConnectionMode.Gateway;
                    options.LimitToEndpoint = true;
                }
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(1000, 9999);
            var auditEvent = new AuditEvent();
            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                AuditEvent = auditEvent
            }))
            {
                scope.SetCustomField("value", "added");
            };

            var ev = dp.GetEvent(id, eventType);

            Assert.That(auditEvent.CustomFields["id"].ToString(), Is.EqualTo(id));
            Assert.That(ev.CustomFields["value"].ToString(), Is.EqualTo(auditEvent.CustomFields["value"].ToString()));
            Assert.That(auditEvent.EventType, Is.EqualTo(eventType));
        }
        
        [Test]
        [Category("Integration")]
        [Category("Azure")]
        [Category("AzureCosmos")]
        public async Task TestAzureCosmos_CustomIdAsync()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = AzureSettings.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = AzureSettings.AzureDocDbAuthKey,
                IdBuilder = _ => id,
                CosmosClientOptionsAction = options =>
                {
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    options.ConnectionMode = ConnectionMode.Gateway;
                }
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(1000, 9999);
            var auditEvent = new AuditEvent();
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                AuditEvent = auditEvent
            }))
            {
                scope.SetCustomField("value", "added");
            };

            var ev = await dp.GetEventAsync(id, eventType);

            Assert.That(auditEvent.CustomFields["id"].ToString(), Is.EqualTo(id));
            Assert.That(ev.CustomFields["value"].ToString(), Is.EqualTo(auditEvent.CustomFields["value"].ToString()));
            Assert.That(auditEvent.EventType, Is.EqualTo(eventType));
        }
    }
}