using Audit.Core;
using Audit.NET.RavenDB.ConfigurationApi;
using Audit.NET.RavenDB.Providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Audit.RavenDB.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("RavenDB")]
    public class RavenDbDataProviderTests
    {
        private const string ravenServerUrl = "http://localhost:8080";
#if IS_TEXT_JSON
        private const string eventType = "SysTxtJson";
#else
        private const string eventType = "Newtonsoft";
#endif

        public class DailyTemperature
        {
            public double LowTemp { get; set; }
            public double HighTemp { get; set; }
        }

        [Test]
        public void Test_RavenDbDataProvider_DefaultAdapter_NoExtData()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            TryCreateDatabase(rdb.DocumentStore, databaseName);

            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            Audit.Core.Configuration.DataProvider = rdb;

            // Act
            var scope = AuditScope.Create(_ => _
                .EventType(eventType));

            scope.Save();

            var id = scope.EventId.ToString();
            var ev = rdb.GetEvent<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
#if IS_TEXT_JSON
            Assert.AreEqual("SysTxtJson", ev.EventType);
#else
            Assert.AreEqual("Newtonsoft", ev.EventType);
#endif
        }


        [Test]
        public void Test_RavenDbDataProvider_DefaultAdapter()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            TryCreateDatabase(rdb.DocumentStore, databaseName);
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            Audit.Core.Configuration.DataProvider = rdb;
            var target = new DailyTemperature() { LowTemp = 20, HighTemp = 100 };

            // Act
            var scope = AuditScope.Create(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new {field = "field value", doc = new {subField = "sub-field value"}}));

            scope.Event.Environment.CustomFields["extra"] = new { prop = "test" };

            target.HighTemp++;
            target.LowTemp++;

            scope.Save();

            var id = scope.EventId.ToString();
            var ev = rdb.GetEvent<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
#if IS_TEXT_JSON
            Assert.AreEqual("sub-field value", ((dynamic)ev.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("test", ((dynamic)ev.Environment.CustomFields["extra"])?["prop"]?.ToString());
#else
            Assert.AreEqual("sub-field value", ((dynamic)ev.CustomFields["doc"])?.subField?.ToString());
            Assert.AreEqual("test", ((dynamic)ev.Environment.CustomFields["extra"])?.prop.ToString());
#endif
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }

        [Test]
        public void Test_RavenDbDataProvider_RavenContractResolver()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .UseDocumentStore(new DocumentStore()
                {
                    Urls = new [] { ravenServerUrl },
                    Database = "AuditTest"
                }));

            TryCreateDatabase(rdb.DocumentStore, databaseName);
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            Audit.Core.Configuration.DataProvider = rdb;
            var target = new DailyTemperature() { LowTemp = 20, HighTemp = 100 };

            // Act
            var scope = AuditScope.Create(_ => _
                .EventType("DefaultRavenResolver")
                .Target(() => target)
                .ExtraFields(new { field = "field value", doc = new { subField = "sub-field value" } }));

            scope.Event.Environment.CustomFields["extra"] = new { prop = "test" };

            target.HighTemp++;
            target.LowTemp++;

            scope.Save();

            var id = scope.EventId.ToString();
            var ev = rdb.GetEvent<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);

            // CustomFields will not be property deserialized when using newtonsoft.json with the default RavenContractResolver (.NET < 5.0) 
#if IS_TEXT_JSON
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
            Assert.AreEqual("sub-field value", ((dynamic)ev.CustomFields["doc"])?.subField?.ToString());
            Assert.AreEqual("test", ((dynamic)ev.Environment.CustomFields["extra"])?.prop.ToString());
#endif
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }

