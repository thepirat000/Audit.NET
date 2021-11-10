﻿using Audit.Core;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Audit.IntegrationTest
{
    [TestFixture]
    public class AzureCosmosTests
    {
        [Test]
        [Category("AzureDocDb")]
        public void TestAzureCosmos_CustomId()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = AzureSettings.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = AzureSettings.AzureDocDbAuthKey,
                IdBuilder = _ => id
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
            
            Assert.AreEqual(id, auditEvent.CustomFields["id"].ToString());
            Assert.AreEqual(auditEvent.CustomFields["value"].ToString(), ev.CustomFields["value"].ToString());
            Assert.AreEqual(eventType, auditEvent.EventType);
        }
        
        [Test]
        [Category("AzureDocDb")]
        public async Task TestAzureCosmos_CustomIdAsync()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").ToUpper();

            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = AzureSettings.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = AzureSettings.AzureDocDbAuthKey,
                IdBuilder = _ => id
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(1000, 9999);
            var auditEvent = new AuditEvent();

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
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

            Assert.AreEqual(id, auditEvent.CustomFields["id"].ToString());
            Assert.AreEqual(auditEvent.CustomFields["value"].ToString(), ev.CustomFields["value"].ToString());
            Assert.AreEqual(eventType, auditEvent.EventType);
        }

#if NET452 || NET461 
        [Test]
        [Category("AzureDocDb")]
        public void TestAzureCosmos_Query()
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
            };

            var evs = dp.QueryEvents<AuditEventWithId>(new Microsoft.Azure.Documents.Client.FeedOptions() { EnableCrossPartitionQuery = true })
                .Where(x => x.EventType == eventType
                    && x.Environment.AssemblyName.StartsWith("Audit.IntegrationTest")
                    && x.Duration >= 0)
                .OrderByDescending(x => x.StartDate)
                .ToList();

            Assert.AreEqual(3, evs.Count);
            Assert.AreEqual(200, evs[0].CustomFields["value"]);
            Assert.IsFalse(evs[1].CustomFields.ContainsKey("value"));
            Assert.AreEqual(100, evs[2].CustomFields["value"]);
        }

        [Test]
        [Category("AzureDocDb")]
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
            };

            var evs = dp.EnumerateEvents<AuditEventWithId>($"SELECT * FROM c WHERE c.EventType LIKE '{eventType}%' ORDER BY c.StartDate DESC", new Microsoft.Azure.Documents.Client.FeedOptions() { EnableCrossPartitionQuery = true })
                .ToList();

            Assert.AreEqual(3, evs.Count);
            Assert.AreEqual(200, evs[0].CustomFields["value"]);
            Assert.IsFalse(evs[1].CustomFields.ContainsKey("value"));
            Assert.AreEqual(100, evs[2].CustomFields["value"]);
        }
#endif

    }

    public class AuditEventWithId : AuditEvent
    {
        [JsonProperty("id")]
        public string id { get; set; }
    }
}
