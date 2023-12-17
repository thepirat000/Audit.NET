using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Audit.Core;
using Audit.JsonNewtonsoftAdapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Audit.JsonAdapter.UnitTest
{
    [TestFixture]
    public class TestAuditContractResolver
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new AuditContractResolver(),
            Formatting = Newtonsoft.Json.Formatting.Indented
        };

        #region "Sample classes"

        public class TestClassGeneric<TExtData>
        {
            public int Id { get; set; }
            
            [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.Never)] // do not ignore
            public string Name { get; set; }

            [System.Text.Json.Serialization.JsonIgnore]
            public int IgnoredProperty { get; set; }

            [System.Text.Json.Serialization.JsonExtensionData]
            public TExtData MoreData { get; set; }
        }

        public class OnlyNewton
        {
            [Newtonsoft.Json.JsonProperty(Order = 2)]
            public int Id { get; set; }
            [Newtonsoft.Json.JsonProperty(Order = 1, PropertyName = "FullName")]
            public string Name { get; set; }
            [Newtonsoft.Json.JsonExtensionData]
            public JObject ExtraData { get; set; }
        }

        #endregion

        /// <summary>
        /// ExtensionData using IDictionary of String, Object
        /// </summary>
        [Test]
        public void TestAuditContractResolver_ExtDataIDictStrObj()
        {
            var obj = new TestClassGeneric<IDictionary<string, object>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData =
                    new Dictionary<string, object>() { { "InnerProp", new { sub = "Value2" } } }
            };

            SerializeAndAssert(obj);
        }

        /// <summary>
        /// ExtensionData using IDictionary of String, JsonElement
        /// </summary>
        [Test]
        public void TestAuditContractResolver_ExtDataIDictStrJsonElem()
        {
            var obj = new TestClassGeneric<IDictionary<string, JsonElement>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData = new Dictionary<string, JsonElement> { { "InnerProp", JsonDocument.Parse(@"{ ""sub"": ""Value2"" }").RootElement } } 
            };

            SerializeAndAssert(obj);
        }

        /// <summary>
        /// ExtensionData using Dictionary of String, Object
        /// </summary>
        [Test]
        public void TestAuditContractResolver_ExtDataDictStrObj()
        {
            var obj = new TestClassGeneric<Dictionary<string, object>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData =
                    new Dictionary<string, object>() { { "InnerProp", new { sub = "Value2" } } }
            };

            SerializeAndAssert(obj);
        }

        /// <summary>
        /// ExtensionData using Dictionary of String, JsonElement
        /// </summary>
        [Test]
        public void TestAuditContractResolver_ExtDataDictStrJsonElem()
        {
            var obj = new TestClassGeneric<Dictionary<string, JsonElement>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData = new Dictionary<string, JsonElement> { { "InnerProp", JsonDocument.Parse(@"{ ""sub"": ""Value2"" }").RootElement } }
            };

            SerializeAndAssert(obj);
        }

        private void SerializeAndAssert<T>(TestClassGeneric<T> obj)
        {
            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClassGeneric<T>>(serialized, _jsonSerializerSettings);

            Assert.That(serialized, Is.Not.Null);
            Assert.That(serialized.Contains(@"""Id"": 123"), Is.True);
            Assert.That(serialized.Contains(@$"""Name"": ""{obj.Name}"""), Is.True);
            Assert.That(serialized.Contains(@"""InnerProp"": {"), Is.True);
            Assert.That(serialized.Contains(@"""sub"": ""Value2"""), Is.True);
            Assert.IsFalse(serialized.Contains("IgnoredProperty"));
            Assert.IsFalse(serialized.Contains("MoreData"));

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(obj.Id));
            Assert.That(deserialized.Name, Is.EqualTo(obj.Name));
            Assert.AreEqual(1, ((dynamic)deserialized.MoreData).Count);
            var inner = ((dynamic) deserialized.MoreData)["InnerProp"];
            Assert.AreEqual("Value2", inner switch
            {
                JsonElement je => je.GetProperty("sub").GetString(),
                object x => ((dynamic) x)["sub"].ToString()
            });
        }

        [Test]
        public void TestAuditContractResolver_ExtDataDictWrongValueType()
        {
            var obj = new TestClassGeneric<Dictionary<string, string>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData = new Dictionary<string, string> { { "InnerProp", "Test" } } 
            };

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClassGeneric<Dictionary<string, string>>>(serialized, _jsonSerializerSettings);

            Assert.That(serialized, Is.Not.Null);
            Assert.That(serialized.Contains(@"""Id"": 123"), Is.True);
            Assert.That(serialized.Contains(@$"""Name"": ""{obj.Name}"""), Is.True);
            Assert.IsFalse(serialized.Contains("IgnoredProperty"));
            Assert.IsFalse(serialized.Contains("MoreData"));

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(obj.Id));
            Assert.That(deserialized.Name, Is.EqualTo(obj.Name));
            Assert.That(deserialized.MoreData, Is.Null);
        }

        [Test]
        public void TestAuditContractResolver_ExtDataDictWrongType()
        {
            var obj = new TestClassGeneric<OnlyNewton>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData = new OnlyNewton()
            };

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClassGeneric<OnlyNewton>>(serialized, _jsonSerializerSettings);

            Assert.That(serialized, Is.Not.Null);
            Assert.That(serialized.Contains(@"""Id"": 123"), Is.True);
            Assert.That(serialized.Contains(@$"""Name"": ""{obj.Name}"""), Is.True);
            Assert.IsFalse(serialized.Contains("IgnoredProperty"));
            Assert.IsFalse(serialized.Contains("MoreData"));

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(obj.Id));
            Assert.That(deserialized.Name, Is.EqualTo(obj.Name));
            Assert.That(deserialized.MoreData, Is.Null);
        }

        [Test]
        public void TestAuditContractResolver_ExtDataNull()
        {
            var obj = new TestClassGeneric<Dictionary<string, object>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData = null
            };

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClassGeneric<Dictionary<string, object>>>(serialized, _jsonSerializerSettings);

            Assert.That(serialized, Is.Not.Null);
            Assert.That(serialized.Contains(@"""Id"": 123"), Is.True);
            Assert.That(serialized.Contains(@$"""Name"": ""{obj.Name}"""), Is.True);
            Assert.IsFalse(serialized.Contains("IgnoredProperty"));
            Assert.IsFalse(serialized.Contains("MoreData"));

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(obj.Id));
            Assert.That(deserialized.Name, Is.EqualTo(obj.Name));
            Assert.That(deserialized.MoreData, Is.Null);
        }

        [Test]
        public void TestAuditContractResolver_ExtDataValueNull()
        {
            var obj = new TestClassGeneric<Dictionary<string, object>>()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                IgnoredProperty = 2,
                MoreData = new Dictionary<string, object>()
                {
                    {"empty", null}
                }
            };

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClassGeneric<Dictionary<string, object>>>(serialized, _jsonSerializerSettings);

            Assert.That(serialized, Is.Not.Null);
            Assert.That(serialized.Contains(@"""empty"": null"), Is.True);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.MoreData["empty"], Is.Null);
        }

        [Test]
        public void TestAuditContractResolver_OnlyNewtonsoft()
        {
            var obj = new OnlyNewton()
            {
                Id = 123,
                Name = Guid.NewGuid().ToString(),
                ExtraData = JObject.FromObject(new { a = 1, b = 2 })
            };

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<OnlyNewton>(serialized, _jsonSerializerSettings);

            Assert.That(serialized, Is.Not.Null);
            Assert.That(serialized.Contains(@"""Id"": 123"), Is.True);
            Assert.That(serialized.Contains(@$"""a"": 1"), Is.True);
            Assert.That(serialized.Contains(@$"""b"": 2"), Is.True);
            Assert.That(serialized.Contains(@"""FullName"": """), Is.True);
            Assert.IsFalse(serialized.Contains("ExtraData"));
            Assert.That(serialized.IndexOf(@"""FullName""") < serialized.IndexOf(@"""Id"""), Is.True);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(obj.Id));
            Assert.That(deserialized.Name, Is.EqualTo(obj.Name));
            Assert.That((deserialized.ExtraData as JToken)["a"].ToString(), Is.EqualTo("1"));
            Assert.That((deserialized.ExtraData as JToken)["b"].ToString(), Is.EqualTo("2"));
        }

        [Test]
        public void TestAuditContractResolver_AuditEvent()
        {
            var target = new { a = 1, b = 2 };
            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter()
                .UseNullProvider();
                
            var scope = AuditScope.Create("test", () => target, new { extra = new { sub = 10 } });
            var ev = scope.Event;
            ev.Environment.CustomFields["env"] = "DEV";
            target = new { a = 3, b = 4 };
            scope.Dispose();

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(ev, _jsonSerializerSettings);
            var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(serialized, _jsonSerializerSettings);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Environment.UserName, Is.EqualTo(ev.Environment.UserName));
            Assert.That(deserialized.Environment.CustomFields["env"].ToString(), Is.EqualTo("DEV"));
            Assert.That(deserialized.EventType, Is.EqualTo("test"));
            Assert.That((deserialized.Target.Old as JToken)["a"].ToString(), Is.EqualTo("1"));
            Assert.That((deserialized.Target.New as JToken)["a"].ToString(), Is.EqualTo("3"));
        }

    }
}
