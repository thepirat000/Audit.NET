using Audit.Core.Providers;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using Audit.IntegrationTest;

namespace Audit.MongoClient.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.MongoDb)]
    public class AuditMongoClientIntegrationTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void AuditMongo_Integration()
        {
            // Arrange
            Audit.Core.Configuration.Setup().UseInMemoryProvider();

            var eventType = Guid.NewGuid().ToString();

            var mongoSettings = new MongoClientSettings()
            {
                Server = new MongoServerAddress("localhost", 27017),
                ClusterConfigurator = cc => cc
                    // Add the audit subscriber to the cluster config
                    .AddAuditSubscriber(auditConfig => auditConfig
                        .EventType(eventType)
                        .IncludeReply())
            };

            var client = new MongoDB.Driver.MongoClient(mongoSettings);

            var collection = client.GetDatabase("AuditTest").GetCollection<BsonDocument>("MongoClient");

            // Act
            var bson = new { test = new string('z', 40000) }.ToBsonDocument();

            // Insert
            collection.InsertOne(bson);

            // Find
            var filterById = Builders<BsonDocument>.Filter.Eq("_id", (BsonObjectId)bson["_id"]);
            var item = collection.Find(filterById).FirstOrDefault();

            // Delete
            collection.DeleteOne(filterById); 

            // Assert
            var evs = Audit.Core.Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEventsOfType<AuditEventMongoCommand>();

            // Assert
            Assert.That(evs, Is.Not.Null);
            Assert.That(evs.Count, Is.EqualTo(3));
            Assert.That(evs[0].Command.CommandName, Is.EqualTo("insert"));
            Assert.That(evs[1].Command.CommandName, Is.EqualTo("find"));
            Assert.That(evs[2].Command.CommandName, Is.EqualTo("delete"));
        }
    }
}
