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
    [Category("AzureTables")]
    public class AzureTableDataProviderConfigurationTests
    {
        [Test]
        public void Test_AzureTablesDataProvider_FluentApi_TableClientFactory()
        {
            var provider = new AzureTableDataProvider(_ => _
                .TableClientFactory(ev => 
                    new TableClient("test", "AuditTest")));

            Assert.IsNotNull(provider.TableClientFactory);
            Assert.IsNull(provider.ConnectionString);
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
            
            Assert.IsNull(provider.TableClientFactory);
            Assert.AreEqual(cnnString, provider.ConnectionString);
            Assert.AreEqual(table, provider.TableNameBuilder.Invoke(null));
            Assert.AreEqual(66, provider.ClientOptions.Retry.MaxRetries);
            Assert.IsNotNull(entity);
            Assert.AreEqual("Part", entity.PartitionKey);
            Assert.AreEqual(eventType, entity.RowKey);
            Assert.AreEqual(123, entity.GetInt32("test"));
            Assert.AreEqual(eventType, entity.GetString("EventType"));
            Assert.AreEqual(userName, entity.GetString("UserName"));
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

            Assert.IsNull(provider.TableClientFactory);
            Assert.IsNull(provider.ConnectionString);
            Assert.AreEqual(new Uri(url), provider.ServiceEndpoint);
            Assert.AreEqual(table, provider.TableNameBuilder.Invoke(null));
            Assert.AreEqual(66, provider.ClientOptions.Retry.MaxRetries);
            Assert.IsNotNull(entity);
            Assert.AreEqual("Part", entity.PartitionKey);
            Assert.AreEqual(eventType, entity.RowKey);
            Assert.AreEqual(123, entity.GetInt32("test"));
            Assert.AreEqual(eventType, entity.GetString("EventType"));
            Assert.AreEqual(userName, entity.GetString("UserName"));
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

            Assert.IsNull(provider.TableClientFactory);
            Assert.AreEqual(cnnString, provider.ConnectionString);
            Assert.AreEqual(table, provider.TableNameBuilder.Invoke(null));
            Assert.AreEqual(66, provider.ClientOptions.Retry.MaxRetries);
            Assert.IsNotNull(entity);
            Assert.AreEqual("Part", entity.PartitionKey);
            Assert.AreEqual(eventType, entity.RowKey);
            Assert.AreEqual(eventType, (Configuration.JsonAdapter.Deserialize(entity.AuditEvent, typeof(AuditEvent)) as AuditEvent).EventType);
        }
    }
}
