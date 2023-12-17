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

            Assert.That((eventRead.Target.Old as BsonDocument)["UserName"].ToString(), Is.EqualTo("user1"));
            Assert.That((eventRead.Target.New as BsonDocument)["UserName"].ToString(), Is.EqualTo("user2"));
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

            Assert.That((eventRead.Target.Old as BsonDocument)["UserName"].ToString(), Is.EqualTo("user1"));
            Assert.That((eventRead.Target.New as BsonDocument)["UserName"].ToString(), Is.EqualTo("user2"));
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

            Assert.That(x.ConnectionString, Is.EqualTo("c"));
            Assert.That(x.Collection, Is.EqualTo("col"));
            Assert.That(x.Database, Is.EqualTo("db"));
            Assert.That(x.SerializeAsBson, Is.EqualTo(true));
            Assert.That(x.DatabaseSettings.ReadConcern, Is.EqualTo(ReadConcern.Majority));
            Assert.That(x.ClientSettings.ReadConcern, Is.EqualTo(ReadConcern.Linearizable));
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
            var dp = Audit.Core.Configuration.DataProvider as MongoDataProvider;
            var evt = dp.GetEvent(evId);
            Assert.That(DateTime.Parse(evt.CustomFields["someDate"].ToString()).ToUniversalTime().ToString("yyyyMMddHHmmss"), Is.EqualTo(now.ToString("yyyyMMddHHmmss")));
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
