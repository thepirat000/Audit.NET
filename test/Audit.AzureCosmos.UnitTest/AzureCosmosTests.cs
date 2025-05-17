using Audit.Core;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using Audit.IntegrationTest;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;

namespace Audit.AzureCosmos.UnitTest
{
    [TestFixture]
    public class AzureCosmosTests
    {
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
#if IS_COSMOS
                CosmosClientOptionsAction = options =>
                {
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    options.ConnectionMode = ConnectionMode.Gateway;
                }
#endif
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
#if IS_COSMOS
                CosmosClientOptionsAction = options =>
                {
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    options.ConnectionMode = ConnectionMode.Gateway;
                }
#endif
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

#if IS_DOCDB
        [Test]
        [Category("Integration")]
        [Category("Azure")]
        [Category("AzureCosmos")]
        public void TestAzureCosmos_Query()
        {
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = AzureSettings.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = AzureSettings.AzureDocDbAuthKey
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(10000, 99999);

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            }))
            {
                scope.SetCustomField("value", 100);
            };
            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd
            }))
            {
                scope.SetCustomField("value", 200);
            };

            var evs = dp.QueryEvents<AuditEventWithId>(new Microsoft.Azure.Documents.Client.FeedOptions() { EnableCrossPartitionQuery = true, JsonSerializerSettings = new JsonSerializerSettings() })
                .Where(x => x.EventType == eventType
                    && x.Duration >= 0)
                .OrderByDescending(x => x.StartDate)
                .ToList();

            Assert.That(evs.Count, Is.EqualTo(3));
            Assert.That(evs[0].CustomFields["value"], Is.EqualTo(200));
            Assert.IsFalse(evs[1].CustomFields.ContainsKey("value"));
            Assert.That(evs[2].CustomFields["value"], Is.EqualTo(100));
        }

        [Test]
        [Category("Integration")]
        [Category("Azure")]
        [Category("AzureCosmos")]
        public void TestAzureCosmos_Enumerate()
        {
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = AzureSettings.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = AzureSettings.AzureDocDbAuthKey
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(1000, 9999);

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnEnd
            }))
            {
                scope.SetCustomField("value", 100);
            };
            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd
            }))
            {
                scope.SetCustomField("value", 200);
            }

            var evs = dp.EnumerateEvents<AuditEventWithId>($"SELECT * FROM c WHERE c.EventType = '{eventType}' ORDER BY c.StartDate DESC", new Microsoft.Azure.Documents.Client.FeedOptions() { EnableCrossPartitionQuery = true, JsonSerializerSettings = new JsonSerializerSettings() })
                .ToList();

            Assert.That(evs.Count, Is.EqualTo(3));
            Assert.That(evs[0].CustomFields["value"], Is.EqualTo(200));
            Assert.IsFalse(evs[1].CustomFields.ContainsKey("value"));
            Assert.That(evs[2].CustomFields["value"], Is.EqualTo(100));
        }
#endif

    }

    public class AuditEventWithId : AuditEvent
    {
        [JsonProperty("id")]
        public string id { get; set; }
    }
}
