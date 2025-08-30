using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Audit.Channels.Configuration;
using Audit.Core;
using NUnit.Framework;
using Audit.Channels.Providers;
using Audit.Core.ConfigurationApi;

namespace Audit.UnitTest
{
    [TestFixture]
    public class ChannelDataProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_ChannelDataProvider_FluentApi()
        {
            var dpBounded = new ChannelDataProvider(c => c.Bounded(10));
            var dpUnbounded = new ChannelDataProvider(c => c.Unbounded());
            var dpDefault1 = new ChannelDataProvider();
            var dpDefault2 = new ChannelDataProvider((Action<IChannelProviderConfigurator>) null);
            var dpDefault3 = new ChannelDataProvider(c => { });

            Assert.That(dpBounded.GetChannel().GetType().Name, Contains.Substring("Bounded"));
            Assert.That(dpUnbounded.GetChannel().GetType().Name, Contains.Substring("Unbounded"));
            Assert.That(dpDefault1.GetChannel().GetType().Name, Contains.Substring("Unbounded"));
            Assert.That(dpDefault2.GetChannel().GetType().Name, Contains.Substring("Unbounded"));
            Assert.That(dpDefault3.GetChannel().GetType().Name, Contains.Substring("Unbounded"));
        }

        [Test]
        public void Test_ChannelDataProvider_UseInMemoryChannelProvider_NoParams()
        {
            // Arrange & Act
            var result = Configuration.Setup().UseInMemoryChannelProvider();

            // Assert
            Assert.That(result, Is.TypeOf<CreationPolicyConfigurator>());
            Assert.That(Configuration.DataProvider, Is.TypeOf<ChannelDataProvider>());
        }

        [Test]
        public void Test_ChannelDataProvider_UseInMemoryChannelProvider_Config()
        {
            // Arrange & Act
            var result = Configuration.Setup().UseInMemoryChannelProvider(cfg => cfg.Unbounded());

            // Assert
            Assert.That(result, Is.TypeOf<CreationPolicyConfigurator>());
            Assert.That(Configuration.DataProvider, Is.TypeOf<ChannelDataProvider>());
            var channelProvider = (ChannelDataProvider)Configuration.DataProvider;
            Assert.That(channelProvider.GetChannel().GetType().Name, Contains.Substring("Unbounded"));
        }

        [Test]
        public void Test_ChannelDataProvider_UseInMemoryChannelProvider_Config_GetChannel()
        {
            // Arrange & Act
            var result = Configuration.Setup().UseInMemoryChannelProvider(cfg => cfg.Unbounded(), out var channel);

            // Assert
            Assert.That(result, Is.TypeOf<CreationPolicyConfigurator>());
            Assert.That(Configuration.DataProvider, Is.TypeOf<ChannelDataProvider>());
            Assert.That(channel.GetType().Name, Contains.Substring("Unbounded"));
        }

        [Test]
        public void Test_ChannelDataProvider_UseInMemoryChannelProvider_GetChannel()
        {
            // Arrange & Act
            var result = Configuration.Setup().UseInMemoryChannelProvider(out var channel);

            // Assert
            Assert.That(result, Is.TypeOf<CreationPolicyConfigurator>());
            Assert.That(Configuration.DataProvider, Is.TypeOf<ChannelDataProvider>());
            Assert.That(channel.GetType().Name, Contains.Substring("Unbounded"));
        }
        
