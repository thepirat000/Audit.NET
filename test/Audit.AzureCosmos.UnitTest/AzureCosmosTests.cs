using Audit.AzureCosmos.Providers;
using Audit.Core;
using Audit.IntegrationTest;

using Microsoft.Azure.Cosmos;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace Audit.AzureCosmos.UnitTest
{
    [TestFixture]
    public class AzureCosmosTests
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            var cosmosClient = new CosmosClient(TestCommon.AzureDocDbUrl, TestCommon.AzureDocDbAuthKey, new CosmosClientOptions()
            {
                HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }),
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true
            });
            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("Audit", ThroughputProperties.CreateAutoscaleThroughput(10), null, CancellationToken.None);
            var container = await  database.Database.CreateContainerIfNotExistsAsync("AuditTest", "/EventType");
        }

        [Test]
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.Azure)]
        [Category(TestCommon.Category.AzureCosmos)]
        public void TestAzureCosmos_CustomId()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = TestCommon.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = TestCommon.AzureDocDbAuthKey,
                IdBuilder = _ => id,
                CosmosClientOptionsAction = options =>
                {
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    options.ConnectionMode = ConnectionMode.Gateway;
                    options.LimitToEndpoint = true;
                }
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(1000, 9999);
            var auditEvent = new AuditEvent();
            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                AuditEvent = auditEvent
            }))
            {
                scope.SetCustomField("value", "added");
            };

            var ev = dp.GetEvent(id, eventType);

            Assert.That(auditEvent.CustomFields["id"].ToString(), Is.EqualTo(id));
            Assert.That(ev.CustomFields["value"].ToString(), Is.EqualTo(auditEvent.CustomFields["value"].ToString()));
            Assert.That(auditEvent.EventType, Is.EqualTo(eventType));
        }
        
        [Test]
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.Azure)]
        [Category(TestCommon.Category.AzureCosmos)]
        public async Task TestAzureCosmos_CustomIdAsync()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            var dp = new AzureCosmos.Providers.AzureCosmosDataProvider()
            {
                Endpoint = TestCommon.AzureDocDbUrl,
                Database = "Audit",
                Container = "AuditTest",
                AuthKey = TestCommon.AzureDocDbAuthKey,
                IdBuilder = _ => id,
                CosmosClientOptionsAction = options =>
                {
                    options.HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });
                    options.ConnectionMode = ConnectionMode.Gateway;
                }
            };
            var eventType = TestContext.CurrentContext.Test.Name + new Random().Next(1000, 9999);
            var auditEvent = new AuditEvent();
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = dp,
                EventType = eventType,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                AuditEvent = auditEvent
            }))
            {
                scope.SetCustomField("value", "added");
            };

            var ev = await dp.GetEventAsync(id, eventType);

            Assert.That(auditEvent.CustomFields["id"].ToString(), Is.EqualTo(id));
            Assert.That(ev.CustomFields["value"].ToString(), Is.EqualTo(auditEvent.CustomFields["value"].ToString()));
            Assert.That(auditEvent.EventType, Is.EqualTo(eventType));
        }
        
        [Test]
        public void GetEventT_ObjectId_CallsCorrectOverload()
        {
            // Arrange
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            var result = provider.GetEvent<AuditEvent>("id");

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public void GetEventT_ValueTuple_CallsCorrectOverload()
        {
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            var result = provider.GetEvent<AuditEvent>(new ValueTuple<string, string>("id", "pk"));

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public void GetEventT_Tuple_CallsCorrectOverload()
        {
            // Arrange
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            var result = provider.GetEvent<AuditEvent>(new Tuple<string, string>("id", "pk"));

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }
        
        [Test]
        public async Task GetEventT_ObjectId_CallsCorrectOverloadAsync()
        {
            // Arrange
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            var result = await provider.GetEventAsync<AuditEvent>("id");

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public async Task GetEventT_ValueTuple_CallsCorrectOverloadAsync()
        {
            // Arrange
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            var result = await provider.GetEventAsync<AuditEvent>(new ValueTuple<string, string>("id", "pk"));

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public async Task GetEventT_Tuple_CallsCorrectOverloadAsync()
        {
            // Arrange
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            var result = await provider.GetEventAsync<AuditEvent>(new Tuple<string, string>("id", "pk"));

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public void GetSetId_Returns_Id_From_IdBuilder_And_Sets_CustomField()
        {
            // Arrange
            var expectedId = "custom-id-123";
            var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object>() };
            var provider = new AzureCosmosDataProvider
            {
                IdBuilder = _ => expectedId
            };

            // Act
            var id = provider.GetSetId(auditEvent);

            // Assert
            Assert.That(id, Is.EqualTo(expectedId));
            Assert.That(auditEvent.CustomFields["id"], Is.EqualTo(expectedId));
        }

        [Test]
        public void GetSetId_Returns_Existing_Id_From_CustomFields()
        {
            // Arrange
            var existingId = "existing-id-456";
            var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object> { ["id"] = existingId } };
            var provider = new AzureCosmosDataProvider();

            // Act
            var id = provider.GetSetId(auditEvent);

            // Assert
            Assert.That(id, Is.EqualTo(existingId));
            Assert.That(auditEvent.CustomFields["id"], Is.EqualTo(existingId));
        }

        [Test]
        public void GetSetId_Generates_New_Id_When_None_Exists()
        {
            // Arrange
            var auditEvent = new AuditEvent { CustomFields = new Dictionary<string, object>() };
            var provider = new AzureCosmosDataProvider();

            // Act
            var id = provider.GetSetId(auditEvent);

            // Assert
            Assert.That(id, Is.Not.Null.And.Not.Empty);
            Assert.That(auditEvent.CustomFields["id"], Is.EqualTo(id));
            Assert.That(id.Length, Is.EqualTo(32));
            Assert.That(Guid.TryParse(id, out _), Is.True);
        }

        [Test]
        public void QueryEvents_Returns()
        {
            // Arrange
            var provider = GetMockedDataProvider(new AuditEvent());

            // Act
            var events = provider.QueryEvents(new QueryRequestOptions())?.ToList();

            // Assert
            Assert.That(events, Is.Not.Null);
            Assert.That(events, Has.Count.EqualTo(1));
        }

        [Test]
        public void QueryEventsGeneric_Returns()
        {
            // Arrange
            var provider = GetMockedDataProvider(new AuditEvent());

            // Act
            var events = provider.QueryEvents<AuditEvent>(new QueryRequestOptions())?.ToList();

            // Assert
            Assert.That(events, Is.Not.Null);
            Assert.That(events, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task EnumerateEvents_Returns()
        {
            // Arrange
            var expected = new AuditEvent();
            var provider = GetMockedDataProvider(expected);

            // Act
            int count = 0;
            AuditEvent lastEvent = null;
            await foreach (var ev in provider.EnumerateEvents("", new QueryRequestOptions()))
            {
                lastEvent = ev;
                count++;
            }

            // Assert
            Assert.That(count, Is.EqualTo(1));
            Assert.That(lastEvent, Is.SameAs(expected));
        }

        private AzureCosmosDataProvider GetMockedDataProvider(AuditEvent expectedAuditEvent)
        {
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockContainer.Setup(c => c.GetItemLinqQueryable<AuditEvent>(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>(), It.IsAny<CosmosLinqSerializerOptions>()))
                .Returns(new List<AuditEvent> { expectedAuditEvent }.AsQueryable().OrderBy(c => c));

            var mockFeedResponse = new Mock<FeedResponse<AuditEvent>>();
            mockFeedResponse.Setup(fr => fr.GetEnumerator())
                .Returns(new List<AuditEvent> { expectedAuditEvent }.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<AuditEvent>>(MockBehavior.Strict);
            mockFeedIterator.SetupSequence(f => f.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(f => f.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object);
            mockContainer.Setup(c => c.GetItemQueryIterator<AuditEvent>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);
            
            mockItemResponse.Setup(i => i.Resource).Returns(expectedAuditEvent);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };

            return provider;
        }
    }
}