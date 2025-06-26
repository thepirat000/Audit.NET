using Audit.Core;
using Audit.Firestore.ConfigurationApi;
using Audit.Firestore.Providers;
using Google.Cloud.Firestore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    /// 3. A collection named "audit-test" in Firestore
    /// 4. A composite index on the "EventType" and "StartDate" fields in the "audit-test" collection
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Firestore")]
    public class FirestoreDataProviderIntegrationTests
    {
        private const string GoogleAppCredentialsVariable = "GOOGLE_APPLICATION_CREDENTIALS";
        private string _projectId;
        private string _testCollection;

        [SetUp]
        public void Setup()
        {
            // Set your project ID here or via environment variable
            _projectId = Environment.GetEnvironmentVariable("FIRESTORE_PROJECT_ID") ?? "audit-net";
            _testCollection = "audit-test";
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection));

            try
            {
                var col = provider.GetFirestoreCollection();

                var documents = col.ListDocumentsAsync();

                await foreach (var document in documents)
                {
                    await document.DeleteAsync();
                }
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
        }

        [Test]
        public void InsertEvent_StoresEventInFirestore()
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
                CustomFields = new Dictionary<string, object>
                {
                    ["TestField"] = "TestValue",
                    ["NumericField"] = 123
                }
            };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.IsNotNull(eventId);
            Assert.IsNotEmpty(eventId.ToString());

            // Verify we can retrieve it
            var retrievedEvent = provider.GetEvent<AuditEvent>(eventId);

            Assert.IsNotNull(retrievedEvent);
            Assert.AreEqual("TestEvent", retrievedEvent.EventType);
            Assert.AreEqual("TestUser", retrievedEvent.Environment.UserName);
            Assert.AreEqual("TestValue", retrievedEvent.CustomFields["TestField"].ToString());
            Assert.AreEqual("123", retrievedEvent.CustomFields["NumericField"].ToString()); // Firestore stores as long
        }

        [Test]
        public async Task InsertEventAsync_StoresEventInFirestore()
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
                CustomFields = new Dictionary<string, object>
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
            Assert.AreEqual("TestValue", retrievedEvent.CustomFields["TestField"].ToString());
            Assert.AreEqual("123", retrievedEvent.CustomFields["NumericField"].ToString()); // Firestore stores as long
        }

        [Test]
        public void ReplaceEvent_UpdatesExistingEvent()
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

            var eventId = provider.InsertEvent(originalEvent);

            // Act
            var updatedEvent = new AuditEvent
            {
                EventType = "UpdatedEvent",
                StartDate = originalEvent.StartDate,
                EndDate = DateTime.UtcNow,
                Duration = 1000
            };

            provider.ReplaceEvent(eventId, updatedEvent);

            // Assert
            var retrievedEvent = provider.GetEvent<AuditEvent>(eventId);
            Assert.AreEqual("UpdatedEvent", retrievedEvent.EventType);
            Assert.AreEqual(1000, retrievedEvent.Duration);
        }

        [Test]
        public async Task ReplaceEventAsync_UpdatesExistingEvent()
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

            var typeA = $"TypeA-{Guid.NewGuid()}";
            var typeB = $"TypeB-{Guid.NewGuid()}";

            // Insert test events
            for (int i = 0; i < 5; i++)
            {
                await provider.InsertEventAsync(new AuditEvent
                {
                    EventType = i < 4 ? typeA : typeB,
                    StartDate = DateTime.UtcNow.AddMinutes(-i),
                    CustomFields = new Dictionary<string, object> { ["Index"] = i }
                });
            }

            // Act
            var results = provider.QueryEventsAsync(query => query
                .WhereEqualTo("EventType", typeA)
                .OrderBy("StartDate")
                .Limit(3));

            // Assert
            int count = 0;
            await foreach (var evt in results)
            {
                count++;
                Assert.That(evt.EventType, Is.EqualTo(typeA));
            }
            Assert.That(count, Is.EqualTo(3)); // Should return 3 events of typeA
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
        public void CustomIdBuilder_UsesSpecifiedIds()
        {
            // Arrange
            var customId = $"custom-{Guid.NewGuid():N}";
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection)
                .IdBuilder(ev => customId));

            var auditEvent = new AuditEvent { EventType = "TestEvent" };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.AreEqual(customId, eventId);
            
            var retrievedEvent = provider.GetEvent<AuditEvent>(customId);
            Assert.IsNotNull(retrievedEvent);
        }

        [Test]
        public async Task CustomIdBuilder_UsesSpecifiedIdsAsync()
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

        [Test]
        public void Configurator_FirestoreDb_CreatedOnlyOnce()
        {
            // Arrange
            var dp = new FirestoreDataProvider()
            {
                ProjectId = "test"
            };

            // Act
            var firestoreDb1 = dp.GetFirestoreDb();
            dp.ProjectId = "changed";
            var firestoreDb2 = dp.GetFirestoreDb();

            // Act
            Assert.That(firestoreDb1, Is.SameAs(firestoreDb2));
            Assert.That(firestoreDb1.ProjectId, Is.EqualTo("test"));
        }

        [Test]
        public void ReplaceEvent_WithNullEventId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config.ProjectId("test-project"));
            var auditEvent = new AuditEvent();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => provider.ReplaceEvent(null, auditEvent));
        }

        [Test]
        public void ReplaceEventAsync_WithNullEventId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config.ProjectId("test-project"));
            var auditEvent = new AuditEvent();

            // Act & Assert
            Assert.ThrowsAsync<NullReferenceException>(async () =>
                await provider.ReplaceEventAsync(null, auditEvent));
        }

        [Test]
        public void GetEvent_WithNullEventId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config.ProjectId("test-project"));

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => provider.GetEvent<AuditEvent>(null));
        }

        [Test]
        public void Configurator_FirestoreDb_SetsInstance()
        {
            // Arrange
            var configurator = new FirestoreProviderConfigurator();
            var builder = new FirestoreDbBuilder()
            {
                ProjectId = "test"
            };
            var firestoreDb = builder.Build();

            // Act
            configurator.FirestoreDb(firestoreDb);

            // Assert
            Assert.AreEqual(firestoreDb, configurator._firestoreDbFactory.Invoke());
        }

        [Test]
        public void Constructor_FirestoreDb_SetsDefaultValues()
        {
            // Arrange 
            var firestoreDb = FirestoreDb.Create("test-project");
            var provider = new FirestoreDataProvider(firestoreDb);

            // Act
            var firestoreDbFromProvider = provider.GetFirestoreDb();

            // Assert
            Assert.That(firestoreDbFromProvider, Is.SameAs(firestoreDb));
        }

        [Test]
        public async Task CredentialsFromFile_StoresEventInFirestore()
        {
            // Arrange
            var filePath = Environment.GetEnvironmentVariable(GoogleAppCredentialsVariable);
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection)
                .CredentialsFromFile(filePath));

            var auditEvent = new AuditEvent { EventType = "TestEvent" };

            // Act
            var eventId = await provider.InsertEventAsync(auditEvent);
            
            // Assert
            Assert.That(eventId, Is.Not.Null);
        }

        [Test]
        public async Task CredentialsFromJson_StoresEventInFirestore()
        {
            // Arrange
            var filePath = Environment.GetEnvironmentVariable(GoogleAppCredentialsVariable);

#if NET462
            var json = System.IO.File.ReadAllText(filePath!);
#else
            var json = await System.IO.File.ReadAllTextAsync(filePath!);
#endif

            var provider = new FirestoreDataProvider(config => config
                .ProjectId(_projectId)
                .Collection(_testCollection)
                .CredentialsFromJson(json));

            var auditEvent = new AuditEvent { EventType = "TestEvent" };

            // Act
            var eventId = await provider.InsertEventAsync(auditEvent);

            // Assert
            Assert.That(eventId, Is.Not.Null);
        }
    }
} 