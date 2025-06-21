using System;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Firestore.Providers;
using NUnit.Framework;

namespace Audit.Firestore.UnitTest
{
    /// <summary>
    /// Integration tests for FirestoreDataProvider.
    /// These tests require:
    /// 1. A Google Cloud project with Firestore enabled
    /// 2. Either:
    ///    - Application Default Credentials configured (gcloud auth application-default login)
    ///    - GOOGLE_APPLICATION_CREDENTIALS environment variable pointing to a service account key file
    ///    - Running on Google Cloud Platform with appropriate permissions
    /// </summary>
    [TestFixture]
    [Ignore("Integration tests - require Firestore connection")]
    public class FirestoreDataProviderIntegrationTests
    {
        private string _projectId;
        private string _testCollection;

        [SetUp]
        public void Setup()
        {
            // Set your project ID here or via environment variable
            _projectId = Environment.GetEnvironmentVariable("FIRESTORE_PROJECT_ID") ?? "your-project-id";
            _testCollection = $"audit-test-{Guid.NewGuid():N}";
        }

        [TearDown]
        public async Task Cleanup()
        {
            // Clean up test collection if needed
            // Note: This would require admin SDK or manual cleanup
            await Task.CompletedTask;
        }

        [Test]
        public async Task InsertEvent_StoresEventInFirestore()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection));

            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow,
                Environment = new AuditEventEnvironment
                {
                    UserName = "TestUser",
                    MachineName = "TestMachine"
                },
                CustomFields = 
                {
                    ["TestField"] = "TestValue",
                    ["NumericField"] = 123
                }
            };

            // Act
            var eventId = await provider.InsertEventAsync(auditEvent);

            // Assert
            Assert.IsNotNull(eventId);
            Assert.IsNotEmpty(eventId.ToString());

            // Verify we can retrieve it
            var retrievedEvent = await provider.GetEventAsync<AuditEvent>(eventId);
            Assert.IsNotNull(retrievedEvent);
            Assert.AreEqual("TestEvent", retrievedEvent.EventType);
            Assert.AreEqual("TestUser", retrievedEvent.Environment.UserName);
            Assert.AreEqual("TestValue", retrievedEvent.CustomFields["TestField"]);
            Assert.AreEqual(123L, retrievedEvent.CustomFields["NumericField"]); // Firestore stores as long
        }

        [Test]
        public async Task ReplaceEvent_UpdatesExistingEvent()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection));

            var originalEvent = new AuditEvent
            {
                EventType = "OriginalEvent",
                StartDate = DateTime.UtcNow
            };

            var eventId = await provider.InsertEventAsync(originalEvent);

            // Act
            var updatedEvent = new AuditEvent
            {
                EventType = "UpdatedEvent",
                StartDate = originalEvent.StartDate,
                EndDate = DateTime.UtcNow,
                Duration = 1000
            };

            await provider.ReplaceEventAsync(eventId, updatedEvent);

            // Assert
            var retrievedEvent = await provider.GetEventAsync<AuditEvent>(eventId);
            Assert.AreEqual("UpdatedEvent", retrievedEvent.EventType);
            Assert.AreEqual(1000, retrievedEvent.Duration);
        }

        [Test]
        public async Task QueryEvents_ReturnsFilteredResults()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection));

            // Insert test events
            for (int i = 0; i < 5; i++)
            {
                await provider.InsertEventAsync(new AuditEvent
                {
                    EventType = i < 3 ? "TypeA" : "TypeB",
                    StartDate = DateTime.UtcNow.AddMinutes(-i),
                    CustomFields = { ["Index"] = i }
                });
            }

            // Act
            var results = await provider.QueryEventsAsync(query => query
                .WhereEqualTo("EventType", "TypeA")
                .OrderBy("StartDate"));

            // Assert
            Assert.AreEqual(3, results.Count);
            foreach (var evt in results)
            {
                Assert.AreEqual("TypeA", evt.EventType);
            }
        }

        [Test]
        public void TestConnection_SucceedsWithValidCredentials()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection));

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await provider.TestConnectionAsync());
        }

        [Test]
        public async Task DynamicCollectionNames_StoresInCorrectCollections()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(ev => $"{_testCollection}-{ev.EventType.ToLower()}"));

            var loginEvent = new AuditEvent { EventType = "Login" };
            var logoutEvent = new AuditEvent { EventType = "Logout" };

            // Act
            var loginId = await provider.InsertEventAsync(loginEvent);
            var logoutId = await provider.InsertEventAsync(logoutEvent);

            // Assert
            // Events should be stored in different collections
            Assert.IsNotNull(loginId);
            Assert.IsNotNull(logoutId);
            
            // Note: To fully verify they're in different collections, 
            // you'd need to use the Firestore SDK directly to check collection names
        }

        [Test]
        public async Task CustomIdBuilder_UsesSpecifiedIds()
        {
            // Arrange
            var customId = $"custom-{Guid.NewGuid():N}";
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection)
                .IdBuilder(ev => customId));

            var auditEvent = new AuditEvent { EventType = "TestEvent" };

            // Act
            var eventId = await provider.InsertEventAsync(auditEvent);

            // Assert
            Assert.AreEqual(customId, eventId);
            
            var retrievedEvent = await provider.GetEventAsync<AuditEvent>(customId);
            Assert.IsNotNull(retrievedEvent);
        }
    }
} 