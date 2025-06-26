using Audit.Core;
using Audit.Firestore.ConfigurationApi;
using Audit.Firestore.Providers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
                .SanitizeFieldNames(false));

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(provider.ProjectId, Is.EqualTo("test-project"));
                Assert.That(provider.Database, Is.EqualTo("test-database"));
                Assert.That(provider.Collection.GetDefault(), Is.EqualTo("test-collection"));
                Assert.That(provider.CredentialsFilePath, Is.EqualTo("test.json"));
                Assert.That(provider.IdBuilder, Is.Not.Null);
                Assert.That(provider.SanitizeFieldNames, Is.False);
            });
        }

        [Test]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Arrange & Act
            var provider = new FirestoreDataProvider();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(provider, Is.Not.Null);
                Assert.That(provider.Database, Is.EqualTo("(default)"));
                Assert.That(provider.Collection.GetDefault(), Is.EqualTo("AuditEvents"));
                Assert.That(provider.SanitizeFieldNames, Is.False);
                Assert.That(provider.IdBuilder, Is.Null);

            });
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
            Assert.Multiple(() =>
            {
                Assert.That(provider, Is.Not.Null);
                Assert.That(provider.ProjectId, Is.EqualTo("global-project"));
                Assert.That(provider.Collection.GetDefault(), Is.EqualTo("global-collection"));
            });
        }

        [Test]
        public void UseFirestore_WithSimpleOverload_ConfiguresGlobalDataProvider()
        {
            // Arrange & Act
            Audit.Core.Configuration.Setup()
                .UseFirestore("simple-project", "simple-collection", "simple-database");

            // Assert
            var provider = Configuration.DataProvider as FirestoreDataProvider;
            Assert.Multiple(() =>
            {
                Assert.That(provider, Is.Not.Null);
                Assert.That(provider.ProjectId, Is.EqualTo("simple-project"));
                Assert.That(provider.Collection.GetDefault(), Is.EqualTo("simple-collection"));
                Assert.That(provider.Database, Is.EqualTo("simple-database"));
            });
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

            // Act
            var result = provider.FixFieldNames(data);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.ContainsKey("field_with_dots"), Is.True);
                Assert.That(result.ContainsKey("field.with.dots"), Is.False);

                var nested = result["nested"] as Dictionary<string, object>;
                Assert.That(nested, Is.Not.Null);
                Assert.That(nested.ContainsKey("inner_field"), Is.True);
                Assert.That(nested.ContainsKey("inner.field"), Is.False);
            });
        }

        [Test]
        public void FieldNameFixer_HandlesReservedPrefixes()
        {
            // Arrange
            var fieldName = "__reserved";

            // Act
            var result = FirestoreDataProvider.FixFieldName(fieldName);

            // Assert
            Assert.That(result, Is.EqualTo("_reserved"));
        }

        [Test]
        public void FieldNameFixer_HandlesEmpty()
        {
            // Arrange
            var fieldName = string.Empty;

            // Act
            var result = FirestoreDataProvider.FixFieldName(fieldName);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
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

            // Act
            var result = provider.GetDocumentId(auditEvent);

            // Assert
            Assert.That(result, Is.EqualTo($"TestEvent-{auditEvent.StartDate.Ticks}"));
        }

        [Test]
        public void GetDocumentId_WithoutIdBuilder_ReturnsNull()
        {
            // Arrange
            var provider = new FirestoreDataProvider();
            var auditEvent = new AuditEvent { EventType = "TestEvent" };

            // Act
            var result = provider.GetDocumentId(auditEvent);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Configuration_DynamicValues_EvaluatesCorrectly()
        {
            // Arrange
            var provider = new FirestoreDataProvider(config => config
                .ProjectId("project-Test")
                .Database("db-Test")
                .Collection(ev => $"collection-{ev.EventType}"));

            var auditEvent = new AuditEvent { EventType = "Test" };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(provider.ProjectId, Is.EqualTo("project-Test"));
                Assert.That(provider.Database, Is.EqualTo("db-Test"));
                Assert.That(provider.Collection.GetValue(auditEvent), Is.EqualTo("collection-Test"));
            });
        }
        
        [Test]
        public void GetFirestoreDb_WithoutProjectId_ThrowsException()
        {
            // Arrange
            var provider = new FirestoreDataProvider();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => provider.GetFirestoreDb());
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

            // Act
            var result = provider.ConvertToFirestoreData(auditEvent);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.ContainsKey("_timestamp"), Is.True);
                Assert.That(result["_timestamp"], Is.Not.Null);
            });
        }

        [Test]
        public void ConvertToFirestoreData_FixFieldNames_AddsServerTimestamp()
        {
            // Arrange
            var provider = new FirestoreDataProvider()
            {
                SanitizeFieldNames = true
            };
            var auditEvent = new AuditEvent
            {
                EventType = "Test",
                StartDate = DateTime.UtcNow,
                CustomFields = new Dictionary<string, object>()
                {
                    ["__test"] = "value"
                }
            };

            // Act
            var result = provider.ConvertToFirestoreData(auditEvent);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.ContainsKey("_test"), Is.True);
                Assert.That(result["_test"], Is.EqualTo("value"));
                Assert.That(result.ContainsKey("_timestamp"), Is.True);
                Assert.That(result["_timestamp"], Is.Not.Null);
            });
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
            Assert.Multiple(() =>
            {
                Assert.That(configurator._credentialsFilePath, Is.EqualTo("file-path"));
                Assert.That(configurator._credentialsJson, Is.Null);
            });
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
            Assert.Multiple(() =>
            {
                Assert.That(configurator._credentialsJson, Is.EqualTo("json-credentials"));
                Assert.That(configurator._credentialsFilePath, Is.Null);
            });
        }

        [Test]
        public void SanitizeDictionary_Returns_Null()
        {
            // Arrange
            var provider = new FirestoreDataProvider();

            // Act
            var result = provider.SanitizeDictionary(null);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void SanitizeValue_JsonElement_Object_ReturnsDictionary()
        {
            var provider = new FirestoreDataProvider();
            var json = "{\"a\":1,\"b\":{\"c\":2}}";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element) as Dictionary<string, object>;

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result["a"], Is.EqualTo(1));
                Assert.That(result["b"], Is.TypeOf<Dictionary<string, object>>());
                Assert.That(((Dictionary<string, object>)result["b"])["c"], Is.EqualTo(2));
            });
        }

        [Test]
        public void SanitizeValue_JsonElement_Array_ReturnsList()
        {
            var provider = new FirestoreDataProvider();
            var json = "[1, {\"z\": 4}]";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element) as List<object>;

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result[0], Is.EqualTo(1));
                Assert.That(result[1], Is.TypeOf<Dictionary<string, object>>());
                Assert.That(((Dictionary<string, object>)result[1])["z"], Is.EqualTo(4));
            });
        }

        [Test]
        public void SanitizeValue_JsonElement_String_ReturnsString()
        {
            var provider = new FirestoreDataProvider();
            var json = "\"abc\"";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element);

            Assert.That(result, Is.EqualTo("abc"));
        }

        [Test]
        public void SanitizeValue_JsonElement_Number_ReturnsNumber()
        {
            var provider = new FirestoreDataProvider();
            var json = "42";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void SanitizeValue_JsonElement_Number_Double_ReturnsDouble()
        {
            var provider = new FirestoreDataProvider();
            var json = "42.5";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element);

            Assert.That(result, Is.EqualTo(42.5));
        }

        [Test]
        public void SanitizeValue_JsonElement_True_ReturnsTrue()
        {
            var provider = new FirestoreDataProvider();
            var json = "true";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element);

            Assert.That(result, Is.EqualTo(true));
        }

        [Test]
        public void SanitizeValue_JsonElement_False_ReturnsFalse()
        {
            var provider = new FirestoreDataProvider();
            var json = "false";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element);

            Assert.That(result, Is.EqualTo(false));
        }

        [Test]
        public void SanitizeValue_JsonElement_Null_ReturnsNull()
        {
            var provider = new FirestoreDataProvider();
            var json = "null";
            using var doc = JsonDocument.Parse(json);
            var element = doc.RootElement;

            var result = provider.SanitizeValue(element);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FixFieldNamesInList_Handles_Dictionaries_And_Primitives()
        {
            var provider = new FirestoreDataProvider();
            var input = new List<object>
            {
                new Dictionary<string, object> { ["a.b"] = 1, ["__c"] = 2 },
                42,
                "test"
            };

            var result = provider.FixFieldNamesInList(input);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(3));

                // First item: dictionary with fixed keys
                var dict = result[0] as Dictionary<string, object>;
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict.ContainsKey("a_b"), Is.True);
                Assert.That(dict.ContainsKey("_c"), Is.True);
                Assert.That(dict["a_b"], Is.EqualTo(1));
                Assert.That(dict["_c"], Is.EqualTo(2));

                // Second item: primitive
                Assert.That(result[1], Is.EqualTo(42));

                // Third item: primitive
                Assert.That(result[2], Is.EqualTo("test"));
            });
        }

        [Test]
        public void FixFieldNamesInList_Handles_Nested_Lists()
        {
            var provider = new FirestoreDataProvider();
            var input = new List<object>
            {
                new List<object>
                {
                    new Dictionary<string, object> { ["x.y"] = 5 }
                }
            };

            var result = provider.FixFieldNamesInList(input);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));

                var nestedList = result[0] as List<object>;
                Assert.That(nestedList, Is.Not.Null);
                Assert.That(nestedList.Count, Is.EqualTo(1));

                var dict = nestedList[0] as Dictionary<string, object>;
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict.ContainsKey("x_y"), Is.True);
                Assert.That(dict["x_y"], Is.EqualTo(5));
            });
        }

        [Test]
        public void FixFieldNamesInList_Handles_Arrays()
        {
            var provider = new FirestoreDataProvider();
            var input = new List<object>
            {
                new object[]
                {
                    new Dictionary<string, object> { ["foo.bar"] = 7 }
                }
            };

            var result = provider.FixFieldNamesInList(input);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(1));

                var nestedList = result[0] as List<object>;
                Assert.That(nestedList, Is.Not.Null);
                Assert.That(nestedList.Count, Is.EqualTo(1));

                var dict = nestedList[0] as Dictionary<string, object>;
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict.ContainsKey("foo_bar"), Is.True);
                Assert.That(dict["foo_bar"], Is.EqualTo(7));
            });
        }

        [Test]
        public void FixFieldNamesInList_Leaves_NonCollections_Unchanged()
        {
            var provider = new FirestoreDataProvider();
            var input = new List<object> { 1, "abc", true, null };

            var result = provider.FixFieldNamesInList(input);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(4));
                Assert.That(result[0], Is.EqualTo(1));
                Assert.That(result[1], Is.EqualTo("abc"));
                Assert.That(result[2], Is.EqualTo(true));
                Assert.That(result[3], Is.Null);
            });
        }
    }
} 