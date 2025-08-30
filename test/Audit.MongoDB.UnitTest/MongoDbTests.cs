using System;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.IntegrationTest;
using Audit.MongoDB.Providers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace Audit.MongoDb.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.MongoDb)]
    public class MongoDbTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
#if NET472 || NETCOREAPP3_1 || NET6_0_OR_GREATER
           // Allow BSON serialization on any type starting with Audit
            var objectSerializer = new ObjectSerializer(type =>
            ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("Audit") || type.FullName.StartsWith("MongoDB"));
            BsonSerializer.RegisterSerializer(objectSerializer);
#endif
        }

        [Test]
        public void Test_Mongo_ObjectId()
        {
            Audit.Core.Configuration.Setup()
                .UseMongoDB(config => config
                    .ConnectionString("mongodb://localhost:27017")
                    .Database("Audit")
                    .Collection("Event")
                    .SerializeAsBson())
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var up = new UserProfiles()
            {
                UserName = "user1"
            };

            var scope = AuditScope.Create("test", () => up);
            var eventId = scope.EventId;
            up.UserName = "user2";
            scope.Dispose();

            var eventRead = Core.Configuration.DataProviderAs<MongoDataProvider>().GetEvent(eventId);

            Assert.That(((BsonDocument)eventRead.Target.Old)["UserName"].ToString(), Is.EqualTo("user1"));
            Assert.That(((BsonDocument)eventRead.Target.New)["UserName"].ToString(), Is.EqualTo("user2"));
        }

        [Test]
        public async Task Test_Mongo_ObjectIdAsync()
        {
            Audit.Core.Configuration.Setup()
                .UseMongoDB(config => config
                    .ConnectionString("mongodb://localhost:27017")
                    .Database("Audit")
                    .Collection("Event")
                    .SerializeAsBson())
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var up = new UserProfiles()
            {
                UserName = "user1"
            };

            var dp = Configuration.DataProviderAs<MongoDataProvider>();
            dp.TestConnection();
            var collection = dp.GetMongoCollection();
            var collectionAuditEvent = dp.GetMongoCollection<AuditEvent>();
            var eventType = Guid.NewGuid().ToString();

            var scope = await AuditScope.CreateAsync(eventType, () => up);
            var eventId = scope.EventId;
            up.UserName = "user2";
            await scope.DisposeAsync();

            var eventRead = await dp.GetEventAsync(eventId);

            var eventQuery = dp.QueryEvents().FirstOrDefault(d => d.EventType == eventType);
            var eventQueryAuditEvent = dp.QueryEvents<AuditEvent>().FirstOrDefault(d => d.EventType == eventType);

            Assert.That(eventQuery, Is.Not.Null);
            Assert.That(eventQueryAuditEvent, Is.Not.Null);
            Assert.That(collection.Database.DatabaseNamespace.DatabaseName, Is.EqualTo("Audit"));
            Assert.That(collectionAuditEvent.Database.DatabaseNamespace.DatabaseName, Is.EqualTo("Audit"));
            Assert.That(((BsonDocument)eventRead.Target.Old)["UserName"].ToString(), Is.EqualTo("user1"));
            Assert.That(((BsonDocument)eventRead.Target.New)["UserName"].ToString(), Is.EqualTo("user2"));
        }

        [Test]
        public void Test_Mongo_ClientSettings()
        {
            Audit.Core.Configuration.Setup()
                .UseMongoDB(config => config
                    .ClientSettings(new MongoClientSettings() { Server = new MongoServerAddress("localhost", 27017) })
                    .ConnectionString("mongodb://WRONG_HOST:10001")
                    .Database("Audit")
                    .Collection("Event")
                    .SerializeAsBson())
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var up = new UserProfiles()
            {
                UserName = "user1"
            };

            object eventId = null;
            using (var scope = AuditScope.Create("test", () => up))
            {
                eventId = scope.EventId;
                up.UserName = "user2";
            }

            var eventRead = Core.Configuration.DataProviderAs<MongoDataProvider>().GetEvent(eventId);

            Assert.That((eventRead.Target.Old as BsonDocument)["UserName"].ToString(), Is.EqualTo("user1"));
            Assert.That((eventRead.Target.New as BsonDocument)["UserName"].ToString(), Is.EqualTo("user2"));
        }


        [Test]
        public void Test_MongoDataProvider_FluentApi()
        {
            var dataProvider = new MongoDB.Providers.MongoDataProvider(_ => _
                .ConnectionString("c")
                .ClientSettings(new MongoClientSettings() { ReadConcern = ReadConcern.Linearizable })
                .Collection("col")
                .Database("db")
                .SerializeAsBson(true)
                .DatabaseSettings(new MongoDatabaseSettings() { ReadConcern = ReadConcern.Majority }));

            Assert.That(dataProvider.ConnectionString, Is.EqualTo("c"));
            Assert.That(dataProvider.Collection, Is.EqualTo("col"));
            Assert.That(dataProvider.Database, Is.EqualTo("db"));
            Assert.That(dataProvider.SerializeAsBson, Is.EqualTo(true));
            Assert.That(dataProvider.DatabaseSettings.ReadConcern, Is.EqualTo(ReadConcern.Majority));
            Assert.That(dataProvider.ClientSettings.ReadConcern, Is.EqualTo(ReadConcern.Linearizable));
        }

        [Test]
        public void TestMongoDateSerialization()
        {
            Audit.Core.Configuration.Setup()
                .UseMongoDB(config => config
                    .ConnectionString("mongodb://localhost:27017")
                    .Database("Audit")
                    .Collection("Event"))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();

            object evId = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, s =>
            {
                if (evId != null)
                {
                    Assert.Fail("evId should be null");
                }

                evId = s.EventId;
            });
            var now = DateTime.UtcNow;
            var scope = new AuditScopeFactory().Create("test", null, new { someDate = now }, null, null);
            scope.Dispose();

            Audit.Core.Configuration.ResetCustomActions();
            var dp = Configuration.DataProviderAs<MongoDataProvider>();
            var evt = dp.GetEvent(evId);
            Assert.That(DateTime.Parse(evt.CustomFields["someDate"].ToString()).ToUniversalTime().ToString("yyyyMMddHHmmss"), Is.EqualTo(now.ToString("yyyyMMddHHmmss")));
        }

        [Test]
        public void FixDocumentElementNames_ReplacesDotsAndDollarSignsInRoot()
        {
            var doc = new BsonDocument
            {
                { "field.with.dot", 1 },
                { "$fieldWithDollar", 2 },
                { "normalField", 3 }
            };

            var provider = new TestMongoDataProvider();
            provider.FixDocumentElementNames(doc);

            Assert.That(doc.Contains("field_with_dot"), Is.True);
            Assert.That(doc.Contains("_fieldWithDollar"), Is.True);
            Assert.That(doc.Contains("normalField"), Is.True);
            Assert.That(doc.Contains("field.with.dot"), Is.False);
            Assert.That(doc.Contains("$fieldWithDollar"), Is.False);
            Assert.That(doc["field_with_dot"].AsInt32, Is.EqualTo(1));
            Assert.That(doc["_fieldWithDollar"].AsInt32, Is.EqualTo(2));
            Assert.That(doc["normalField"].AsInt32, Is.EqualTo(3));
        }

        [Test]
        public void FixDocumentElementNames_ReplacesInNestedDocuments()
        {
            var nested = new BsonDocument
            {
                { "nested.with.dot", 10 },
                { "$nestedWithDollar", 20 }
            };
            var doc = new BsonDocument
            {
                { "level1", nested }
            };

            var provider = new TestMongoDataProvider();
            provider.FixDocumentElementNames(doc);

            var fixedNested = doc["level1"].AsBsonDocument;
            Assert.That(fixedNested.Contains("nested_with_dot"), Is.True);
            Assert.That(fixedNested.Contains("_nestedWithDollar"), Is.True);
            Assert.That(fixedNested.Contains("nested.with.dot"), Is.False);
            Assert.That(fixedNested.Contains("$nestedWithDollar"), Is.False);
            Assert.That(fixedNested["nested_with_dot"].AsInt32, Is.EqualTo(10));
            Assert.That(fixedNested["_nestedWithDollar"].AsInt32, Is.EqualTo(20));
        }

        [Test]
        public void FixDocumentElementNames_ReplacesInArraysOfDocuments()
        {
            var arr = new BsonArray
            {
                new BsonDocument { { "a.b", 1 }, { "$c", 2 } },
                new BsonDocument { { "x.y", 3 }, { "$z", 4 } }
            };
            var doc = new BsonDocument
            {
                { "array", arr }
            };

            var provider = new TestMongoDataProvider();
            provider.FixDocumentElementNames(doc);

            var fixedArr = doc["array"].AsBsonArray;
            Assert.That(fixedArr[0].AsBsonDocument.Contains("a_b"), Is.True);
            Assert.That(fixedArr[0].AsBsonDocument.Contains("_c"), Is.True);
            Assert.That(fixedArr[0].AsBsonDocument.Contains("a.b"), Is.False);
            Assert.That(fixedArr[0].AsBsonDocument.Contains("$c"), Is.False);
            Assert.That(fixedArr[1].AsBsonDocument.Contains("x_y"), Is.True);
            Assert.That(fixedArr[1].AsBsonDocument.Contains("_z"), Is.True);
            Assert.That(fixedArr[1].AsBsonDocument.Contains("x.y"), Is.False);
            Assert.That(fixedArr[1].AsBsonDocument.Contains("$z"), Is.False);
        }

        [Test]
        public void FixDocumentElementNames_DoesNothingIfNoInvalidNames()
        {
            var doc = new BsonDocument
            {
                { "field1", 1 },
                { "field2", 2 }
            };

            var provider = new TestMongoDataProvider();
            provider.FixDocumentElementNames(doc);

            Assert.That(doc.Contains("field1"), Is.True);
            Assert.That(doc.Contains("field2"), Is.True);
            Assert.That(doc["field1"].AsInt32, Is.EqualTo(1));
            Assert.That(doc["field2"].AsInt32, Is.EqualTo(2));
        }

        [Test]
        public void FixDocumentElementNames_HandlesEmptyDocument()
        {
            var doc = new BsonDocument();

            var provider = new TestMongoDataProvider();
            provider.FixDocumentElementNames(doc);

            Assert.That(doc.ElementCount, Is.EqualTo(0));
        }

        [Test]
        public void CloneValue_ReturnsNull_WhenValueIsNull()
        {
            var provider = new TestMongoDataProvider();
            var result = provider.CloneValue<object>(null, new AuditEvent());
            Assert.That(result, Is.Null);
        }

        [Test]
        public void CloneValue_ReturnsString_WhenValueIsString()
        {
            var provider = new TestMongoDataProvider();
            var result = provider.CloneValue("test", new AuditEvent());
            Assert.That(result, Is.EqualTo("test"));
        }

        [Test]
        public void CloneValue_ReturnsBsonDocument_WhenValueIsBsonDocument_AndSerializeAsBson()
        {
            var provider = new TestMongoDataProvider { SerializeAsBson = true };
            var bsonDoc = new BsonDocument("a", 1);
            var result = provider.CloneValue(bsonDoc, new AuditEvent());
            Assert.That(result, Is.SameAs(bsonDoc));
        }

        [Test]
        public void CloneValue_ReturnsBsonValue_WhenValueIsBsonValue_AndSerializeAsBson()
        {
            var provider = new TestMongoDataProvider { SerializeAsBson = true };
            var bsonValue = new BsonInt32(42);
            var result = provider.CloneValue(bsonValue, new AuditEvent());
            Assert.That(result, Is.SameAs(bsonValue));
        }

        [Test]
        public void CloneValue_MapsToBsonValue_WhenPossible_AndSerializeAsBson()
        {
            var provider = new TestMongoDataProvider { SerializeAsBson = true };
            var value = 123; // int can be mapped to BsonValue
            var result = provider.CloneValue(value, new AuditEvent());
            Assert.That(result, Is.TypeOf<BsonInt32>());
            Assert.That(((BsonInt32)result).Value, Is.EqualTo(123));
        }

        [Test]
        public void CloneValue_ConvertsToBsonDocument_WhenNotBsonValue_AndSerializeAsBson()
        {
            var provider = new TestMongoDataProvider { SerializeAsBson = true };
            var value = new { Name = "abc", Value = 5 };
            var result = provider.CloneValue(value, new AuditEvent());
            Assert.That(result, Is.TypeOf<BsonDocument>());
            Assert.That(((BsonDocument)result).Contains("Name"), Is.True);
            Assert.That(((BsonDocument)result)["Name"].AsString, Is.EqualTo("abc"));
            Assert.That(((BsonDocument)result)["Value"].AsInt32, Is.EqualTo(5));
        }

        [Test]
        public void CloneValue_CallsBase_WhenSerializeAsBsonIsFalse()
        {
            var provider = new TestMongoDataProvider { SerializeAsBson = false };
            var value = new { Name = "abc" };
            var result = provider.CloneValue(value, new AuditEvent());

            Assert.That(result, Is.EqualTo(value));
        }

        public class UserProfiles
        {
            [BsonId] public ObjectId Id { get; set; }
            public int UserId { get; set; }
            [BsonRequired] public string UserName { get; set; }
            [BsonRequired] public string Password { get; set; }
            public string Role { get; set; }
            [BsonRequired] public string Email { get; set; }
            [BsonRequired] public string ProjectId { get; set; }
        }

        private class TestMongoDataProvider : MongoDataProvider
        {
            public new void FixDocumentElementNames(BsonDocument document)
            {
                base.FixDocumentElementNames(document);
            }
        }
    }
}
