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
		private const string host = "127.0.0.1:59351";
		
        [Test]
        public void Test_KafkaDataProvider_FluentApi()
        {
            var x = new KafkaDataProvider<string>(_ => _
                .ProducerConfig(new ProducerConfig())
                .Topic("audit-topic")
                .Partition(0)
                .KeySelector(ev => "key"));

            Assert.That(x.Topic.GetDefault(), Is.EqualTo("audit-topic"));
            Assert.That(x.Partition.GetDefault(), Is.EqualTo(0));
            Assert.That(x.KeySelector.Invoke(null), Is.EqualTo("key"));
        }

        
        [Test]
        [Category("Integration")]
        [Category("Kafka")]
        public async Task Test_KafkaDataProvider_Stress()
        {
            var locker = new object();
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            string topic = "topic-" + Guid.NewGuid();

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

            Assert.That(reports.Count, Is.EqualTo(count));
            Assert.That(reports.All(r => r.Status == PersistenceStatus.Persisted), Is.True);
        }


        [Test]
        [Category("Integration")]
        [Category("Kafka")]
        public async Task Test_KafkaDataProvider_HappyPath_Async()
        {
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            const string topic = "my-audit-topic-happy-path-async";

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

            Assert.That(reports.Count, Is.EqualTo(2));
            Assert.That(reports[0].Status, Is.EqualTo(PersistenceStatus.Persisted));
            Assert.That(reports[1].Status, Is.EqualTo(PersistenceStatus.Persisted));

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

            Assert.That(r1, Is.Not.Null);
            Assert.That(r2, Is.Not.Null);
            Assert.That(r1.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo(guid.ToString()));
            Assert.That(r2.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo("UPDATED:" + guid));
        }

        [Test]
        [Category("Integration")]
        [Category("Kafka")]
        public void Test_KafkaDataProvider_HappyPath()
        {
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            const string topic = "my-audit-topic-happy-path";
            
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

            Assert.That(reports.Count, Is.EqualTo(2));
            Assert.That(reports[0].Status, Is.EqualTo(PersistenceStatus.Persisted));
            Assert.That(reports[1].Status, Is.EqualTo(PersistenceStatus.Persisted));

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

            Assert.That(r1, Is.Not.Null);
            Assert.That(r2, Is.Not.Null);
            Assert.That(r1.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo(guid.ToString()));
            Assert.That(r2.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo("UPDATED:" + guid));
        }

        [Test]
        [Category("Integration")]
        [Category("Kafka")]
        public void Test_KafkaDataProvider_KeyedHappyPath()
        {
            var reports = new List<DeliveryResult<string, AuditEvent>>();
            const string topic = "my-audit-topic-keyed-happypath";

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

            Assert.That(reports.Count, Is.EqualTo(1));
            Assert.That(reports[0].Status, Is.EqualTo(PersistenceStatus.Persisted));

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

            Assert.That(r1, Is.Not.Null);
            Assert.That(r1.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo("UPDATED:" + guid));
            Assert.That(r1.Message.Key, Is.EqualTo("key1"));
        }

        private async Task DeleteTopic(string host, string topic)
        {
            var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
            await admin.DeleteTopicsAsync(new string[] { topic });
        }
    }


}
