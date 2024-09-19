using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Audit.Core;
using Audit.IntegrationTest;
using Audit.PostgreSql.Providers;

namespace Audit.PostgreSql.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("PostgreSQL")]
    public class PostgreSqlTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            Audit.Core.Configuration.Reset();
            SetupConfiguredPostgreSqlDataProvider();
            AuditScope.Log("test", null);
        }

        [Test]
        public void Test_PostgreDataProvider_FluentApi()
        {
            var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                .ConnectionString("c")
                .DataColumn("dc")
                .IdColumnName("id")
                .LastUpdatedColumnName("lud")
                .Schema("sc")
                .TableName("t")
                .CustomColumn("c1", ev => 1)
                .CustomColumn("c2", ev => 2));
            Assert.That(x.ConnectionString.GetValue(null), Is.EqualTo("c"));
            Assert.That(x.DataColumnName.GetValue(null), Is.EqualTo("dc"));
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
        public void Test_PostgreDataProvider_FluentApiBuilder()
        {
            var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                .ConnectionString(ev => "c")
                .DataColumn(ev => "dc")
                .IdColumnName(ev => "id")
                .LastUpdatedColumnName(ev => "lud")
                .Schema(ev => "sc")
                .TableName(ev => "t")
                .CustomColumn("c1", ev => 1)
                .CustomColumn("c2", ev => 2));
            Assert.That(x.ConnectionString.GetDefault(), Is.EqualTo("c"));
            Assert.That(x.DataColumnName.GetDefault(), Is.EqualTo("dc"));
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
            
            var scope = AuditScope.Create("test", null);
            scope.Dispose();

            var id = scope.EventId;

            var getEvent = dp?.GetEvent<AuditEvent>(id);

            Assert.That(getEvent?.EventType, Is.EqualTo(overrideEventType));
        }

        [Test]
        public void Test_PostgreDataProvider_Paging_No_Where()
        {
            const int pageNumberOne = 1;
            const int pageSize = 10;

            var dp = SetupConfiguredPostgreSqlDataProvider();
            
            IEnumerable<AuditEvent> events = dp?.EnumerateEvents(pageNumberOne, pageSize);
            ICollection<AuditEvent> realizedEvents = events.ToList();
            Assert.That(realizedEvents, Is.Not.Null);
        }
        
        [Test]
        public void Test_PostgreDataProvider_Paging_OutOfRange_Inputs()
        {
            /* define negative values.  the code should "safety-ize" them to page-number=1 and page-size=1 */
            const int pageNumberOne = -333;
            const int pageSize = -444;

            var dp = SetupConfiguredPostgreSqlDataProvider();

            IEnumerable<AuditEvent> events = dp?.EnumerateEvents(pageNumberOne, pageSize);
            ICollection<AuditEvent> realizedEvents = events.ToList();
            Assert.That(realizedEvents, Is.Not.Null);
        }        
        
        [Test]
        public void Test_PostgreDataProvider_Paging_With_Where()
        {
            const int pageNumber = 3;
            const int pageSize = 10;

            string whereExpression = @""""+ GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900'";
            var dp = SetupConfiguredPostgreSqlDataProvider();

            IEnumerable<AuditEvent> events = dp?.EnumerateEvents(pageNumber, pageSize, whereExpression);
            ICollection<AuditEvent> realizedEvents = events.ToList();
            Assert.That(realizedEvents, Is.Not.Null);
        }
        
        [Test]
        public void Test_EnumerateEvents_WhereExpression()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            var scope = AuditScope.Create("test", null);
            scope.Dispose();

            var whereExpression = @"""" + GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900'";
            var events = dp?.EnumerateEvents(whereExpression);
            var ev = events?.FirstOrDefault();

            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Test_EnumerateEvents_WhereSortByExpression()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);

            var scope = AuditScope.Create("test", null);
            scope.Dispose();

            var whereExpression = @"""" + GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900'";
            var events = dp?.EnumerateEvents<AuditEvent>(whereExpression, GetLastUpdatedColumnNameColumnName(), "1");
            var ev = events?.FirstOrDefault();

            Assert.That(ev, Is.Not.Null);
        }

        [Test]
        public void Test_AuditEventData_AsText()
        {
            // Arrange
            var dp = new CustomPostgreSqlDataProvider(config => config
                .ConnectionString(AzureSettings.PostgreSqlConnectionString)
                .TableName("event_text")
                .IdColumnName("id")
                .DataColumn("data", Configuration.DataType.String)
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

        private static string GetLastUpdatedColumnNameColumnName()
        {
            return "updated_date";
        }

        private static CustomPostgreSqlDataProvider SetupConfiguredPostgreSqlDataProvider()
        {
            string overrideEventType = Guid.NewGuid().ToString();
            return GetConfiguredPostgreSqlDataProvider(overrideEventType);
        }
        
        private static CustomPostgreSqlDataProvider GetConfiguredPostgreSqlDataProvider(string overrideEventType)
        {
            Audit.Core.Configuration.Setup()
                .UseCustomProvider(new CustomPostgreSqlDataProvider(config => config
                    .ConnectionString(AzureSettings.PostgreSqlConnectionString)
                    .TableName("event")
                    .IdColumnName(_ => "id")
                    .DataColumn("data", Configuration.DataType.JSONB, auditEvent =>
                    {
                        auditEvent.EventType = overrideEventType;
                        return auditEvent.ToJson();
                    })
                    .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                    .CustomColumn("event_type", ev => ev.EventType)
                    .CustomColumn("some_date", ev => DateTime.UtcNow)
                    .CustomColumn("some_null", ev => null)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();
            
            var dp = Core.Configuration.DataProviderAs<CustomPostgreSqlDataProvider>();
            return dp;
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
