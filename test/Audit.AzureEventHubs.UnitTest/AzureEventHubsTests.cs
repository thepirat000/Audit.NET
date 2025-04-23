using System;
using System.Text.Json;
using System.Threading.Tasks;
using Audit.AzureEventHubs.Providers;
using Audit.Core;
using Audit.IntegrationTest;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using NUnit.Framework;
#pragma warning disable S1751
#pragma warning disable S6966

namespace Audit.AzureEventHubs.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("Azure")]
    public class AzureEventHubsTests
    {
        public void Pepe(IServiceProvider serviceProvider)
        {
            var Settings = new { ConnectionString = "", HubName = ""};



        }

        [Test]
        public void Test_AzureEventHubs_Configuration_ConnectionString()
        {
            var cnnString = "TestCnnString";
            var hub = "hub";

            var dp = new AzureEventHubsDataProvider(cfg => cfg
                .WithConnectionString(cnnString, hub)
                .CustomizeEventData(eventData =>
                {
                    eventData.MessageId = "123";
                    eventData.Properties["Test"] = "Test1";
                }));

            Assert.That(dp.ConnectionString, Is.EqualTo(cnnString));
            Assert.That(dp.HubName, Is.EqualTo(hub));
            Assert.That(dp.CustomizeEventData, Is.Not.Null);
            Assert.That(dp.ProducerClientFactory, Is.Null);
        }

        [Test]
        public async Task Test_AzureEventHubs_Configuration_Client()
        {
            var client = new EventHubProducerClient(AzureSettings.AzureEventHubCnnString);
            
            var dp = new AzureEventHubsDataProvider(cfg => cfg
                .WithClient(client)
                .CustomizeEventData(eventData =>
                {
                    eventData.MessageId = "123";
                    eventData.Properties["Test"] = "Test1";
                }));

            await client.DisposeAsync();

            Assert.That(dp.ConnectionString, Is.Null);
            Assert.That(dp.HubName, Is.Null);
            Assert.That(dp.CustomizeEventData, Is.Not.Null);
            Assert.That(dp.ProducerClientFactory, Is.Not.Null);
        }

        [Test]
        public async Task Test_AzureEventHubs_HappyPath()
        {
            var msgId = Guid.NewGuid().ToString();
            var evType = Guid.NewGuid().ToString();

            var readTask = ReadOneEvent();

            var dp = new AzureEventHubsDataProvider(cfg => cfg
                .WithConnectionString(AzureSettings.AzureEventHubCnnString)
                .CustomizeEventData(eventData =>
                {
                    eventData.MessageId = msgId;
                    eventData.Properties["Test"] = "Test property";
                }));

            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = evType,
                DataProvider = dp,
                CreationPolicy = EventCreationPolicy.Manual
            });

            scope.Save();

            var result = await readTask;

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.Data.MessageId, Is.EqualTo(msgId));
                Assert.That(result.Value.Data.ContentType, Is.EqualTo("application/json"));
            });
            var ev = await JsonSerializer.DeserializeAsync<AuditEvent>(result.Value.Data.BodyAsStream, Configuration.JsonSettings);
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo(evType));
        }

        [Test]
        public async Task Test_AzureEventHubs_HappyPathAsync()
        {
            var msgId = Guid.NewGuid().ToString();
            var evType = Guid.NewGuid().ToString();

            var readTask = ReadOneEvent();

            var dp = new AzureEventHubsDataProvider(cfg => cfg
                .WithConnectionString(AzureSettings.AzureEventHubCnnString)
                .CustomizeEventData(eventData =>
                {
                    eventData.MessageId = msgId;
                    eventData.Properties["Test"] = "Test property";
                }));

            var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                EventType = evType,
                DataProvider = dp,
                CreationPolicy = EventCreationPolicy.Manual
            });

            await scope.SaveAsync();

            var result = await readTask;

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Value.Data.MessageId, Is.EqualTo(msgId));
                Assert.That(result.Value.Data.ContentType, Is.EqualTo("application/json"));
            });
            var ev = await JsonSerializer.DeserializeAsync<AuditEvent>(result.Value.Data.BodyAsStream, Configuration.JsonSettings);
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo(evType));
        }

        [Test]
        public void Test_AzureEventHubs_ClientCreatedOnlyOnce()
        {
            var dp = new AzureEventHubsDataProvider(cfg => cfg.WithConnectionString(AzureSettings.AzureEventHubCnnString));

            var client1 = dp.EnsureProducerClient();
            var client2 = dp.EnsureProducerClient();
            
            Assert.That(client1, Is.SameAs(client2));
        }

        private static async Task<PartitionEvent?> ReadOneEvent()
        {
            var consumer = new EventHubConsumerClient("$Default", AzureSettings.AzureEventHubCnnString);

            await foreach (var partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.Latest))
            {
                return partitionEvent;
            }

            return null;
        }
    }
}
