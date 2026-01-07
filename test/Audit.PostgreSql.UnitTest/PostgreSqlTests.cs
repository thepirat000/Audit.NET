using Audit.Core;
using Audit.IntegrationTest;
using Audit.PostgreSql.Configuration;
using Audit.PostgreSql.Providers;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.PostgreSql.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.PostgreSql)]
    public class PostgreSqlTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void PostgreSqlDataProvider_NoJsonColumn_ShouldInsertCustomColumns()
        {
            var efPostgresSqlDataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = TestCommon.PostgreSqlConnectionString,
                TableName = "event",
                IdColumnName = "id",
                CustomColumns =
                [
                    new(){Name = "event_type", Value = ev => ev.EventType}
                ],
                LastUpdatedDateColumnName = new(),
                DataJsonType = null,
                DataJsonStringBuilder = null
            };

            var eventType = "TEST_123";
            var ev = new AuditEvent() { EventType = eventType };
            var eventId = efPostgresSqlDataProvider.InsertEvent(ev);

            Assert.That(eventId, Is.Positive);

            using var cnn = efPostgresSqlDataProvider.GetDbConnection(ev);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = $@"select event_type from ""event"" WHERE ""id"" = @id";
            var idParameter = cmd.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.Value = eventId;
            cmd.Parameters.Add(idParameter);

            var eventTypeFromDb = cmd.ExecuteScalar();

            cmd = cnn.CreateCommand();
            cmd.CommandText = $@"select data from ""event"" WHERE ""id"" = {eventId}";
            var dataFromDb = cmd.ExecuteScalar();

            Assert.That(eventTypeFromDb, Is.EqualTo(eventType));
            Assert.That(dataFromDb, Is.EqualTo(DBNull.Value));

            cmd = cnn.CreateCommand();
            cmd.CommandText = $@"delete from ""event"" WHERE ""id"" = {eventId}";
            cmd.ExecuteNonQuery();
        }

        [Test]
        public void PostgreSqlDataProviderFluent_NoJsonColumn_ShouldInsertCustomColumns()
        {
            var efPostgresSqlDataProvider = new PostgreSqlDataProvider(c => c
                    .ConnectionString(TestCommon.PostgreSqlConnectionString)
                    .TableName("event")
                    .IdColumnName("id")
                    .CustomColumn("event_type", ev => ev.EventType)
                    .DataJsonColumn((string)null));

            var eventType = "TEST_321";
            var ev = new AuditEvent() { EventType = eventType };
            var eventId = efPostgresSqlDataProvider.InsertEvent(ev);

            Assert.That(eventId, Is.Positive);

            using var cnn = efPostgresSqlDataProvider.GetDbConnection(ev);
            cnn.Open();
            var cmd = cnn.CreateCommand();
            cmd.CommandText = $@"select event_type from ""event"" WHERE ""id"" = @id";
            var idParameter = cmd.CreateParameter();
            idParameter.ParameterName = "id";
            idParameter.Value = eventId;
            cmd.Parameters.Add(idParameter);

            var eventTypeFromDb = cmd.ExecuteScalar();

            cmd = cnn.CreateCommand();
            cmd.CommandText = $@"select data from ""event"" WHERE ""id"" = {eventId}";
            var dataFromDb = cmd.ExecuteScalar();

            Assert.That(eventTypeFromDb, Is.EqualTo(eventType));
            Assert.That(dataFromDb, Is.EqualTo(DBNull.Value));

            cmd = cnn.CreateCommand();
            cmd.CommandText = $@"delete from ""event"" WHERE ""id"" = {eventId}";
            cmd.ExecuteNonQuery();
        }

        [Test]
        public void PostgreSqlDataProvider_NoJsonColumn_GetEventShouldGetNoEvent()
        {
            var efPostgresSqlDataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = TestCommon.PostgreSqlConnectionString,
                TableName = "event",
                IdColumnName = "id",
                CustomColumns =
                [
                    new(){Name = "event_type", Value = ev => ev.EventType}
                ],
                LastUpdatedDateColumnName = new(),
                DataJsonType = null,
                DataJsonStringBuilder = null
            };

            var eventType = "TEST_123";
            var ev = new AuditEvent() { EventType = eventType };
            var eventId = efPostgresSqlDataProvider.InsertEvent(ev);

            Assert.That(eventId, Is.Positive);

            var eventFromDb = efPostgresSqlDataProvider.GetEvent(eventId);

            Assert.That(eventFromDb, Is.Null);
        }

        [Test]
        public async Task PostgreSqlDataProvider_NoJsonColumn_GetEventShouldGetNoEventAsync()
        {
            var efPostgresSqlDataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = TestCommon.PostgreSqlConnectionString,
                TableName = "event",
                IdColumnName = "id",
                CustomColumns =
                [
                    new(){Name = "event_type", Value = ev => ev.EventType}
                ],
                LastUpdatedDateColumnName = new(),
                DataJsonType = null,
                DataJsonStringBuilder = null
            };

            var eventType = "TEST_123";
            var ev = new AuditEvent() { EventType = eventType };
            var eventId = await efPostgresSqlDataProvider.InsertEventAsync(ev);

            Assert.That(eventId, Is.Positive);

            var eventFromDb = await efPostgresSqlDataProvider.GetEventAsync(eventId);

            Assert.That(eventFromDb, Is.Null);
        }

        [Test]
        public void PostgreSqlDataProvider_NoJsonColumn_EnumerateEventShouldGetNoEvents()
        {
            var efPostgresSqlDataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = TestCommon.PostgreSqlConnectionString,
                TableName = "event",
                IdColumnName = "id",
                CustomColumns =
                [
                    new(){Name = "event_type", Value = ev => ev.EventType}
                ],
                LastUpdatedDateColumnName = new(),
                DataJsonType = null,
                DataJsonStringBuilder = null
            };

            var eventType = "TEST_123";
            var ev = new AuditEvent() { EventType = eventType };
            var eventId = efPostgresSqlDataProvider.InsertEvent(ev);

            Assert.That(eventId, Is.Positive);

            var eventsFromDb = efPostgresSqlDataProvider.EnumerateEvents(string.Empty);

            Assert.That(eventsFromDb, Is.Empty);
        }

        [Test]
        public void PostgreSqlDataProvider_NoJsonColumn_EnumerateEventOverloadShouldGetNoEvents()
        {
            var efPostgresSqlDataProvider = new PostgreSqlDataProvider()
            {
                ConnectionString = TestCommon.PostgreSqlConnectionString,
                TableName = "event",
                IdColumnName = "id",
                CustomColumns =
                [
                    new(){Name = "event_type", Value = ev => ev.EventType}
                ],
                LastUpdatedDateColumnName = new(),
                DataJsonType = null,
                DataJsonStringBuilder = null
            };

            var eventType = "TEST_123";
            var ev = new AuditEvent() { EventType = eventType };
            var eventId = efPostgresSqlDataProvider.InsertEvent(ev);

            Assert.That(eventId, Is.Positive);

            var eventsFromDb = efPostgresSqlDataProvider.EnumerateEvents<AuditEvent>(string.Empty, string.Empty, string.Empty);

            Assert.That(eventsFromDb, Is.Empty);
        }

        [Test]
        public void PostgreSqlDataProvider_CustomColumn_Constructor()
        {
            var column = new CustomColumn();

            Assert.That(column.Name, Is.Null);
        }

        [Test]
        public void PostgreSqlDataProvider_Constructor()
        {
            var dp = new PostgreSqlDataProvider();

            Assert.That(dp.DataJsonType, Is.EqualTo("JSON"));
        }

        [Test]
        public void PostgreSqlConfigurator_Extension()
        {
            Audit.Core.Configuration.Setup().UsePostgreSql(cfg => cfg.ConnectionString("test"));

            Assert.That(Audit.Core.Configuration.DataProvider, Is.TypeOf<PostgreSqlDataProvider>());
        }

        [Test]
        public void Test_PostgreDataProvider_FluentApi_DataColumnType()
        {
            var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                .ConnectionString("c")
                .DataJsonColumn("dc", DataType.JSON, null)
                .IdColumnName("id")
                .LastUpdatedColumnName("lud")
                .Schema("sc")
                .TableName("t")
                .CustomColumn("c1", ev => 1)
                .CustomColumn("c2", ev => 2));
            Assert.That(x.ConnectionString.GetValue(null), Is.EqualTo("c"));
            Assert.That(x.DataJsonColumnName.GetValue(null), Is.EqualTo("dc"));
            Assert.That(x.IdColumnName.GetValue(null), Is.EqualTo("id"));
            Assert.That(x.LastUpdatedDateColumnName.GetValue(null), Is.EqualTo("lud"));
            Assert.That(x.Schema.GetValue(null), Is.EqualTo("sc"));
            Assert.That(x.TableName.GetValue(null), Is.EqualTo("t"));
            Assert.That(x.CustomColumns.Count, Is.EqualTo(2));
            Assert.That(x.CustomColumns[0].Name, Is.EqualTo("c1"));
            Assert.That(x.CustomColumns[0].Value.Invoke(null), Is.EqualTo(1));
            Assert.That(x.CustomColumns[1].Name, Is.EqualTo("c2"));
            Assert.That(x.CustomColumns[1].Value.Invoke(null), Is.EqualTo(2));
        }

        [Test]
        public void Test_PostgreDataProvider_FluentApi()
        {
            var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                .ConnectionString("c")
                .DataJsonColumn("dc")
                .IdColumnName("id")
                .LastUpdatedColumnName("lud")
                .Schema("sc")
                .TableName("t")
                .CustomColumn("c1", ev => 1)
                .CustomColumn("c2", ev => 2));
            Assert.That(x.ConnectionString.GetValue(null), Is.EqualTo("c"));
            Assert.That(x.DataJsonColumnName.GetValue(null), Is.EqualTo("dc"));
            Assert.That(x.IdColumnName.GetValue(null), Is.EqualTo("id"));
            Assert.That(x.LastUpdatedDateColumnName.GetValue(null), Is.EqualTo("lud"));
            Assert.That(x.Schema.GetValue(null), Is.EqualTo("sc"));
            Assert.That(x.TableName.GetValue(null), Is.EqualTo("t"));
            Assert.That(x.CustomColumns.Count, Is.EqualTo(2));
            Assert.That(x.CustomColumns[0].Name, Is.EqualTo("c1"));
            Assert.That(x.CustomColumns[0].Value.Invoke(null), Is.EqualTo(1));
            Assert.That(x.CustomColumns[1].Name, Is.EqualTo("c2"));
            Assert.That(x.CustomColumns[1].Value.Invoke(null), Is.EqualTo(2));
        }

        [Test]
        public void Test_PostgreDataProvider_FluentApiBuilder_DataColumnType()
        {
            var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                .ConnectionString(ev => "c")
                .DataJsonColumn(ev => "dc", DataType.JSON, null)
                .IdColumnName(ev => "id")
                .LastUpdatedColumnName(ev => "lud")
                .Schema(ev => "sc")
                .TableName(ev => "t")
                .CustomColumn("c1", ev => 1)
                .CustomColumn("c2", ev => 2));
            Assert.That(x.ConnectionString.GetDefault(), Is.EqualTo("c"));
            Assert.That(x.DataJsonColumnName.GetDefault(), Is.EqualTo("dc"));
            Assert.That(x.IdColumnName.GetDefault(), Is.EqualTo("id"));
            Assert.That(x.LastUpdatedDateColumnName.GetDefault(), Is.EqualTo("lud"));
            Assert.That(x.Schema.GetDefault(), Is.EqualTo("sc"));
            Assert.That(x.TableName.GetDefault(), Is.EqualTo("t"));
            Assert.That(x.CustomColumns.Count, Is.EqualTo(2));
            Assert.That(x.CustomColumns[0].Name, Is.EqualTo("c1"));
            Assert.That(x.CustomColumns[0].Value.Invoke(null), Is.EqualTo(1));
            Assert.That(x.CustomColumns[1].Name, Is.EqualTo("c2"));
            Assert.That(x.CustomColumns[1].Value.Invoke(null), Is.EqualTo(2));
        }

        [Test]
        public void Test_PostgreDataProvider_FluentApiBuilder()
        {
            var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                .ConnectionString(ev => "c")
                .DataJsonColumn(ev => "dc")
                .IdColumnName(ev => "id")
                .LastUpdatedColumnName(ev => "lud")
                .Schema(ev => "sc")
                .TableName(ev => "t")
                .CustomColumn("c1", ev => 1)
                .CustomColumn("c2", ev => 2));
            Assert.That(x.ConnectionString.GetDefault(), Is.EqualTo("c"));
            Assert.That(x.DataJsonColumnName.GetDefault(), Is.EqualTo("dc"));
            Assert.That(x.IdColumnName.GetDefault(), Is.EqualTo("id"));
            Assert.That(x.LastUpdatedDateColumnName.GetDefault(), Is.EqualTo("lud"));
            Assert.That(x.Schema.GetDefault(), Is.EqualTo("sc"));
            Assert.That(x.TableName.GetDefault(), Is.EqualTo("t"));
            Assert.That(x.CustomColumns.Count, Is.EqualTo(2));
            Assert.That(x.CustomColumns[0].Name, Is.EqualTo("c1"));
            Assert.That(x.CustomColumns[0].Value.Invoke(null), Is.EqualTo(1));
            Assert.That(x.CustomColumns[1].Name, Is.EqualTo("c2"));
            Assert.That(x.CustomColumns[1].Value.Invoke(null), Is.EqualTo(2));
        }

        [Test]
        public void Test_PostgreDataProvider_CustomDataColumn()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);
            
            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var id = scope.EventId;

            var getEvent = dp?.GetEvent<AuditEvent>(id);

            Assert.That(getEvent?.EventType, Is.EqualTo(overrideEventType));
        }

        [Test]
        public void Test_PostgreDataProvider_GetEventNotFound()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            object id = -555;

            var getEvent = dp?.GetEvent<AuditEvent>(id);

            Assert.That(getEvent, Is.Null);
        }

        [Test]
        public async Task Test_PostgreDataProvider_GetEventNotFoundAsync()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            object id = -555;

            var getEvent = await dp.GetEventAsync<AuditEvent>(id);

            Assert.That(getEvent, Is.Null);
        }

        [Test]
        public void Test_PostgreDataProvider_Paging_No_Where()
        {
            const int pageNumberOne = 1;
            const int pageSize = 10;

            var dp = GetConfiguredPostgreSqlDataProvider("test");

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var events = dp?.EnumerateEvents(pageNumberOne, pageSize);
            var realizedEvents = events?.ToList();
            Assert.That(realizedEvents, Is.Not.Null);
            Assert.That(realizedEvents, Has.Count.GreaterThan(0));
        }
        
        [Test]
        public void Test_PostgreDataProvider_Paging_OutOfRange_Inputs()
        {
            /* define negative values.  the code should "safety-ize" them to page-number=1 and page-size=1 */
            const int pageNumberOne = -333;
            const int pageSize = -444;

            var dp = GetConfiguredPostgreSqlDataProvider("test");

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var events = dp?.EnumerateEvents(pageNumberOne, pageSize);
            var realizedEvents = events?.ToList();
            Assert.That(realizedEvents, Is.Not.Null);
            Assert.That(realizedEvents, Has.Count.GreaterThan(0));
        }        
        
        [Test]
        public void Test_PostgreDataProvider_Paging_With_Where()
        {
            const int pageNumber = 3;
            const int pageSize = 10;

            string whereExpression = @""""+ GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900'";
            var dp = GetConfiguredPostgreSqlDataProvider("test");

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            IEnumerable<AuditEvent> events = dp?.EnumerateEvents(pageNumber, pageSize, whereExpression);
            ICollection<AuditEvent> realizedEvents = events?.ToList();
            Assert.That(realizedEvents, Is.Not.Null);
            Assert.That(realizedEvents, Has.Count.GreaterThan(0));
        }
        
        [Test]
        public void Test_EnumerateEvents_WhereExpression()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var whereExpression = @"""" + GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900' AND ""data"" IS NOT NULL";
            var events = dp?.EnumerateEvents(whereExpression);
            var ev = events?.LastOrDefault();

            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Test_EnumerateEvents_WhereEmptyExpression()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var events = dp?.EnumerateEvents(string.Empty);
            var ev = events?.LastOrDefault();

            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Test_EnumerateEventsGeneric_WhereEmptyExpression()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var whereExpression = string.Empty;
            var events = dp?.EnumerateEvents<AuditEvent>(whereExpression, GetLastUpdatedColumnNameColumnName(), "1");
            var ev = events?.LastOrDefault();

            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Test_EnumerateEvents_WhereSortByExpression()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            var scope = AuditScope.Create(new AuditScopeOptions { EventType = "test", DataProvider = dp });
            scope.Dispose();

            var whereExpression = @"""" + GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900'";
            var events = dp?.EnumerateEvents<AuditEvent>(whereExpression, GetLastUpdatedColumnNameColumnName(), "1");
            var ev = events?.LastOrDefault();

            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Test_AuditEventData_AsText()
        {
            // Arrange
            var dp = new CustomPostgreSqlDataProvider(config => config
                .ConnectionString(TestCommon.PostgreSqlConnectionString)
                .TableName("event_text")
                .IdColumnName("id")
                .DataJsonColumn("data", Configuration.DataType.String)
                .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                .CustomColumn("event_type", ev => ev.EventType)
                .CustomColumn("some_date", ev => DateTime.UtcNow)
                .CustomColumn("some_null", ev => null));
            
            var auditEvent = new AuditEvent()
            {
                EventType = "EventType",
                CustomFields = new Dictionary<string, object>()
                {
                    {"Field1", "Value1"}
                }
            };

            // Act
            var id = (long)dp.InsertEvent(auditEvent);
            var auditEventLoaded = dp.GetEvent<AuditEvent>(id);

            // Assert
            Assert.That(id, Is.TypeOf<long>());
            Assert.That(auditEventLoaded, Is.Not.Null);
            Assert.That(auditEventLoaded.EventType, Is.EqualTo("EventType"));
            Assert.That(auditEventLoaded.CustomFields, Is.Not.Null);
            Assert.That(auditEventLoaded.CustomFields.Count, Is.EqualTo(1));
            Assert.That(auditEventLoaded.CustomFields["Field1"].ToString(), Is.EqualTo("Value1"));
        }

        [Test]
        public async Task Test_AuditEventData_AsTextAsync()
        {
            // Arrange
            var dp = new CustomPostgreSqlDataProvider(config => config
                .ConnectionString(TestCommon.PostgreSqlConnectionString)
                .TableName("event_text")
                .IdColumnName("id")
                .DataJsonColumn("data", Configuration.DataType.String)
                .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                .CustomColumn("event_type", ev => ev.EventType)
                .CustomColumn("some_date", ev => DateTime.UtcNow)
                .CustomColumn("some_null", ev => null));

            var auditEvent = new AuditEvent()
            {
                EventType = "EventType",
                CustomFields = new Dictionary<string, object>()
                {
                    {"Field1", "Value1"}
                }
            };

            // Act
            var id = (long) await dp.InsertEventAsync(auditEvent);
            var auditEventLoaded = await dp.GetEventAsync<AuditEvent>(id);

            // Assert
            Assert.That(id, Is.TypeOf<long>());
            Assert.That(auditEventLoaded, Is.Not.Null);
            Assert.That(auditEventLoaded.EventType, Is.EqualTo("EventType"));
            Assert.That(auditEventLoaded.CustomFields, Is.Not.Null);
            Assert.That(auditEventLoaded.CustomFields.Count, Is.EqualTo(1));
            Assert.That(auditEventLoaded.CustomFields["Field1"].ToString(), Is.EqualTo("Value1"));
        }

        [Test]
        public void Test_AuditEventData_Replace()
        {
            // Arrange
            var dp = new CustomPostgreSqlDataProvider(config => config
                .ConnectionString(TestCommon.PostgreSqlConnectionString)
                .TableName("event_text")
                .IdColumnName("id")
                .DataJsonColumn("data", Configuration.DataType.String)
                .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                .CustomColumn("event_type", ev => ev.EventType)
                .CustomColumn("some_date", ev => DateTime.UtcNow)
                .CustomColumn("some_null", ev => null));

            var auditEvent = new AuditEvent()
            {
                EventType = "EventType",
                CustomFields = new Dictionary<string, object>()
                {
                    {"Field1", "Value1"}
                }
            };

            // Act
            var id = (long)dp.InsertEvent(auditEvent);
            auditEvent.CustomFields["Field2"] = "Value2";
            dp.ReplaceEvent(id, auditEvent);
            var auditEventLoaded = dp.GetEvent<AuditEvent>(id);

            // Assert
            Assert.That(id, Is.TypeOf<long>());
            Assert.That(auditEventLoaded, Is.Not.Null);
            Assert.That(auditEventLoaded.EventType, Is.EqualTo("EventType"));
            Assert.That(auditEventLoaded.CustomFields, Is.Not.Null);
            Assert.That(auditEventLoaded.CustomFields.Count, Is.EqualTo(2));
            Assert.That(auditEventLoaded.CustomFields["Field1"].ToString(), Is.EqualTo("Value1"));
            Assert.That(auditEventLoaded.CustomFields["Field2"].ToString(), Is.EqualTo("Value2"));
        }

        [Test]
        public async Task Test_AuditEventData_ReplaceAsync()
        {
            // Arrange
            var dp = new CustomPostgreSqlDataProvider(config => config
                .ConnectionString(TestCommon.PostgreSqlConnectionString)
                .TableName("event_text")
                .IdColumnName("id")
                .DataJsonColumn("data", Configuration.DataType.String)
                .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                .CustomColumn("event_type", ev => ev.EventType)
                .CustomColumn("some_date", ev => DateTime.UtcNow)
                .CustomColumn("some_null", ev => null));

            var auditEvent = new AuditEvent()
            {
                EventType = "EventType",
                CustomFields = new Dictionary<string, object>()
                {
                    {"Field1", "Value1"}
                }
            };

            // Act
            var id = (long)await dp.InsertEventAsync(auditEvent);
            auditEvent.CustomFields["Field2"] = "Value2";
            await dp.ReplaceEventAsync(id, auditEvent);
            var auditEventLoaded = await dp.GetEventAsync<AuditEvent>(id);

            // Assert
            Assert.That(id, Is.TypeOf<long>());
            Assert.That(auditEventLoaded, Is.Not.Null);
            Assert.That(auditEventLoaded.EventType, Is.EqualTo("EventType"));
            Assert.That(auditEventLoaded.CustomFields, Is.Not.Null);
            Assert.That(auditEventLoaded.CustomFields.Count, Is.EqualTo(2));
            Assert.That(auditEventLoaded.CustomFields["Field1"].ToString(), Is.EqualTo("Value1"));
            Assert.That(auditEventLoaded.CustomFields["Field2"].ToString(), Is.EqualTo("Value2"));
        }

        private static string GetLastUpdatedColumnNameColumnName()
        {
            return "updated_date";
        }
        
        private static CustomPostgreSqlDataProvider GetConfiguredPostgreSqlDataProvider(string overrideEventType)
        {
            return new CustomPostgreSqlDataProvider(config => config
                    .ConnectionString(TestCommon.PostgreSqlConnectionString)
                    .TableName("event")
                    .IdColumnName(_ => "id")
                    .DataJsonColumn("data", Configuration.DataType.JSONB, auditEvent =>
                    {
                        auditEvent.EventType = overrideEventType;
                        return auditEvent.ToJson();
                    })
                    .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                    .CustomColumn("event_type", ev => ev.EventType)
                    .CustomColumn("some_date", ev => DateTime.UtcNow)
                    .CustomColumn("some_null", ev => null));
        }
    }

    /// <summary>
    /// Custom postgre sql data provider with pagination
    /// </summary>
    public class CustomPostgreSqlDataProvider : PostgreSqlDataProvider
    {
        public CustomPostgreSqlDataProvider(Action<PostgreSql.Configuration.IPostgreSqlProviderConfigurator> config) : base(config)
        {
        }

        public IEnumerable<AuditEvent> EnumerateEvents(int pageNumber, int pageSize, string whereExpression = null)
        {
            return EnumerateEvents<AuditEvent>(pageNumber, pageSize, whereExpression);
        }

        public IEnumerable<T> EnumerateEvents<T>(int pageNumber, int pageSize, string whereExpression = null) where T : AuditEvent
        {
            int safePageNumber = Math.Max(pageNumber, 1);
            int safePageSize = Math.Max(pageSize, 1);
            int offset = (safePageSize * safePageNumber) - safePageSize;
            string schema = GetSchema(null);

            /* note, the "where-clause" must be applied "inside" the "derived1" inner-query...thus the variable name "inner-where-clause" */
            string innerWhereClause = string.IsNullOrWhiteSpace(whereExpression) ? "" : $" WHERE {whereExpression}";

            /* create the paging-query...which uses an inner-derived-table to capture the ROW_NUMBER values */
            string fullPagingSql =
                $@"SELECT aet.""{GetDataColumnName(null)}"" FROM {schema}""{GetTableName(null)}"" as aet"
                +
                $@" JOIN ( SELECT ""{GetIdColumnName(null)}"", ROW_NUMBER() OVER (ORDER BY ""{GetIdColumnName(null)}"")"
                +
                $@" as ""RowNumb"" FROM {schema}""{GetTableName(null)}"" innerAet"
                +
                $@" {innerWhereClause}) as derived1"
                +
                $@" ON derived1.""{GetIdColumnName(null)}"" = aet.""{GetIdColumnName(null)}"" WHERE derived1.""RowNumb"" > {offset}"
                +
                $@" ORDER BY aet.""{GetIdColumnName(null)}"" ASC LIMIT {safePageSize};";

            return EnumerateEventsByFullSql<T>(fullPagingSql);
        }
    }
}