#if IS_TEXT_JSON
        [Test]
        public void Test_RavenDbDataProvider_TextJson_WithNewtonAdapter()
        {
            // Arrange
            var databaseName = "AuditTest";

            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter()
                .UseRavenDB(cfg => cfg
                    .WithSettings(settings => settings
                        .Urls(ravenServerUrl)
                        .DatabaseDefault(databaseName)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var rdb = (RavenDbDataProvider)Configuration.DataProvider;
            TryCreateDatabase(rdb.DocumentStore, databaseName);

            var target = new DailyTemperature() { LowTemp = 20, HighTemp = 100 };

            // Act
            var scope = AuditScope.Create(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new { field = "field value", doc = new { subField = "sub-field value" } }));

            target.HighTemp++;
            target.LowTemp++;

            scope.Save();

            var id = scope.EventId.ToString();
            var ev = rdb.GetEvent<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
            Assert.AreEqual("sub-field value", (ev.CustomFields["doc"] as JToken)?["subField"]?.ToString());
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }
#endif

        [Test]
        public void Test_RavenDbDataProvider_InsertOnStartReplaceOnEnd()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            TryCreateDatabase(rdb.DocumentStore, databaseName);

            Audit.Core.Configuration.Setup()
#if IS_TEXT_JSON
                .JsonNewtonsoftAdapter()
#endif
                .UseCustomProvider(rdb)
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var target = new DailyTemperature() { LowTemp = 20, HighTemp = 100 };

            // Act
            var scope = AuditScope.Create(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new { field = "field value", doc = new { subField = "sub-field value" } }));

            scope.Event.Environment.CustomFields["extra"] = new { prop = "test" };

            target.HighTemp++;
            target.LowTemp++;

            scope.Dispose();

            var id = scope.EventId.ToString();
            var ev = rdb.GetEvent<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
#if IS_TEXT_JSON
            Assert.AreEqual("sub-field value", ((dynamic)ev.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("test", ((dynamic)ev.Environment.CustomFields["extra"])?["prop"]?.ToString());
#else
            Assert.AreEqual("sub-field value", ((dynamic)ev.CustomFields["doc"])?.subField?.ToString());
            Assert.AreEqual("test", ((dynamic)ev.Environment.CustomFields["extra"])?.prop.ToString());
#endif
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }

        [Test]
        public void Test_RavenDbDataProvider_InsertOnStartInsertOnEnd()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            TryCreateDatabase(rdb.DocumentStore, databaseName);

            Audit.Core.Configuration.Setup()
#if IS_TEXT_JSON
                .JsonNewtonsoftAdapter()
#endif
                .UseCustomProvider(rdb)
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var target = new DailyTemperature() { LowTemp = 20, HighTemp = 100 };

            // Act
            var scope = AuditScope.Create(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new { field = "field value", doc = new { subField = "sub-field value" } }));

            scope.Event.Environment.CustomFields["extra"] = new { prop = "test" };

            target.HighTemp++;
            target.LowTemp++;
            var id1 = scope.EventId.ToString();

            scope.Dispose();

            var id2 = scope.EventId.ToString();
            var ev1 = rdb.GetEvent<AuditEvent>(id1);
            var ev2 = rdb.GetEvent<AuditEvent>(id2);

            // Assert
            Assert.IsNotNull(ev1);
            Assert.IsNotNull(ev2);
            Assert.AreEqual("field value", ev1.CustomFields["field"].ToString());
            Assert.AreEqual("field value", ev2.CustomFields["field"].ToString());
            Assert.IsFalse(ev1.Environment.CustomFields.ContainsKey("extra"));
            Assert.IsTrue(ev2.Environment.CustomFields.ContainsKey("extra"));
#if IS_TEXT_JSON
            Assert.AreEqual("sub-field value", ((dynamic)ev1.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("sub-field value", ((dynamic)ev2.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("test", ((dynamic)ev2.Environment.CustomFields["extra"])?["prop"]?.ToString());
#else
            Assert.AreEqual("sub-field value", ((dynamic)ev1.CustomFields["doc"])?.subField?.ToString());
            Assert.AreEqual("sub-field value", ((dynamic)ev2.CustomFields["doc"])?.subField?.ToString());
            Assert.AreEqual("test", ((dynamic)ev2.Environment.CustomFields["extra"])?.prop.ToString());
#endif
            Assert.AreEqual(20, (ev1.Target.Old as DailyTemperature)?.LowTemp);
            Assert.IsNull(ev1.Target.New);
            Assert.AreEqual(20, (ev2.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev2.Target.New as DailyTemperature)?.LowTemp);
        }


        private void TryCreateDatabase(IDocumentStore store, string database)
        {
            try
            {
                store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
            }
        }
    }
}