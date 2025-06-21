using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Firestore.ConfigurationApi;
using Audit.Firestore.Providers;
using Google.Cloud.Firestore;
using Moq;
using NUnit.Framework;

namespace Audit.Firestore.UnitTest
{
    [TestFixture]
    public class FirestoreDataProviderTests
    {
        [SetUp]
        public void Setup()
        {
            // Reset configuration before each test
            Configuration.ResetCustomActions();
        }

        [Test]
        public void Constructor_WithConfig_SetsProperties()
        {
            // Arrange & Act
            var provider = new FirestoreDataProvider(config => config
                .ProjectId("test-project")
                .Database("test-database")
                .Collection("test-collection")
                .CredentialsFromFile("test.json")
                .IdBuilder(ev => $"custom-{ev.EventType}")
                .IgnoreElementNameRestrictions(false));

            // Assert
            Assert.AreEqual("test-project", provider.ProjectId.GetDefault());
            Assert.AreEqual("test-database", provider.Database.GetDefault());
            Assert.AreEqual("test-collection", provider.Collection.GetDefault());
            Assert.AreEqual("test.json", provider.CredentialsFilePath);
            Assert.NotNull(provider.IdBuilder);
            Assert.IsFalse(provider.IgnoreElementNameRestrictions);
        }

        [Test]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Arrange & Act
            var provider = new FirestoreDataProvider();

            // Assert
            Assert.AreEqual("(default)", provider.Database.GetDefault());
            Assert.AreEqual("AuditEvents", provider.Collection.GetDefault());
            Assert.IsTrue(provider.IgnoreElementNameRestrictions);
            Assert.IsNull(provider.IdBuilder);
        }

        [Test]
        public void UseFirestore_ConfiguresGlobalDataProvider()
        {
            // Arrange & Act
            Audit.Core.Configuration.Setup()
                .UseFirestore(config => config
                    .ProjectId("global-project")
                    .Collection("global-collection"));

            // Assert
            var provider = Configuration.DataProvider as FirestoreDataProvider;
            Assert.NotNull(provider);
            Assert.AreEqual("global-project", provider.ProjectId.GetDefault());
            Assert.AreEqual("global-collection", provider.Collection.GetDefault());
        }

        [Test]
        public void UseFirestore_WithSimpleOverload_ConfiguresGlobalDataProvider()
        {
            // Arrange & Act
            Audit.Core.Configuration.Setup()
                .UseFirestore("simple-project", "simple-collection", "simple-database");

            // Assert
            var provider = Configuration.DataProvider as FirestoreDataProvider;
            Assert.NotNull(provider);
            Assert.AreEqual("simple-project", provider.ProjectId.GetDefault());
            Assert.AreEqual("simple-collection", provider.Collection.GetDefault());
            Assert.AreEqual("simple-database", provider.Database.GetDefault());
        }

        [Test]
        public void FieldNameFixer_ReplacesDotsWithUnderscores()
        {
            // Arrange
            var provider = new FirestoreDataProvider();
            var data = new Dictionary<string, object>
            {
                ["field.with.dots"] = "value",
                ["nested"] = new Dictionary<string, object>
                {
                    ["inner.field"] = "inner value"
                },
                ["array"] = new List<object>
                {
                    new Dictionary<string, object> { ["item.field"] = "item value" }
                }
            };

            // Act - Using reflection to test private method
            var fixFieldNamesMethod = typeof(FirestoreDataProvider).GetMethod("FixFieldNames", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = fixFieldNamesMethod.Invoke(provider, new object[] { data }) as Dictionary<string, object>;

            // Assert
            Assert.IsTrue(result.ContainsKey("field_with_dots"));
            Assert.IsFalse(result.ContainsKey("field.with.dots"));
            
            var nested = result["nested"] as Dictionary<string, object>;
            Assert.IsTrue(nested.ContainsKey("inner_field"));
            Assert.IsFalse(nested.ContainsKey("inner.field"));
        }

        [Test]
        public void FieldNameFixer_HandlesReservedPrefixes()
        {
            // Arrange
            var provider = new FirestoreDataProvider();
            var fieldName = "__reserved";

            // Act - Using reflection to test private method
            var fixFieldNameMethod = typeof(FirestoreDataProvider).GetMethod("FixFieldName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = fixFieldNameMethod.Invoke(provider, new object[] { fieldName }) as string;

            // Assert
            Assert.AreEqual("_reserved", result);
        }

        [Test]
        public void GetDocumentId_WithIdBuilder_ReturnsCustomId()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .IdBuilder(ev => $"{ev.EventType}-{ev.StartDate.Ticks}"));
            
            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = new DateTime(2024, 1, 1)
            };

            // Act - Using reflection to test private method
            var getDocumentIdMethod = typeof(FirestoreDataProvider).GetMethod("GetDocumentId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = getDocumentIdMethod.Invoke(provider, new object[] { auditEvent }) as string;

            // Assert
            Assert.AreEqual($"TestEvent-{auditEvent.StartDate.Ticks}", result);
        }

