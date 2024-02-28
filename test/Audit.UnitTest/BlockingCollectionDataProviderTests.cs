using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class BlockingCollectionDataProviderTests
    {
        [Test]
        public void Test_BlockingCollectionDataProvider_Take()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            dataProvider.InsertEvent(auditEvent);

            var count1 = dataProvider.Count;
            var result1 = dataProvider.Take();
            var result2 = dataProvider.TryTake(0);
            var count2 = dataProvider.Count;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(0));
            Assert.That(result1.EventType, Is.EqualTo(evType));
        }

        [Test]
        public async Task Test_BlockingCollectionDataProvider_TakeAsync()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            await dataProvider.InsertEventAsync(auditEvent);

            var count1 = dataProvider.Count;
            var result1 = dataProvider.Take();
            var result2 = dataProvider.TryTake(0);
            var count2 = dataProvider.Count;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(0));
            Assert.That(result1.EventType, Is.EqualTo(evType));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_TryTake()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            dataProvider.InsertEvent(auditEvent);
            
            var count1 = dataProvider.Count;
            var result1 = dataProvider.TryTake(1);
            var result2 = dataProvider.TryTake(1);
            var count2 = dataProvider.Count;

            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Null);
            Assert.That(count1, Is.EqualTo(1));
            Assert.That(count2, Is.EqualTo(0));
            Assert.That(result1.EventType, Is.EqualTo(evType));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_Take_CancellationToken_Cancelled_Throws()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            var evType = Guid.NewGuid().ToString();
            var auditEvent = new Audit.Core.AuditEvent() { EventType = evType };
            dataProvider.InsertEvent(auditEvent);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => dataProvider.Take(cts.Token));
            Assert.That(dataProvider.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_TryTake_Timeout()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();

            var auditEvent = dataProvider.TryTake(10);
            
            Assert.That(auditEvent, Is.Null);
        }

        [Test]
        public async Task Test_BlockingCollectionDataProvider_Take_Blocks()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            
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
        public void Test_BlockingCollectionDataProvider_ReplaceEvent_Throws_NotImplementedException()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            Assert.Throws<NotImplementedException>(() => dataProvider.ReplaceEvent(Guid.NewGuid(), new Audit.Core.AuditEvent()));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_ReplaceEventAsync_Throws_NotImplementedException()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            Assert.ThrowsAsync<NotImplementedException>(() => dataProvider.ReplaceEventAsync(Guid.NewGuid(), new Audit.Core.AuditEvent()));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_GetEvent_Throws_NotImplementedException()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            Assert.Throws<NotImplementedException>(() => dataProvider.GetEvent(Guid.NewGuid()));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_GetEventAsync_Throws_NotImplementedException()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();
            Assert.ThrowsAsync<NotImplementedException>(() => dataProvider.GetEventAsync(Guid.NewGuid()));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_GetAllEvents()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();

            var guid = Guid.NewGuid().ToString();

            for (int i = 0; i < 10; i++)
            {
                dataProvider.InsertEvent(new AuditEvent() { EventType = guid, CustomFields = new Dictionary<string, object>() {{"Index", i}}});
            }

            var events = dataProvider.GetAllEvents();
            var count = dataProvider.Count;

            Assert.That(events.Count, Is.EqualTo(10));
            Assert.That(count, Is.EqualTo(10));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_GetConsumingEnumerable()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();

            var guid = Guid.NewGuid().ToString();

            var cts = new CancellationTokenSource();

            var consumed = 0;

            var consumerTask = Task.Run(() =>
            {
                foreach (var auditEvent in dataProvider.GetConsumingEnumerable(cts.Token))
                {
                    consumed++;
                }
            });

            for (int i = 0; i < 10; i++)
            {
                dataProvider.InsertEvent(new AuditEvent() { EventType = guid, CustomFields = new Dictionary<string, object>() { { "Index", i } } });
            }

            var consumerCompleted = consumerTask.Wait(250);

            var count = dataProvider.Count;

            Assert.That(consumerCompleted, Is.False);
            Assert.That(count, Is.Zero);
            Assert.That(consumed, Is.EqualTo(10));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_GetBlockingCollection()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider();

            var guid = Guid.NewGuid().ToString();

            var collection = dataProvider.GetBlockingCollection();

            for (int i = 0; i < 10; i++)
            {
                dataProvider.InsertEvent(new AuditEvent() { EventType = guid, CustomFields = new Dictionary<string, object>() { { "Index", i } } });
            }
            
            var count = collection.Count;

            Assert.That(count, Is.EqualTo(10));
        }

        [Test]
        public async Task Test_BlockingCollectionDataProvider_WithCapacity()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider(null, 5);

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
                var auditEvent = dataProvider.Take();
                Assert.That(auditEvent.EventType, Is.EqualTo(i.ToString()));
            }

            Assert.That(totalProduced, Is.EqualTo(10));
            Assert.That(dataProvider.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_AsStackCollection()
        {
            var dataProvider = new Audit.Core.Providers.BlockingCollectionDataProvider(new ConcurrentStack<AuditEvent>(), null);

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

            Assert.That(produceCompleted, Is.True);
            Assert.That(totalProduced, Is.EqualTo(10));
            Assert.That(dataProvider.Count, Is.EqualTo(10));

            for (int i = 0; i < 10; i++)
            {
                var auditEvent = dataProvider.Take();
                Assert.That(auditEvent.EventType, Is.EqualTo((9 - i).ToString()));
            }

            Assert.That(totalProduced, Is.EqualTo(10));
            Assert.That(dataProvider.Count, Is.EqualTo(0));
        }

        private Type GetInternalCollectionType(BlockingCollection<AuditEvent> bc)
        {
            return (bc.GetType().GetField("m_collection", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? bc.GetType().GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance))!
                .GetValue(bc).GetType();
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_AsQueue()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryBlockingCollectionProvider(c => c
                    .AsQueue()
                    .WithCapacity(100));

            var dataProvider = Configuration.DataProviderAs<BlockingCollectionDataProvider>();
            var internalCollectionType = GetInternalCollectionType(dataProvider.GetBlockingCollection());

            Assert.That(dataProvider, Is.Not.Null);
            Assert.That(dataProvider.GetBlockingCollection().BoundedCapacity, Is.EqualTo(100));
            Assert.That(internalCollectionType.Name, Contains.Substring("Queue"));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_AsStack()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryBlockingCollectionProvider(c => c
                    .AsStack()
                    .WithCapacity(100));

            var dataProvider = Configuration.DataProviderAs<BlockingCollectionDataProvider>();

            var internalCollectionType = GetInternalCollectionType(dataProvider.GetBlockingCollection());

            Assert.That(dataProvider, Is.Not.Null);
            Assert.That(dataProvider.GetBlockingCollection().BoundedCapacity, Is.EqualTo(100));
            Assert.That(internalCollectionType.Name, Contains.Substring("Stack"));
        }

        [Test]
        public void Test_BlockingCollectionDataProvider_AsBag()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryBlockingCollectionProvider(c => c
                    .AsBag()
                    .WithCapacity(100));

            var dataProvider = Configuration.DataProviderAs<BlockingCollectionDataProvider>();

            var internalCollectionType = GetInternalCollectionType(dataProvider.GetBlockingCollection());

            Assert.That(dataProvider, Is.Not.Null);
            Assert.That(dataProvider.GetBlockingCollection().BoundedCapacity, Is.EqualTo(100));
            Assert.That(internalCollectionType.Name, Contains.Substring("Bag"));
        }
    }
}
