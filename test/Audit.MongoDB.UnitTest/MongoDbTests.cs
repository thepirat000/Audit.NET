using System;
using Audit.Core;
using Audit.MongoDB.Providers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using NUnit.Framework;

namespace Audit.MongoDb.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("Mongo")]
    public class MongoDbTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
#if NETCOREAPP3_1 || NET6_0
           // Allow BSON serialization on any type starting with Audit
            var objectSerializer = new ObjectSerializer(type =>
                ObjectSerializer.DefaultAllowedTypes(type) ||
            type.FullName.StartsWith("Audit")
                || type.FullName.StartsWith("MongoDB"));
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
            object eventId = scope.EventId;
            up.UserName = "user2";
            scope.Dispose();

            var eventRead = (Audit.Core.Configuration.DataProvider as MongoDataProvider).GetEvent(eventId);

            Assert.AreEqual("user1", (eventRead.Target.Old as BsonDocument)["UserName"].ToString());
            Assert.AreEqual("user2", (eventRead.Target.New as BsonDocument)["UserName"].ToString());
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

            var eventRead = (Audit.Core.Configuration.DataProvider as MongoDataProvider).GetEvent(eventId);

            Assert.AreEqual("user1", (eventRead.Target.Old as BsonDocument)["UserName"].ToString());
            Assert.AreEqual("user2", (eventRead.Target.New as BsonDocument)["UserName"].ToString());
        }


        [Test]
        public void Test_MongoDataProvider_FluentApi()
        {
            var x = new MongoDB.Providers.MongoDataProvider(_ => _
                .ConnectionString("c")
                .ClientSettings(new MongoClientSettings() { ReadConcern = ReadConcern.Linearizable })
                .Collection("col")
                .Database("db")
                .SerializeAsBson(true)
                .DatabaseSettings(new MongoDatabaseSettings() { ReadConcern = ReadConcern.Majority }));

            Assert.AreEqual("c", x.ConnectionString);
            Assert.AreEqual("col", x.Collection);
            Assert.AreEqual("db", x.Database);
            Assert.AreEqual(true, x.SerializeAsBson);
            Assert.AreEqual(ReadConcern.Majority, x.DatabaseSettings.ReadConcern);
            Assert.AreEqual(ReadConcern.Linearizable, x.ClientSettings.ReadConcern);
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
            using (var s = new AuditScopeFactory().Create("test", null, new { someDate = now }, null, null))
            {
            }

            Audit.Core.Configuration.ResetCustomActions();
            var dp = Audit.Core.Configuration.DataProvider as MongoDataProvider;
            var evt = dp.GetEvent(evId);
            Assert.AreEqual(now.ToString("yyyyMMddHHmmss"),
                DateTime.Parse(evt.CustomFields["someDate"].ToString()).ToUniversalTime().ToString("yyyyMMddHHmmss"));
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
    }
}
