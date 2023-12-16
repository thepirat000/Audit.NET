using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Audit.Core;
using Audit.SqlServer.Providers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Audit.SqlServer.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
    public class SqlServerTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            SqlTestHelper.EnsureDatabaseCreated();
        }

        [Test]
        public void Test_SqlServer_Provider()
        {
            Audit.Core.Configuration.Setup()
                .UseSqlServer(_ => _
                    .ConnectionString(SqlTestHelper.GetConnectionString())
                    .TableName(ev => SqlTestHelper.TableName)
                    .IdColumnName(ev => "EventId")
                    .JsonColumnName(ev => "Data")
                    .LastUpdatedColumnName("LastUpdatedDate")
                    .CustomColumn("EventType", ev => ev.EventType));

            var ids = new List<object>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                ids.Add(scope.EventId);
            });

            var guid = Guid.NewGuid();
            AuditScope.Log(nameof(Test_SqlServer_Provider), new { guid });

            var sqlDp = Audit.Core.Configuration.DataProviderAs<SqlDataProvider>();

            Assert.AreEqual(1, ids.Count);

            var auditEvent = sqlDp.GetEvent(ids[0]);

            Assert.IsNotNull(auditEvent);
            Assert.AreEqual(nameof(Test_SqlServer_Provider), auditEvent.EventType);
            Assert.AreEqual(guid.ToString(), auditEvent.CustomFields["guid"]?.ToString());
        }

        [Test]
        public void Test_SqlServer_Provider_GuardCondition()
        {
            Audit.Core.Configuration.Setup()
                .UseSqlServer(_ => _
                    .ConnectionString(SqlTestHelper.GetConnectionString())
                    .TableName(ev => SqlTestHelper.TableName)
                    .IdColumnName(ev => "EventId")
                    .JsonColumnName(ev => "Data")
                    .LastUpdatedColumnName("LastUpdatedDate")
                    .CustomColumn("DoesNotExists1", ev => throw new Exception("Should not happen"), ev => false)
                    .CustomColumn("DoesNotExists2", ev => null, ev => false)
                    .CustomColumn("EventType", ev => ev.EventType, ev => ev.EventType == nameof(Test_SqlServer_Provider)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var ids = new List<object>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                ids.Add(scope.EventId);
            });

            using (var scope = AuditScope.Create(nameof(Test_SqlServer_Provider), null, new { field = "initial" }))
            {
                scope.SetCustomField("field", "final");
            }

            var sqlDp = Audit.Core.Configuration.DataProviderAs<SqlDataProvider>();

            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(ids[0], ids[1]);
            var auditEvent = sqlDp.GetEvent(ids[0]);

            Assert.IsNotNull(auditEvent);
            Assert.AreEqual(nameof(Test_SqlServer_Provider), auditEvent.EventType);
            Assert.AreEqual("final", auditEvent.CustomFields["field"]?.ToString());
        }

        [Test]
        public void Test_SqlDataProvider_FluentApi()
        {
            var x = new SqlDataProvider(_ => _
                    .ConnectionString("cnnString")
                    .IdColumnName(ev => ev.EventType)
                    .JsonColumnName("json")
                    .LastUpdatedColumnName("last")
                    .Schema(ev => "schema")
                    .TableName("table")
                    .CustomColumn("EventType", ev => ev.EventType)
            );
            Assert.AreEqual("cnnString", x.ConnectionStringBuilder.Invoke(null));
            Assert.AreEqual("evType", x.IdColumnNameBuilder.Invoke(new AuditEvent() { EventType = "evType" }));
            Assert.AreEqual("json", x.JsonColumnNameBuilder.Invoke(null));
            Assert.IsTrue(x.CustomColumns.Any(cc => cc.Name == "EventType" && (string)cc.Value.Invoke(new AuditEvent() { EventType = "pepe" }) == "pepe"));
            Assert.AreEqual("last", x.LastUpdatedDateColumnNameBuilder.Invoke(null));
            Assert.AreEqual("schema", x.SchemaBuilder.Invoke(null));
            Assert.AreEqual("table", x.TableNameBuilder.Invoke(null));
        }

        [Test]
        public void Test_Sql_DbContextOptions()
        {
            var cnnString = TestHelper.GetConnectionString("Audit");
            TestInterceptor.Count = 0;
            var sqlProvider = new SqlDataProvider(config => config
                    .ConnectionString(cnnString)
                    .DbContextOptions(new DbContextOptionsBuilder().AddInterceptors(new TestInterceptor()).Options)
                    .TableName(ev => "Event")
                    .IdColumnName(ev => "EventId")
                    .JsonColumnName(ev => "Data")
                    .LastUpdatedColumnName("LastUpdatedDate")
                    .CustomColumn("EventType", ev => ev.EventType));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = sqlProvider, EventType = "TestInterceptor", CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
            }

            Assert.AreEqual(1, TestInterceptor.Count);
        }

        public class TestInterceptor : DbConnectionInterceptor
        {
            public static int Count { get; set; }
            public TestInterceptor() : base()
            {
            }

#if NET462
            public override async Task<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
#else
            public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
#endif
            {
                await Task.Delay(0);
                Count++;
                return result;
            }
            public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            {
                Count++;
                return result;
            }
        }
    }
}