        [Test]
        public void Test_ChannelDataProvider_Take()
        {
            var dataProvider = new ChannelDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            dataProvider.InsertEvent(auditEvent);

            var count1 = dataProvider.Count;
            var result1 = dataProvider.Take();
            var result2 = dataProvider.TryTakeAsync(1).GetAwaiter().GetResult();
            var count2 = dataProvider.Count;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(0));
            Assert.That(result1.EventType, Is.EqualTo(evType));
        }

        [Test]
        public async Task Test_ChannelDataProvider_TakeAsync()
        {
            var dataProvider = new ChannelDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            await dataProvider.InsertEventAsync(auditEvent);

            var count1 = dataProvider.Count;
            var result1 = await dataProvider.TakeAsync();
            var result2 = await dataProvider.TryTakeAsync(1);
            var count2 = dataProvider.Count;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(0));
            Assert.That(result1.EventType, Is.EqualTo(evType));
        }

        [Test]
        public void Test_ChannelDataProvider_TryTake()
        {
            var dataProvider = new ChannelDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            dataProvider.InsertEvent(auditEvent);
            
            var count1 = dataProvider.Count;
            var result1 = dataProvider.TryTakeAsync(1).GetAwaiter().GetResult();
            var result2 = dataProvider.TryTakeAsync(1).GetAwaiter().GetResult();
            var count2 = dataProvider.Count;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(0));
            Assert.That(result1.EventType, Is.EqualTo(evType));
        }

        [Test]
        public void Test_ChannelDataProvider_Take_CancellationToken_Cancelled_Throws()
        {
            var dataProvider = new ChannelDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            dataProvider.InsertEvent(auditEvent);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<TaskCanceledException>(() => dataProvider.Take(cts.Token));
            Assert.That(dataProvider.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_ChannelDataProvider_TryTake_Timeout()
        {
            var dataProvider = new ChannelDataProvider();
            using var cts = new CancellationTokenSource(5000);
            var auditEvent = await dataProvider.TryTakeAsync(10, cts.Token);

            Assert.That(auditEvent, Is.Null);
        }

        [Test]
        public async Task Test_ChannelDataProvider_Take_Blocks()
        {
            var dataProvider = new ChannelDataProvider();

            var tasks = new Task[]
            {
                Task.Run(() => dataProvider.Take()),
                Task.Delay(100)
            };

            await Task.WhenAny(tasks);

            Assert.That(tasks[0].IsCompleted, Is.False);
            Assert.That(tasks[1].IsCompleted, Is.True);
        }

        [Test]
        public void Test_ChannelDataProvider_TryTake_Cancels()
        {
            var dataProvider = new ChannelDataProvider();
            using var cts = new CancellationTokenSource(100);
            
            Assert.ThrowsAsync<OperationCanceledException>(async () => await dataProvider.TryTakeAsync(30000, cts.Token));
        }
        
        [Test]
        public void Test_ChannelDataProvider_ReplaceEvent_Throws_NotImplementedException()
        {
            var dataProvider = new ChannelDataProvider();
            Assert.Throws<NotImplementedException>(() => dataProvider.ReplaceEvent(Guid.NewGuid(), new Audit.Core.AuditEvent()));
        }

        [Test]
        public void Test_ChannelDataProvider_ReplaceEventAsync_Throws_NotImplementedException()
        {
            var dataProvider = new ChannelDataProvider();
            Assert.ThrowsAsync<NotImplementedException>(() => dataProvider.ReplaceEventAsync(Guid.NewGuid(), new Audit.Core.AuditEvent()));
        }

        [Test]
        public void Test_ChannelDataProvider_GetEvent_Throws_NotImplementedException()
        {
            var dataProvider = new ChannelDataProvider();
            Assert.Throws<NotImplementedException>(() => dataProvider.GetEvent(Guid.NewGuid()));
        }

        [Test]
        public void Test_ChannelDataProvider_GetEventAsync_Throws_NotImplementedException()
        {
            var dataProvider = new ChannelDataProvider();
            Assert.ThrowsAsync<NotImplementedException>(() => dataProvider.GetEventAsync(Guid.NewGuid()));
        }

        [Test]
        public void Test_ChannelDataProvider_GetChannel()
        {
            var dataProvider = new ChannelDataProvider();

            var guid = Guid.NewGuid().ToString();

            var channel = dataProvider.GetChannel();

            for (int i = 0; i < 10; i++)
            {
                dataProvider.InsertEvent(new AuditEvent() { EventType = guid, CustomFields = new Dictionary<string, object>() { { "Index", i } } });
            }
            
            var count = channel.Reader.Count;

            Assert.That(count, Is.EqualTo(10));
        }

        [Test]
        public async Task Test_ChannelDataProvider_WithCapacity()
        {
            var dataProvider = new ChannelDataProvider(Channel.CreateBounded<AuditEvent>(5));

            var totalProduced = 0;

            var producerTask = Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    // should block after the fifth
                    var auditEvent = new AuditEvent() { EventType = i.ToString() };
                    dataProvider.InsertEvent(auditEvent);
                    totalProduced++;
                }
            });

            var produceCompleted = producerTask.Wait(500);
            
            Assert.That(produceCompleted, Is.False);
            Assert.That(totalProduced, Is.EqualTo(5));
            Assert.That(dataProvider.Count, Is.EqualTo(5));

            for (int i = 0; i < 10; i++)
            {
                var auditEvent = await dataProvider.TakeAsync();
                Assert.That(auditEvent.EventType, Is.EqualTo(i.ToString()));
            }

            Assert.That(totalProduced, Is.EqualTo(10));
            Assert.That(dataProvider.Count, Is.EqualTo(0));
        }
    }
}
