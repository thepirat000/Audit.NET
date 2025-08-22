using Audit.AzureCosmos.Providers;
using Audit.Core;
using Audit.IntegrationTest;

using Microsoft.Azure.Cosmos;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace Audit.AzureCosmos.UnitTest
{

    [TestFixture]
#if NET462
    [Category(TestCommon.Category.MonoIncompatible)]
#endif
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
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockItemResponse.Setup(i => i.Resource).Returns(expected);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };
            
            // Act
            var result = provider.GetEvent<AuditEvent>("id");

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }

        [Test]
        public void GetEventT_ValueTuple_CallsCorrectOverload()
        {
            // Arrange
            var expected = new AuditEvent();
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockItemResponse.Setup(i => i.Resource).Returns(expected);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };

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
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockItemResponse.Setup(i => i.Resource).Returns(expected);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };

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
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockItemResponse.Setup(i => i.Resource).Returns(expected);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };

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
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockItemResponse.Setup(i => i.Resource).Returns(expected);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };

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
            var mockClient = new Mock<CosmosClient>(MockBehavior.Strict);
            var mockContainer = new Mock<Container>(MockBehavior.Strict);
            var mockItemResponse = new Mock<ItemResponse<AuditEvent>>(MockBehavior.Strict);

            mockClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(mockContainer.Object);

            mockContainer.Setup(c => c.ReadItemAsync<AuditEvent>(It.IsAny<string>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockItemResponse.Object);

            mockItemResponse.Setup(i => i.Resource).Returns(expected);

            var provider = new AzureCosmosDataProvider()
            {
                CosmosClient = mockClient.Object
            };

            // Act
            var result = await provider.GetEventAsync<AuditEvent>(new Tuple<string, string>("id", "pk"));

            // Assert
            Assert.That(result, Is.SameAs(expected));
        }
    }
}