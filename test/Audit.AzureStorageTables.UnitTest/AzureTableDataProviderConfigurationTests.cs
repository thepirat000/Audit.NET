using Audit.AzureStorageTables.ConfigurationApi;
using Audit.AzureStorageTables.Providers;
using Audit.Core;
using Azure.Data.Tables;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Audit.AzureStorageTables.UnitTest
{
    [TestFixture]
    public class AzureTableDataProviderConfigurationTests
    {
        [Test]
        public void Test_AzureTablesDataProvider_FluentApi_TableClientFactory()
        {
            var provider = new AzureTableDataProvider(_ => _
                .TableClientFactory(ev => 
                    new TableClient("test", "AuditTest")));

            Assert.That(provider.TableClientFactory, Is.Not.Null);
            Assert.That(provider.ConnectionString, Is.Null);
        }

        [Test]
        public void Test_AzureTablesDataProvider_FluentApi_CnnString_ColumnsFromObject()
        {
            var cnnString = "test-cnn-string";
            var table = "tableTest";
            var eventType = "et";
            var userName = "test user name";
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(cnnString)
                    .TableName(table)
                    .ClientOptions(new TableClientOptions() { Retry = { MaxRetries = 66 } })
                    .EntityBuilder(b => b
                        .PartitionKey("Part")
                        .RowKey(ev => ev.EventType)
                        .Columns(c => c.FromObject(ev => new { test = 123, ev.EventType, ev.Environment.UserName }))));
            var entity = provider.TableEntityMapper?.Invoke(new Core.AuditEvent() { EventType = eventType, Environment = new Core.AuditEventEnvironment() { UserName = userName } }) as TableEntity;

            Assert.That(provider.TableClientFactory, Is.Null);
            Assert.That(provider.ConnectionString, Is.EqualTo(cnnString));
            Assert.That(provider.TableNameBuilder.Invoke(null), Is.EqualTo(table));
            Assert.That(provider.ClientOptions.Retry.MaxRetries, Is.EqualTo(66));
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(eventType));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(eventType));
            Assert.That(entity.GetString("UserName"), Is.EqualTo(userName));
        }


        [Test]
        public void Test_AzureTablesDataProvider_FluentApi_Endpoint_ColumnsFromDict()
        {
            var url = "https://test-cnn-string/";
            var table = "tableTest";
            var eventType = "et";
            var userName = "test user name";
            var provider = new AzureTableDataProvider(_ => _
                .Endpoint(new Uri(url))
                    .TableName(table)
                    .ClientOptions(new TableClientOptions() { Retry = { MaxRetries = 66 } })
                    .EntityBuilder(b => b
                        .PartitionKey("Part")
                        .RowKey(ev => ev.EventType)
                        .Columns(c => c.FromDictionary(ev => new Dictionary<string, object> { { "test", 123 }, { "EventType", ev.EventType }, { "UserName", ev.Environment.UserName } }))));
            var entity = provider.TableEntityMapper?.Invoke(new Core.AuditEvent() { EventType = eventType, Environment = new Core.AuditEventEnvironment() { UserName = userName } }) as TableEntity;

            Assert.That(provider.TableClientFactory, Is.Null);
            Assert.That(provider.ConnectionString, Is.Null);
            Assert.That(provider.ServiceEndpoint, Is.EqualTo(new Uri(url)));
            Assert.That(provider.TableNameBuilder.Invoke(null), Is.EqualTo(table));
            Assert.That(provider.ClientOptions.Retry.MaxRetries, Is.EqualTo(66));
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(eventType));
            Assert.That(entity.GetInt32("test"), Is.EqualTo(123));
            Assert.That(entity.GetString("EventType"), Is.EqualTo(eventType));
            Assert.That(entity.GetString("UserName"), Is.EqualTo(userName));
        }

        [Test]
        public void Test_AzureTablesDataProvider_FluentApi_EntityMapper()
        {
            var cnnString = "test-cnn-string";
            var table = "tableTest";
            var eventType = "et";
            var userName = "test user name";
            var provider = new AzureTableDataProvider(_ => _
                .ConnectionString(cnnString)
                    .TableName(table)
                    .ClientOptions(new TableClientOptions() { Retry = { MaxRetries = 66 } })
                    .EntityMapper(ev => new AuditEventTableEntity("Part", ev.EventType, ev)));
            var entity = provider.TableEntityMapper?.Invoke(new Core.AuditEvent() { EventType = eventType, Environment = new Core.AuditEventEnvironment() { UserName = userName } }) as AuditEventTableEntity;

            Assert.That(provider.TableClientFactory, Is.Null);
            Assert.That(provider.ConnectionString, Is.EqualTo(cnnString));
            Assert.That(provider.TableNameBuilder.Invoke(null), Is.EqualTo(table));
            Assert.That(provider.ClientOptions.Retry.MaxRetries, Is.EqualTo(66));
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(eventType));
            Assert.That((Configuration.JsonAdapter.Deserialize(entity.AuditEvent, typeof(AuditEvent)) as AuditEvent).EventType, Is.EqualTo(eventType));
        }
    }
}
