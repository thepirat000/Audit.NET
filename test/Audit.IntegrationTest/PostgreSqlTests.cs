#if NET461 || NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Audit.Core;
using Audit.PostgreSql.Configuration;
using Audit.PostgreSql.Providers;

namespace Audit.IntegrationTest
{
    [TestFixture]
    public class PostgreSqlTests
    {
        [Test]
        [Category("PostgreSQL")]
        public void Test_PostgreDataProvider_CustomDataColumn()
        {
            var overrideEventType = Guid.NewGuid().ToString();
            RunLocalAuditConfiguration(overrideEventType);

            var scope = AuditScope.Create("test", null);
            scope.Dispose();

            var id = scope.EventId;
            var dp = (PostgreSqlDataProvider)Configuration.DataProvider;

            var getEvent = dp?.GetEvent<AuditEvent>(id);

            Assert.AreEqual(overrideEventType, getEvent?.EventType);
        }

        [Test]
        [Category("PostgreSQL")]
        public void Test_PostgreDataProvider_Paging()
        {
            string overrideEventType = Guid.NewGuid().ToString();
            RunLocalAuditConfiguration(overrideEventType);

            const int pageNumber = 3;
            const int pageSize = 10;

            PostgreSqlDataProvider dp = (PostgreSqlDataProvider)Configuration.DataProvider;
            IEnumerable<AuditEvent> events = dp?.EnumerateEvents<AuditEvent>(pageNumber, pageSize);
            ICollection<AuditEvent> realizedEvents = events.ToList();
            Assert.IsNotNull(realizedEvents);
        }

        private static void RunLocalAuditConfiguration(string overrideEventType)
        {
            Audit.Core.Configuration.Setup()
                .UsePostgreSql(config => config
                    .ConnectionString(AzureSettings.PostgreSqlConnectionString)
                    .TableName("event")
                    .IdColumnName(_ => "id")
                    .DataColumn("data", Audit.PostgreSql.Configuration.DataType.JSONB, auditEvent =>
                    {
                        auditEvent.EventType = overrideEventType;
                        return auditEvent.ToJson();
                    })
                    .LastUpdatedColumnName("updated_date")
                    .CustomColumn("event_type", ev => ev.EventType)
                    .CustomColumn("some_date", ev => DateTime.UtcNow)
                    .CustomColumn("some_null", ev => null))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();
        }
    }
}
#endif