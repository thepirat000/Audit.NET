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
            var dp = GetConfiguredPostgreSqlDataProvider(overrideEventType);
            
            var scope = AuditScope.Create("test", null);
            scope.Dispose();

            var id = scope.EventId;

            var getEvent = dp?.GetEvent<AuditEvent>(id);

            Assert.AreEqual(overrideEventType, getEvent?.EventType);
        }

        [Test]
        [Category("PostgreSQL")]
        public void Test_PostgreDataProvider_Paging_No_Where()
        {
            const int pageNumberOne = 1;
            const int pageSize = 10;

            PostgreSqlDataProvider dp = GetConfiguredPostgreSqlDataProvider();
            IEnumerable<AuditEvent> events = dp?.EnumerateEvents(pageNumberOne, pageSize);
            ICollection<AuditEvent> realizedEvents = events.ToList();
            Assert.IsNotNull(realizedEvents);
        }
        
        [Test]
        [Category("PostgreSQL")]
        public void Test_PostgreDataProvider_Paging_With_Where()
        {
            const int pageNumber = 3;
            const int pageSize = 10;

            string whereExpression = @""""+ GetLastUpdatedColumnNameColumnName() + @""" > '12/31/1900'";
            PostgreSqlDataProvider dp = GetConfiguredPostgreSqlDataProvider();
            IEnumerable<AuditEvent> events = dp?.EnumerateEvents(pageNumber, pageSize, whereExpression);
            ICollection<AuditEvent> realizedEvents = events.ToList();
            Assert.IsNotNull(realizedEvents);
        }
        
        private static string GetLastUpdatedColumnNameColumnName()
        {
            /* this value is encapsulated so both RunLocalAuditConfiguration and Test_PostgreDataProvider_Paging_With_Where use the same value */
            return "UpdateDate";
        }

        private static PostgreSqlDataProvider GetConfiguredPostgreSqlDataProvider()
        {
            string overrideEventType = Guid.NewGuid().ToString();
            return GetConfiguredPostgreSqlDataProvider(overrideEventType);
        }
        
        private static PostgreSqlDataProvider GetConfiguredPostgreSqlDataProvider(string overrideEventType)
        {
            Audit.Core.Configuration.Setup()
                .UsePostgreSql(config => config
                    .ConnectionString(AzureSettings.PostgreSqlConnectionString)
                    .TableName("event")
                    .IdColumnName(_ => "id")
                    .DataColumn("data", DataType.JSONB, auditEvent =>
                    {
                        auditEvent.EventType = overrideEventType;
                        return auditEvent.ToJson();
                    })
                    .LastUpdatedColumnName(GetLastUpdatedColumnNameColumnName())
                    .CustomColumn("event_type", ev => ev.EventType)
                    .CustomColumn("some_date", ev => DateTime.UtcNow)
                    .CustomColumn("some_null", ev => null))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();
            
            
            var dp = (PostgreSqlDataProvider)Configuration.DataProvider;
            return dp;
        }
    }
}
#endif