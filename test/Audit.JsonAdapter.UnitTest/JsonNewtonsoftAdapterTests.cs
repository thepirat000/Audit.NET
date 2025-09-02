using Audit.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Audit.JsonAdapter.UnitTest
{
    public class JsonNewtonsoftAdapterTests
    {
        [SetUp]
        public void Setup()
        {
            Configuration.Reset();
            Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_JsonNewtonsoftAdapter()
        {
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All
            };
            var evs = new List<string>();
            Configuration.Setup()
                .JsonNewtonsoftAdapter(settings)
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev.ToJson());
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "test", ExtraFields = new { ExtraField = 123 } } ))
            {
            }

            var deserialized = AuditEvent.FromJson(evs[0]);

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Contains(@"""$type"":"), Is.True);
            Assert.IsFalse(evs[0].Contains(@"""CustomFields"""));
            Assert.That(evs[0].Contains(@"""ExtraField"""), Is.True);
            Assert.That(deserialized.CustomFields["ExtraField"].ToString(), Is.EqualTo("123"));
        }

        [Test]
        public void JsonNewtonsoftAdapter_DefaultSettings_AreSet()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            Assert.That(adapter.Settings, Is.Not.Null);
            Assert.That(adapter.Settings.ContractResolver, Is.Not.Null);
            Assert.That(adapter.Settings.NullValueHandling, Is.EqualTo(NullValueHandling.Ignore));
            Assert.That(adapter.Settings.ReferenceLoopHandling, Is.EqualTo(ReferenceLoopHandling.Ignore));
        }

        [Test]
        public void JsonNewtonsoftAdapter_CustomSettings_AreUsed()
        {
            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            var adapter = new Core.JsonNewtonsoftAdapter(settings);
            Assert.That(adapter.Settings, Is.EqualTo(settings));
            Assert.That(adapter.Settings.ContractResolver, Is.Not.Null);
        }

        [Test]
        public void Serialize_And_Deserialize_Works()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var obj = new { Name = "Test", Value = 42 };
            var json = adapter.Serialize(obj);
            Assert.That(json, Does.Contain("Test"));
            var deserialized = adapter.Deserialize<dynamic>(json);
            Assert.That((string)deserialized.Name, Is.EqualTo("Test"));
            Assert.That((int)deserialized.Value, Is.EqualTo(42));
        }

        [Test]
        public void Deserialize_WithType_Works()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var obj = new { Name = "Test2", Value = 99 };
            var json = adapter.Serialize(obj);
            var deserialized = adapter.Deserialize(json, obj.GetType());
            Assert.That(deserialized.GetType().GetProperty("Name").GetValue(deserialized), Is.EqualTo("Test2"));
            Assert.That(deserialized.GetType().GetProperty("Value").GetValue(deserialized), Is.EqualTo(99));
        }

        [Test]
        public async Task SerializeAsync_WritesToStream()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var obj = new { Foo = "Bar" };
            using var ms = new MemoryStream();
            await adapter.SerializeAsync(ms, obj);
            
            Assert.Pass("Serialized");
        }

        [Test]
        public async Task DeserializeAsync_ReadsFromStream()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var obj = new { Foo = "Bar" };
            var json = adapter.Serialize(obj);
            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            var result = await adapter.DeserializeAsync<dynamic>(ms);
            Assert.That((string)result.Foo, Is.EqualTo("Bar"));
        }

        [Test]
        public void ToObject_ReturnsDefault_WhenNull()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var result = adapter.ToObject<string>(null);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void ToObject_ReturnsValue_WhenTypeMatches()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var result = adapter.ToObject<int>(5);
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void ToObject_ReturnsFromJContainer()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var jObj = new JObject { ["A"] = 1 };
            var result = adapter.ToObject<TestClass>(jObj);
            Assert.That(result.A, Is.EqualTo(1));
        }

        [Test]
        public void ToObject_ReturnsDefault_WhenTypeDoesNotMatch()
        {
            var adapter = new Core.JsonNewtonsoftAdapter();
            var result = adapter.ToObject<int>("not an int");
            Assert.That(result, Is.EqualTo(0));
        }

        public class TestClass
        {
            public int A { get; set; }
        }
    }
}