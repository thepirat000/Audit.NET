using System.Threading.Tasks;
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
    public class RavenDbDataProviderTests_Async
    {
        private const string ravenServerUrl = "http://localhost:8080";
        private const string eventType = "SysTxtJson";

        public class DailyTemperature
        {
            public double LowTemp { get; set; }
            public double HighTemp { get; set; }
        }

        [Test]
        public async Task Test_RavenDbDataProvider_DefaultAdapter_NoExtData_Async()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            await TryCreateDatabaseAsync(rdb.DocumentStore, databaseName);

            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            Audit.Core.Configuration.DataProvider = rdb;

            // Act
            var scope = await AuditScope.CreateAsync(_ => _
                .EventType(eventType));

            await scope.SaveAsync();

            var id = scope.EventId.ToString();
            var ev = await rdb.GetEventAsync<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("SysTxtJson", ev.EventType);
        }


        [Test]
        public async Task Test_RavenDbDataProvider_DefaultAdapterAsync()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            await TryCreateDatabaseAsync(rdb.DocumentStore, databaseName);
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            Audit.Core.Configuration.DataProvider = rdb;
            var target = new DailyTemperature() {LowTemp = 20, HighTemp = 100};

            // Act
            var scope = await AuditScope.CreateAsync(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new {field = "field value", doc = new {subField = "sub-field value"}}));

            scope.Event.Environment.CustomFields["extra"] = new {prop = "test"};

            target.HighTemp++;
            target.LowTemp++;

            await scope.SaveAsync();

            var id = scope.EventId.ToString();
            var ev = await rdb.GetEventAsync<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
            Assert.AreEqual("sub-field value", ((dynamic) ev.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("test", ((dynamic) ev.Environment.CustomFields["extra"])?["prop"]?.ToString());
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }

        [Test]
        public async Task Test_RavenDbDataProvider_TextJson_WithNewtonAdapterAsync()
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

            var rdb = (RavenDbDataProvider) Configuration.DataProvider;
            await TryCreateDatabaseAsync(rdb.DocumentStore, databaseName);

            var target = new DailyTemperature() {LowTemp = 20, HighTemp = 100};

            // Act
            var scope = await AuditScope.CreateAsync(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new {field = "field value", doc = new {subField = "sub-field value"}}));

            target.HighTemp++;
            target.LowTemp++;

            await scope.SaveAsync();

            var id = scope.EventId.ToString();
            var ev = await rdb.GetEventAsync<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
            Assert.AreEqual("sub-field value", (ev.CustomFields["doc"] as JToken)?["subField"]?.ToString());
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }

        [Test]
        public async Task Test_RavenDbDataProvider_InsertOnStartReplaceOnEndAsync()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            await TryCreateDatabaseAsync(rdb.DocumentStore, databaseName);

            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter()
                .UseCustomProvider(rdb)
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var target = new DailyTemperature() {LowTemp = 20, HighTemp = 100};

            // Act
            var scope = await AuditScope.CreateAsync(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new {field = "field value", doc = new {subField = "sub-field value"}}));

            scope.Event.Environment.CustomFields["extra"] = new {prop = "test"};

            target.HighTemp++;
            target.LowTemp++;

            scope.Dispose();

            var id = scope.EventId.ToString();
            var ev = await rdb.GetEventAsync<AuditEvent>(id);

            // Assert
            Assert.IsNotNull(ev);
            Assert.AreEqual("field value", ev.CustomFields["field"].ToString());
            Assert.AreEqual("sub-field value", ((dynamic) ev.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("test", ((dynamic) ev.Environment.CustomFields["extra"])?["prop"]?.ToString());
            Assert.AreEqual(20, (ev.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev.Target.New as DailyTemperature)?.LowTemp);
        }

        [Test]
        public async Task Test_RavenDbDataProvider_InsertOnStartInsertOnEndAsync()
        {
            // Arrange
            var databaseName = "AuditTest";
            var rdb = new RavenDbDataProvider(cfg => cfg
                .WithSettings(settings => settings
                    .Urls(ravenServerUrl)
                    .DatabaseDefault(databaseName)));

            await TryCreateDatabaseAsync(rdb.DocumentStore, databaseName);

            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter()
                .UseCustomProvider(rdb)
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            var target = new DailyTemperature() {LowTemp = 20, HighTemp = 100};

            // Act
            var scope = await AuditScope.CreateAsync(_ => _
                .EventType(eventType)
                .Target(() => target)
                .ExtraFields(new {field = "field value", doc = new {subField = "sub-field value"}}));

            scope.Event.Environment.CustomFields["extra"] = new {prop = "test"};

            target.HighTemp++;
            target.LowTemp++;
            var id1 = scope.EventId.ToString();

            await scope.DisposeAsync();

            var id2 = scope.EventId.ToString();
            var ev1 = await rdb.GetEventAsync<AuditEvent>(id1);
            var ev2 = await rdb.GetEventAsync<AuditEvent>(id2);

            // Assert
            Assert.IsNotNull(ev1);
            Assert.IsNotNull(ev2);
            Assert.AreEqual("field value", ev1.CustomFields["field"].ToString());
            Assert.AreEqual("field value", ev2.CustomFields["field"].ToString());
            Assert.IsFalse(ev1.Environment.CustomFields.ContainsKey("extra"));
            Assert.IsTrue(ev2.Environment.CustomFields.ContainsKey("extra"));
            Assert.AreEqual("sub-field value", ((dynamic) ev1.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("sub-field value", ((dynamic) ev2.CustomFields["doc"])?["subField"]?.ToString());
            Assert.AreEqual("test", ((dynamic) ev2.Environment.CustomFields["extra"])?["prop"]?.ToString());
            Assert.AreEqual(20, (ev1.Target.Old as DailyTemperature)?.LowTemp);
            Assert.IsNull(ev1.Target.New);
            Assert.AreEqual(20, (ev2.Target.Old as DailyTemperature)?.LowTemp);
            Assert.AreEqual(21, (ev2.Target.New as DailyTemperature)?.LowTemp);
        }


        private async Task TryCreateDatabaseAsync(IDocumentStore store, string database)
        {
            try
            {
                await store.Maintenance.ForDatabase(database).SendAsync(new GetStatisticsOperation());
            }
            catch (DatabaseDoesNotExistException)
            {
                await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(database)));
            }
        }
    }
}