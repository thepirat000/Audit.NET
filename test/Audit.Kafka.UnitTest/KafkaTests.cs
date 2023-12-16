using Audit.Core;
using Audit.Kafka.Providers;
using Confluent.Kafka;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Kafka.UnitTest
{
    [TestFixture]
    public class KafkaTests
    {
        [Test]
        public void Test_KafkaDataProvider_FluentApi()
        {
            var x = new KafkaDataProvider<string>(_ => _
                .ProducerConfig(new ProducerConfig())
                .Topic("audit-topic")
                .Partition(0)
                .KeySelector(ev => "key"));

            Assert.AreEqual("audit-topic", x.TopicSelector.Invoke(null));
            Assert.AreEqual(0, x.PartitionSelector.Invoke(null));
            Assert.AreEqual("key", x.KeySelector.Invoke(null));
        }

        
        [Test]
        [Category("Integration-Kafka")]
        public async Task Test_KafkaDataProvider_Stress()
        {
            var locker = new object();
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            string topic = "topic-" + Guid.NewGuid();
            const string host = "[::1]:9092";
            var pConfig = new ProducerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName()
            };
            Audit.Core.Configuration.Setup()
                .UseKafka(_ => _
                    .ProducerConfig(pConfig)
                    .Topic(topic)
                    .ResultHandler(rpt =>
                    {
                        lock (locker)
                        {
                            reports.Add(rpt);
                        }
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Action<int> insertAction = async (int index) =>
            {
                var scope = await AuditScope.CreateAsync("type1", null, new { index = index });
                await scope.DisposeAsync();
            };

            int count = 50;
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(new Task(() => insertAction(i)));
                tasks[i].Start();
            }
            await Task.WhenAll(tasks.ToArray());

            var waiterTime = Task.Delay(10000);
            var waiterCount = new Task(() => { while (true) { lock(locker) if (reports.Count >= count) break; }; });
            await Task.WhenAny(new Task[] { waiterTime, waiterCount });

            Assert.AreEqual(count, reports.Count);
            Assert.IsTrue(reports.All(r => r.Status == PersistenceStatus.Persisted));
        }


        [Test]
        [Category("Integration-Kafka")]
        public async Task Test_KafkaDataProvider_HappyPath_Async()
        {
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            const string topic = "my-audit-topic-happy-path-async";
            const string host = "[::1]:9092";
            var pConfig = new ProducerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName()
            };
            Audit.Core.Configuration.Setup()
                .UseKafka(_ => _
                    .ProducerConfig(pConfig)
                    .Topic(topic)
                    .ResultHandler(rpt =>
                    {
                        reports.Add(rpt);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var guid = Guid.NewGuid();
            var scope = await AuditScope.CreateAsync("type1", null, new { custom_field = guid });
            scope.Event.CustomFields["custom_field"] = "UPDATED:" + guid;
            await scope.DisposeAsync();

            Assert.AreEqual(2, reports.Count);
            Assert.AreEqual(PersistenceStatus.Persisted, reports[0].Status);
            Assert.AreEqual(PersistenceStatus.Persisted, reports[1].Status);

            var cv = new ConsumerBuilder<Null, AuditEvent>(new ConsumerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName(),
                GroupId = "test-" + guid,
                AutoOffsetReset = AutoOffsetReset.Earliest, 
            }).SetValueDeserializer(new DefaultJsonSerializer<AuditEvent>()).Build();
            cv.Subscribe(topic);
            await Task.Delay(1000);
            cv.Seek(new TopicPartitionOffset(topic, reports[0].Partition, reports[0].Offset));
            var r1 = cv.Consume(1000);
            cv.Seek(new TopicPartitionOffset(topic, reports[1].Partition, reports[1].Offset));
            var r2 = cv.Consume(1000);

            Assert.IsNotNull(r1);
            Assert.IsNotNull(r2);
            Assert.AreEqual(guid.ToString(), r1.Message.Value.CustomFields["custom_field"].ToString());
            Assert.AreEqual("UPDATED:" + guid, r2.Message.Value.CustomFields["custom_field"].ToString());
        }

        [Test]
        [Category("Integration-Kafka")]
        public void Test_KafkaDataProvider_HappyPath()
        {
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            const string topic = "my-audit-topic-happy-path";
            const string host = "[::1]:9092";
            var pConfig = new ProducerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName(), 
                AllowAutoCreateTopics = true
            };
            Audit.Core.Configuration.Setup()
                .UseKafka(_ => _
                    .ProducerConfig(pConfig)
                    .Topic(topic)
                    .Partition(0)
                    .ResultHandler(rpt =>
                    {
                        reports.Add(rpt);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var guid = Guid.NewGuid();
            var scope = AuditScope.Create("type1", null, new { custom_field = guid });
            scope.Event.CustomFields["custom_field"] = "UPDATED:" + guid;
            scope.Dispose();

            Assert.AreEqual(2, reports.Count);
            Assert.AreEqual(PersistenceStatus.Persisted, reports[0].Status);
            Assert.AreEqual(PersistenceStatus.Persisted, reports[1].Status);

            var cv = new ConsumerBuilder<Null, AuditEvent>(new ConsumerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName(),
                GroupId = "test-" + guid,
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).SetValueDeserializer(new DefaultJsonSerializer<AuditEvent>()).Build();

            cv.Subscribe(topic);
            Thread.Sleep(1000);
            cv.Seek(new TopicPartitionOffset(topic, reports[0].Partition, reports[0].Offset));
            var r1 = cv.Consume(1000);
            cv.Seek(new TopicPartitionOffset(topic, reports[0].Partition, reports[1].Offset));
            var r2 = cv.Consume(1000);
            
            Assert.IsNotNull(r1);
            Assert.IsNotNull(r2);
            Assert.AreEqual(guid.ToString(), r1.Message.Value.CustomFields["custom_field"].ToString());
            Assert.AreEqual("UPDATED:" + guid, r2.Message.Value.CustomFields["custom_field"].ToString());
        }

        [Test]
        [Category("Integration-Kafka")]
        public void Test_KafkaDataProvider_KeyedHappyPath()
        {
            var reports = new List<DeliveryResult<string, AuditEvent>>();
            const string topic = "my-audit-topic-keyed-happypath";
            const string host = "[::1]:9092";
            var pConfig = new ProducerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName()
            };
            Audit.Core.Configuration.Setup()
                .UseKafka<string>(_ => _
                    .ProducerConfig(pConfig)
                    .Topic(topic)
                    .ResultHandler(rpt =>
                    {
                        reports.Add(rpt);
                    })
                    .KeySelector(ev => ev.EventType))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var guid = Guid.NewGuid();
            var scope = AuditScope.Create("key1", null, new { custom_field = guid });
            scope.Event.CustomFields["custom_field"] = "UPDATED:" + guid;
            scope.Dispose();

            Assert.AreEqual(1, reports.Count);
            Assert.AreEqual(PersistenceStatus.Persisted, reports[0].Status);

            var cv = new ConsumerBuilder<string, AuditEvent>(new ConsumerConfig()
            {
                BootstrapServers = host,
                ClientId = Dns.GetHostName(),
                GroupId = "test-" + guid,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            }).SetValueDeserializer(new DefaultJsonSerializer<AuditEvent>()).Build();
            cv.Subscribe(topic);
            Thread.Sleep(1000);
            cv.Seek(new TopicPartitionOffset(topic, reports[0].Partition, reports[0].Offset));
            var r1 = cv.Consume(1000);

            Assert.IsNotNull(r1);
            Assert.AreEqual("UPDATED:" + guid, r1.Message.Value.CustomFields["custom_field"].ToString());
            Assert.AreEqual("key1", r1.Message.Key);
        }

        private async Task DeleteTopic(string host, string topic)
        {
            var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
            await admin.DeleteTopicsAsync(new string[] { topic });
        }
    }


}
