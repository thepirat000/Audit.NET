using Audit.Core;
using Audit.IntegrationTest;
using Audit.Kafka.Providers;

using Confluent.Kafka;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Audit.Kafka.UnitTest
{
    [TestFixture]
    public class KafkaTests
    {
		private const string BootstrapHost = "127.0.0.1:9094";


        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

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
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.Kafka)]
        public async Task Test_KafkaDataProvider_Stress()
        {
            var locker = new object();
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            var topic = "topic-" + Guid.NewGuid();

            var pConfig = new ProducerConfig()
            {
                BootstrapServers = BootstrapHost,
                ClientId = Dns.GetHostName(),
                AllowAutoCreateTopics = true,
                SecurityProtocol = SecurityProtocol.Plaintext
            };
            Audit.Core.Configuration.Setup()
                .UseKafka(cfg => cfg
                    .ProducerConfig(pConfig)
                    .Topic(topic)
                    .ResultHandler(rpt =>
                    {
                        lock (locker)
                        {
                            rpt.Value = AuditEvent.FromJson(rpt.Value.ToJson());
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
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.Kafka)]
        public async Task Test_KafkaDataProvider_HappyPath_Async()
        {
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            const string topic = "my-audit-topic-happy-path-async";

            var pConfig = new ProducerConfig()
            {
                BootstrapServers = BootstrapHost,
                ClientId = Dns.GetHostName(),
                AllowAutoCreateTopics = true,
                SecurityProtocol = SecurityProtocol.Plaintext
            };
            Audit.Core.Configuration.Setup()
                .UseKafka(cfg => cfg
                    .ProducerConfig(pConfig)
                    .Topic(topic)
                    .HeadersSelector(ev => new Headers { { "Type", Encoding.UTF8.GetBytes(ev.EventType) } })
                    .ResultHandler(rpt =>
                    {
                        rpt.Value = AuditEvent.FromJson(rpt.Value.ToJson());
                        reports.Add(rpt);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var guid = Guid.NewGuid();
            var scope = await AuditScope.CreateAsync("type1", null, new { custom_field = guid });
            scope.Event.CustomFields["custom_field"] = "UPDATED:" + guid;
            await scope.DisposeAsync();

            Assert.That(reports.Count, Is.EqualTo(2));

            var r1 = reports[0];
            var r2 = reports[1];

            Assert.That(r1.Status, Is.EqualTo(PersistenceStatus.Persisted));
            Assert.That(r2.Status, Is.EqualTo(PersistenceStatus.Persisted));
            
            Assert.That(r1, Is.Not.Null);
            Assert.That(r2, Is.Not.Null);
            Assert.That(r1.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo(guid.ToString()));
            Assert.That(r2.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo("UPDATED:" + guid));
            Assert.That(r1.Headers[0].Key, Is.EqualTo("Type"));
            Assert.That(r1.Headers[0].GetValueBytes(), Is.EqualTo(Encoding.UTF8.GetBytes("type1")));
            Assert.That(r2.Headers[0].Key, Is.EqualTo("Type"));
            Assert.That(r2.Headers[0].GetValueBytes(), Is.EqualTo(Encoding.UTF8.GetBytes("type1")));
        }

        [Test]
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.Kafka)]
        public void Test_KafkaDataProvider_HappyPath()
        {
            var reports = new List<DeliveryResult<Null, AuditEvent>>();
            var topic = "my-audit-topic-happy-path";
            
            var pConfig = new ProducerConfig()
            {
                BootstrapServers = BootstrapHost,
                ClientId = Dns.GetHostName(), 
                AllowAutoCreateTopics = true,
                SecurityProtocol = SecurityProtocol.Plaintext
            };
            Audit.Core.Configuration.Setup()
                .UseKafka(cfg => cfg
                    .ProducerConfig(pConfig)
                    .HeadersSelector(ev => new Headers { { "Type", Encoding.UTF8.GetBytes(ev.EventType) } })
                    .Topic(topic)
                    .Partition(0)
                    .ResultHandler(rpt =>
                    {
                        rpt.Value = AuditEvent.FromJson(rpt.Value.ToJson());
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
            
            Assert.That(reports.Count, Is.EqualTo(2));

            var msg1 = reports[0];
            var msg2 = reports[1];

            Assert.That(msg1, Is.Not.Null);
            Assert.That(msg2, Is.Not.Null);

            Assert.That(msg1.Status, Is.EqualTo(PersistenceStatus.Persisted));
            Assert.That(msg2.Status, Is.EqualTo(PersistenceStatus.Persisted));

            Assert.That(msg1.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo(guid.ToString()));
            Assert.That(msg2.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo("UPDATED:" + guid));
            Assert.That(msg1.Headers[0].Key, Is.EqualTo("Type"));
            Assert.That(msg1.Headers[0].GetValueBytes(), Is.EqualTo("type1"u8.ToArray()));
            Assert.That(msg2.Headers[0].Key, Is.EqualTo("Type"));
            Assert.That(msg2.Headers[0].GetValueBytes(), Is.EqualTo("type1"u8.ToArray()));
        }

        [Test]
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.Kafka)]
        public void Test_KafkaDataProvider_KeyedHappyPath()
        {
            var reports = new List<DeliveryResult<string, AuditEvent>>();
            const string topic = "my-audit-topic-keyed-happypath";

            var pConfig = new ProducerConfig()
            {
                BootstrapServers = BootstrapHost,
                ClientId = Dns.GetHostName(),
                AllowAutoCreateTopics = true,
                SecurityProtocol = SecurityProtocol.Plaintext
            };
            Audit.Core.Configuration.Setup()
                .UseKafka<string>(_ => _
                    .ProducerConfig(pConfig)
                    .HeadersSelector(ev => new Headers { { "Type", Encoding.UTF8.GetBytes(ev.EventType) } })
                    .Topic(topic)
                    .ResultHandler(rpt =>
                    {
                        rpt.Value = AuditEvent.FromJson(rpt.Value.ToJson());
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

            Assert.That(reports.Count, Is.EqualTo(1));

            var r1 = reports[0];
            
            Assert.That(r1.Status, Is.EqualTo(PersistenceStatus.Persisted));
            Assert.That(r1.Message.Value.CustomFields["custom_field"].ToString(), Is.EqualTo("UPDATED:" + guid));
            Assert.That(r1.Message.Key, Is.EqualTo("key1"));
        }

        [Test]
        public void Serialize_ShouldReturnUtf8JsonBytes()
        {
            // Arrange
            var testObj = new TestMessage { Sender = "A", Receiver = "B" };
            var expectedJson = "{\"Sender\":\"A\",\"Receiver\":\"B\"}";

            var serializer = new DefaultJsonSerializer<TestMessage>();

            // Act
            var result = serializer.Serialize(testObj, new SerializationContext());

            // Assert
            Assert.That(result, Is.EqualTo(Encoding.UTF8.GetBytes(expectedJson)));
        }

        [Test]
        public void Deserialize_ShouldReturnObject_WhenDataIsNotNull()
        {
            // Arrange
            var expectedObj = new TestMessage { Sender = "A", Receiver = "B" };
            var json = "{\"Sender\":\"A\",\"Receiver\":\"B\"}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var serializer = new DefaultJsonSerializer<TestMessage>();

            // Act
            var result = serializer.Deserialize(bytes, false, new SerializationContext());

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Sender, Is.EqualTo("A"));
            Assert.That(result.Receiver, Is.EqualTo("B"));
        }

        public record TestMessage
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
        }
    }


}
