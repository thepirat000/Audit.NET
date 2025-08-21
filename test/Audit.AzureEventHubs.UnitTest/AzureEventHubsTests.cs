using Audit.AzureEventHubs.Providers;
using Audit.Core;
using Audit.IntegrationTest;

using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;

using NUnit.Framework;

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable S1751
#pragma warning disable S6966

namespace Audit.AzureEventHubs.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.Azure)]
    [Category(TestCommon.Category.AzureEventHubs)]
    public class AzureEventHubsTests
    {
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private static Task _readTask;
        private static readonly ConcurrentDictionary<string, PartitionEvent> Events = new();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _readTask = ReadEvents(TokenSource.Token);
            Thread.Sleep(3000);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            TokenSource.Cancel();
            try
            {
                await _readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation occurs.
            }
            finally
            {
                TokenSource.Dispose();
            }
            _readTask.Dispose();
        }


        [SetUp]
        public void Setup()
        {
            Configuration.Reset();
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
            var client = new EventHubProducerClient(TestCommon.AzureEventHubCnnString);
            
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
        public void Test_AzureEventHubs_HappyPathSync()
        {
            var msgId = Guid.NewGuid().ToString();
            var evType = Guid.NewGuid().ToString();

            var dp = new AzureEventHubsDataProvider(cfg => cfg
                .WithConnectionString(TestCommon.AzureEventHubCnnString)
                .CustomizeEventData(eventData =>
                {
                    eventData.MessageId = msgId;
                    eventData.Properties["Test"] = "Test property";
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions()
                   {
                       EventType = evType,
                       DataProvider = dp,
                       CreationPolicy = EventCreationPolicy.Manual
                   }))
            {
                scope.Save();
            }

            Thread.Sleep(2000);

            Assert.That(Events, Does.ContainKey(msgId));

            var result = Events[msgId];

            Assert.Multiple(() =>
            {
                Assert.That(result.Data.MessageId, Is.EqualTo(msgId));
                Assert.That(result.Data.ContentType, Is.EqualTo("application/json"));
            });
            var ev = JsonSerializer.Deserialize<AuditEvent>(result.Data.BodyAsStream, Configuration.JsonSettings);
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo(evType));
        }

        [Test]
        public async Task Test_AzureEventHubs_HappyPathAsync()
        {
            var msgId = Guid.NewGuid().ToString();
            var evType = Guid.NewGuid().ToString();

            var dp = new AzureEventHubsDataProvider(cfg => cfg
                .WithConnectionString(TestCommon.AzureEventHubCnnString)
                .CustomizeEventData(eventData =>
                {
                    eventData.MessageId = msgId;
                    eventData.Properties["Test"] = "Test property";
                }));

            await using var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                EventType = evType,
                DataProvider = dp,
                CreationPolicy = EventCreationPolicy.Manual
            });

            await scope.SaveAsync();

            await Task.Delay(2000);

            Assert.That(Events, Does.ContainKey(msgId));

            var result = Events[msgId];

            Assert.Multiple(() =>
            {
                Assert.That(result.Data.MessageId, Is.EqualTo(msgId));
                Assert.That(result.Data.ContentType, Is.EqualTo("application/json"));
            });
            var ev = await JsonSerializer.DeserializeAsync<AuditEvent>(result.Data.BodyAsStream, Configuration.JsonSettings);
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo(evType));
        }

        [Test]
        public void Test_AzureEventHubs_ClientCreatedOnlyOnce()
        {
            var dp = new AzureEventHubsDataProvider(cfg => cfg.WithConnectionString(TestCommon.AzureEventHubCnnString));

            var client1 = dp.EnsureProducerClient();
            var client2 = dp.EnsureProducerClient();
            
            Assert.That(client1, Is.SameAs(client2));
        }

        private static async Task ReadEvents(CancellationToken cancellationToken)
        {
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, TestCommon.AzureEventHubCnnString);

            var readOptions = new ReadEventOptions
            {
                MaximumWaitTime = TimeSpan.FromSeconds(10)
            };

            await foreach (var partitionEvent in consumer.ReadEventsAsync(readOptions, cancellationToken))
            {
                if (partitionEvent.Data != null)
                {
                    Events[partitionEvent.Data.MessageId] = partitionEvent;
                }
            }
        }
    }
}