        [Test]
        public void GetDocumentId_WithoutIdBuilder_ReturnsNull()
        {
            // Arrange
            var provider = new FirestoreDataProvider();
            var auditEvent = new AuditEvent { EventType = "TestEvent" };

            // Act - Using reflection to test private method
            var getDocumentIdMethod = typeof(FirestoreDataProvider).GetMethod("GetDocumentId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = getDocumentIdMethod.Invoke(provider, new object[] { auditEvent }) as string;

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Configuration_DynamicValues_EvaluatesCorrectly()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId(ev => $"project-{ev.EventType}")
                .Database(ev => $"db-{ev.EventType}")
                .Collection(ev => $"collection-{ev.EventType}"));

            var auditEvent = new AuditEvent { EventType = "Test" };

            // Assert
            Assert.AreEqual("project-Test", provider.ProjectId.GetValue(auditEvent));
            Assert.AreEqual("db-Test", provider.Database.GetValue(auditEvent));
            Assert.AreEqual("collection-Test", provider.Collection.GetValue(auditEvent));
        }

        [Test]
        public void ReplaceEvent_WithNullEventId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config.ProjectId("test-project"));
            var auditEvent = new AuditEvent();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.ReplaceEvent(null, auditEvent));
        }

        [Test]
        public async Task ReplaceEventAsync_WithNullEventId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config.ProjectId("test-project"));
            var auditEvent = new AuditEvent();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await provider.ReplaceEventAsync(null, auditEvent));
        }

        [Test]
        public void GetEvent_WithNullEventId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config.ProjectId("test-project"));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => provider.GetEvent<AuditEvent>(null));
        }

        [Test]
        public void GetFirestoreDb_WithoutProjectId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider();

            // Act & Assert - Using reflection to test private method
            var getFirestoreDbMethod = typeof(FirestoreDataProvider).GetMethod("GetFirestoreDb", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.Throws<System.Reflection.TargetInvocationException>(() => 
                getFirestoreDbMethod.Invoke(provider, new object[] { null }));
        }

        [Test]
        public void ConvertToFirestoreData_AddsServerTimestamp()
        {
            // Arrange
            var provider = new FirestoreDataProvider();
            var auditEvent = new AuditEvent
            {
                EventType = "Test",
                StartDate = DateTime.UtcNow
            };

            // Act - Using reflection to test private method
            var convertMethod = typeof(FirestoreDataProvider).GetMethod("ConvertToFirestoreData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = convertMethod.Invoke(provider, new object[] { auditEvent }) as Dictionary<string, object>;

            // Assert
            Assert.IsTrue(result.ContainsKey("_timestamp"));
            Assert.IsNotNull(result["_timestamp"]);
        }

        [Test]
        public void Configurator_CredentialsFromFile_ClearsJsonCredentials()
        {
            // Arrange
            var configurator = new FirestoreProviderConfigurator();

            // Act
            configurator.CredentialsFromJson("json-credentials");
            configurator.CredentialsFromFile("file-path");

            // Assert
            Assert.AreEqual("file-path", configurator._credentialsFilePath);
            Assert.IsNull(configurator._credentialsJson);
        }

        [Test]
        public void Configurator_CredentialsFromJson_ClearsFileCredentials()
        {
            // Arrange
            var configurator = new FirestoreProviderConfigurator();

            // Act
            configurator.CredentialsFromFile("file-path");
            configurator.CredentialsFromJson("json-credentials");

            // Assert
            Assert.AreEqual("json-credentials", configurator._credentialsJson);
            Assert.IsNull(configurator._credentialsFilePath);
        }

        [Test]
        public void Configurator_FirestoreDb_ClearsBuilder()
        {
            // Arrange
            var configurator = new FirestoreProviderConfigurator();
            FirestoreDb firestoreDb = null; // Can't mock sealed class

            // Act
            configurator.FirestoreDb(() => firestoreDb);
            configurator.FirestoreDb(firestoreDb);

            // Assert
            Assert.AreEqual(firestoreDb, configurator._firestoreDb);
            Assert.IsNull(configurator._firestoreDbBuilder);
        }

        [Test]
        public void Configurator_FirestoreDbBuilder_ClearsInstance()
        {
            // Arrange
            var configurator = new FirestoreProviderConfigurator();
            FirestoreDb firestoreDb = null; // Can't mock sealed class
            Func<FirestoreDb> builder = () => firestoreDb;

            // Act
            configurator.FirestoreDb(firestoreDb);
            configurator.FirestoreDb(builder);

            // Assert
            Assert.AreEqual(builder, configurator._firestoreDbBuilder);
            Assert.IsNull(configurator._firestoreDb);
        }
    }
} 