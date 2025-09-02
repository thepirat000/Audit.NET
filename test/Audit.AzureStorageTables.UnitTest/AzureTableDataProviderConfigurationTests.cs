using Audit.AzureStorageTables.ConfigurationApi;
using Audit.AzureStorageTables.Providers;
using Audit.Core;

using Azure;

using Azure.Core;
using Azure.Data.Tables;

using Moq;

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
            Assert.That(provider.TableName.GetDefault(), Is.EqualTo(table));
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
            Assert.That(provider.TableName.GetDefault(), Is.EqualTo(table));
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
            Assert.That(provider.TableName.GetDefault(), Is.EqualTo(table));
            Assert.That(provider.ClientOptions.Retry.MaxRetries, Is.EqualTo(66));
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity.PartitionKey, Is.EqualTo("Part"));
            Assert.That(entity.RowKey, Is.EqualTo(eventType));
            Assert.That((Configuration.JsonAdapter.Deserialize(entity.AuditEvent, typeof(AuditEvent)) as AuditEvent).EventType, Is.EqualTo(eventType));
        }

        [Test]
        public void ConnectionString_SetsConnectionStringAndReturnsTableConfig()
        {
            var configurator = new AzureTableConnectionConfigurator();
            var tableConfig = configurator.ConnectionString("test-connection-string");

            Assert.That(configurator._connectionString, Is.EqualTo("test-connection-string"));
            Assert.That(tableConfig, Is.EqualTo(configurator._tableConfig));
        }

        [Test]
        public void Endpoint_SetsEndpointAndReturnsTableConfig()
        {
            var configurator = new AzureTableConnectionConfigurator();
            var uri = new Uri("https://test.table.core.windows.net");
            var tableConfig = configurator.Endpoint(uri);

            Assert.That(configurator._endpointUri, Is.EqualTo(uri));
            Assert.That(tableConfig, Is.EqualTo(configurator._tableConfig));
        }

        [Test]
        public void Endpoint_WithTableSharedKeyCredential_SetsEndpointAndCredential()
        {
            var configurator = new AzureTableConnectionConfigurator();
            var uri = new Uri("https://test.table.core.windows.net");
            var credential = new TableSharedKeyCredential(Convert.ToBase64String("account"u8.ToArray()), Convert.ToBase64String("key"u8.ToArray()));
            var tableConfig = configurator.Endpoint(uri, credential);

            Assert.That(configurator._endpointUri, Is.EqualTo(uri));
            Assert.That(configurator._sharedKeyCredential, Is.EqualTo(credential));
            Assert.That(tableConfig, Is.EqualTo(configurator._tableConfig));
        }

        [Test]
        public void Endpoint_WithAzureSasCredential_SetsEndpointAndCredential()
        {
            var configurator = new AzureTableConnectionConfigurator();
            var uri = new Uri("https://test.table.core.windows.net");
            var credential = new AzureSasCredential("sas-token");
            var tableConfig = configurator.Endpoint(uri, credential);

            Assert.That(configurator._endpointUri, Is.EqualTo(uri));
            Assert.That(configurator._sasCredential, Is.EqualTo(credential));
            Assert.That(tableConfig, Is.EqualTo(configurator._tableConfig));
        }

        [Test]
        public void Endpoint_WithTokenCredential_SetsEndpointAndCredential()
        {
            var configurator = new AzureTableConnectionConfigurator();
            var uri = new Uri("https://test.table.core.windows.net");
            var credential = new Mock<TokenCredential>().Object;
            var tableConfig = configurator.Endpoint(uri, credential);

            Assert.That(configurator._endpointUri, Is.EqualTo(uri));
            Assert.That(configurator._tokenCredential, Is.EqualTo(credential));
            Assert.That(tableConfig, Is.EqualTo(configurator._tableConfig));
        }

        [Test]
        public void TableClientFactory_SetsClientFactoryAndReturnsTableConfig()
        {
            var configurator = new AzureTableConnectionConfigurator();
            Func<AuditEvent, TableClient> factory = (e) => new Mock<TableClient>().Object;
            var tableConfig = configurator.TableClientFactory(factory);

            Assert.That(configurator._clientFactory, Is.EqualTo(factory));
            Assert.That(tableConfig, Is.EqualTo(configurator._tableConfig));
        }
    }
}
