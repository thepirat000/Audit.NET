using Audit.AzureStorageTables.ConfigurationApi;
using Audit.AzureStorageTables.Providers;
using Audit.Core;
using Azure.Data.Tables;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Audit.AzureStorageTables.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("Azure")]
    public class AzureTableDataProviderIntegrationTests
    {
        #region Sync
        [Test]
        public void Test_AzureTablesDataProvider_EntityBuilder_Integration()
        {
            var id = Guid.NewGuid().ToString();
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                .TableName("AuditTest")
                .EntityBuilder(b => b
                    .PartitionKey("Part")
                    .RowKey(ev => ev.EventType)
                    .Columns(c => c.FromObject(ev => new { test = 123, ev.EventType, ev.Environment.UserName }))));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };
            var entityId = provider.InsertEvent(auditEvent) as string[];

            auditEvent.Environment.UserName = "test updated user name";

            provider.ReplaceEvent(entityId, auditEvent);

            var entity = provider.GetTableClient(auditEvent).GetEntity<TableEntity>(entityId[0], entityId[1]).Value;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(id));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(id));
            Assert.That(entity.GetString("UserName"), Is.EqualTo("test updated user name"));
        }

        [Test]
        public void Test_AzureTablesDataProvider_EntityMapper_Integration()
        {
            var id = Guid.NewGuid().ToString();
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                .TableName("AuditTest")
                .EntityMapper(ev => new TableEntity("Part", ev.EventType)
                {
                    { "test", 123 },
                    { "EventType", ev.EventType },
                    { "UserName", ev.Environment.UserName }
                }));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };
            var entityId = provider.InsertEvent(auditEvent) as string[];

            auditEvent.Environment.UserName = "test updated user name";

            provider.ReplaceEvent(entityId, auditEvent);

            var entity = provider.GetTableClient(auditEvent).GetEntity<TableEntity>(entityId[0], entityId[1]).Value;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(id));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(id));
            Assert.That(entity.GetString("UserName"), Is.EqualTo("test updated user name"));
        }

        [Test]
        public void Test_AzureTablesDataProvider_DefaultMapper_Integration()
        {
            var id = Guid.NewGuid().ToString();
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                .TableName("AuditTest"));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };
            var entityId = provider.InsertEvent(auditEvent) as string[];

            auditEvent.Environment.UserName = "test updated user name";

            provider.ReplaceEvent(entityId, auditEvent);

            var entity = provider.GetTableClient(auditEvent).GetEntity<AuditEventTableEntity>(entityId[0], entityId[1]).Value;
            var auditEventLoaded = Configuration.JsonAdapter.Deserialize(entity.AuditEvent, typeof(AuditEvent)) as AuditEvent;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo(nameof(AuditEvent)));
            Assert.That(entity.RowKey, Is.Not.Null);
            Assert.That(auditEventLoaded.EventType, Is.EqualTo(id));
            Assert.That(auditEventLoaded.Environment.UserName, Is.EqualTo("test updated user name"));
        }

        [Test]
        public void Test_AzureTablesDataProvider_UseAzureTable_Integration()
        {
            var id = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseAzureTableStorage(cfg => cfg
                    .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                    .TableName("AuditTest")
                    .EntityBuilder(b => b
                        .PartitionKey("Part")
                        .RowKey(ev => ev.EventType)
                        .Columns(c => c.FromObject(ev => new { test = 123, ev.EventType, ev.Environment.UserName }))));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                AuditEvent = auditEvent,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd
            }))
            {
                scope.Event.Environment.UserName = "test updated user name";
            }

            var provider = Configuration.DataProviderAs<AzureTableDataProvider>();
            var entity = provider.GetTableClient(auditEvent).GetEntity<TableEntity>("Part", id).Value;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(id));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(id));
            Assert.That(entity.GetString("UserName"), Is.EqualTo("test updated user name"));
        }
        #endregion

        #region Async
        [Test]
        public async Task Test_AzureTablesDataProvider_EntityBuilder_IntegrationAsync()
        {
            var id = Guid.NewGuid().ToString();
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                .TableName("AuditTest")
                .EntityBuilder(b => b
                    .PartitionKey("Part")
                    .RowKey(ev => ev.EventType)
                    .Columns(c => c.FromObject(ev => new { test = 123, ev.EventType, ev.Environment.UserName }))));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };
            var entityId = await provider.InsertEventAsync(auditEvent) as string[];

            auditEvent.Environment.UserName = "test updated user name";

            await provider.ReplaceEventAsync(entityId, auditEvent);

            var entity = (await (await provider.GetTableClientAsync(auditEvent)).GetEntityAsync<TableEntity>(entityId[0], entityId[1])).Value;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(id));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(id));
            Assert.That(entity.GetString("UserName"), Is.EqualTo("test updated user name"));
        }

        [Test]
        public async Task Test_AzureTablesDataProvider_EntityMapper_IntegrationAsync()
        {
            var id = Guid.NewGuid().ToString();
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                .TableName("AuditTest")
                .EntityMapper(ev => new TableEntity("Part", ev.EventType)
                {
                    { "test", 123 },
                    { "EventType", ev.EventType },
                    { "UserName", ev.Environment.UserName }
                }));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };
            var entityId = await provider.InsertEventAsync(auditEvent) as string[];

            auditEvent.Environment.UserName = "test updated user name";

            await provider.ReplaceEventAsync(entityId, auditEvent);

            var entity = (await (await provider.GetTableClientAsync(auditEvent)).GetEntityAsync<TableEntity>(entityId[0], entityId[1])).Value;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(id));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(id));
            Assert.That(entity.GetString("UserName"), Is.EqualTo("test updated user name"));
        }

        [Test]
        public async Task Test_AzureTablesDataProvider_DefaultMapper_IntegrationAsync()
        {
            var id = Guid.NewGuid().ToString();
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                .TableName("AuditTest"));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };
            var entityId = await provider.InsertEventAsync(auditEvent) as string[];

            auditEvent.Environment.UserName = "test updated user name";

            await provider.ReplaceEventAsync(entityId, auditEvent);
            
            var entity = (await (await provider.GetTableClientAsync(auditEvent)).GetEntityAsync<AuditEventTableEntity>(entityId[0], entityId[1])).Value;
            var auditEventLoaded = Configuration.JsonAdapter.Deserialize(entity.AuditEvent, typeof(AuditEvent)) as AuditEvent;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo(nameof(AuditEvent)));
            Assert.That(entity.RowKey, Is.Not.Null);
            Assert.That(auditEventLoaded.EventType, Is.EqualTo(id));
            Assert.That(auditEventLoaded.Environment.UserName, Is.EqualTo("test updated user name"));
        }

        [Test]
        public async Task Test_AzureTablesDataProvider_UseAzureTable_IntegrationAsync()
        {
            var id = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseAzureTableStorage(cfg => cfg
                    .ConnectionString(IntegrationTest.AzureSettings.AzureTableCnnString)
                    .TableName("AuditTest")
                    .EntityBuilder(b => b
                        .PartitionKey("Part")
                        .RowKey(ev => ev.EventType)
                        .Columns(c => c.FromObject(ev => new { test = 123, ev.EventType, ev.Environment.UserName }))));
            var auditEvent = new Core.AuditEvent()
            {
                EventType = id,
                Environment = new Core.AuditEventEnvironment()
                {
                    UserName = "test user name"
                }
            };

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                AuditEvent = auditEvent,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd
            }))
            {
                scope.Event.Environment.UserName = "test updated user name";
            }

            var provider = Configuration.DataProviderAs<AzureTableDataProvider>();
            var entity = (await (await provider.GetTableClientAsync(auditEvent)).GetEntityAsync<TableEntity>("Part", id)).Value;

            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(id));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(id));
            Assert.That(entity.GetString("UserName"), Is.EqualTo("test updated user name"));
        }
        #endregion
    }
}
