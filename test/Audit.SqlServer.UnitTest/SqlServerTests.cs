using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Audit.Core;
using Audit.SqlServer.Providers;
#if !NET45
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
#endif
using NUnit.Framework;

namespace Audit.SqlServer.UnitTest
{
    [TestFixture]
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

            var sqlDp = (SqlDataProvider)Audit.Core.Configuration.DataProvider;

            Assert.AreEqual(1, ids.Count);

            var auditEvent = sqlDp.GetEvent(ids[0]);

            Assert.IsNotNull(auditEvent);
            Assert.AreEqual(nameof(Test_SqlServer_Provider), auditEvent.EventType);
            Assert.AreEqual(guid.ToString(), auditEvent.CustomFields["guid"]?.ToString());
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
#if NET45
                    .SetDatabaseInitializerNull()
#endif
            );
            Assert.AreEqual("cnnString", x.ConnectionStringBuilder.Invoke(null));
            Assert.AreEqual("evType", x.IdColumnNameBuilder.Invoke(new AuditEvent() { EventType = "evType" }));
            Assert.AreEqual("json", x.JsonColumnNameBuilder.Invoke(null));
            Assert.IsTrue(x.CustomColumns.Any(cc => cc.Name == "EventType" && (string)cc.Value.Invoke(new AuditEvent() { EventType = "pepe" }) == "pepe"));
            Assert.AreEqual("last", x.LastUpdatedDateColumnNameBuilder.Invoke(null));
            Assert.AreEqual("schema", x.SchemaBuilder.Invoke(null));
            Assert.AreEqual("table", x.TableNameBuilder.Invoke(null));
#if NET45
            Assert.AreEqual(true, x.SetDatabaseInitializerNull);
#endif
        }

#if NETCOREAPP3_0 || NET5_0_OR_GREATER
        [Test]
        public void Test_Sql_DbContextOptions()
        {
            TestInterceptor.Count = 0;
            var sqlProvider = new SqlDataProvider(config => config
                    .ConnectionString("data source=localhost;initial catalog=Audit;integrated security=true;Encrypt=False;")
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
            public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
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
#endif

#if NET45
        [Test]
        public void Test_SqlServer_DbConnection()
        {
            var sqlDp = new SqlDataProvider(_ => _
                .DbConnection(ev => new SqlConnection(SqlTestHelper.GetConnectionString()))
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

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(sqlDp);

            AuditScope.Log("test1", new { Name = "John" });
            AuditScope.Log("test2", new { Name = "Mary" });

            Assert.AreEqual(2, ids.Count);

            var ev1 = sqlDp.GetEvent(ids[0]);
            var ev2 = sqlDp.GetEvent(ids[1]);

            Assert.AreEqual("John", ev1.CustomFields["Name"].ToString());
            Assert.AreEqual("Mary", ev2.CustomFields["Name"].ToString());
        }
#endif

    }
}