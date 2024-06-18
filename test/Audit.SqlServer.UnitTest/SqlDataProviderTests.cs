using System.Collections.Generic;

using Audit.Core;
using Audit.SqlServer.Providers;

using NUnit.Framework;

namespace Audit.SqlServer.UnitTest
{
    [TestFixture]
    public class SqlDataProviderTests
    {
        [Test]
        public void Test_SqlServerProvider_GetFullTableName_With_Schema()
        {
            // Arrange
            var dp = new SqlDataProvider(c => c
                .Schema(ev => ev.CustomFields["Schema"].ToString())
                .TableName(ev => ev.CustomFields["TableName"].ToString()));

            var auditEvent = new AuditEvent()
            {
                CustomFields = new Dictionary<string, object>()
                {
                    { "Schema", "Schema1" },
                    { "TableName", "TableName1" },
                }
            };
            
            // Act
            var tableName = dp.GetFullTableName(auditEvent);

            // Assert
            Assert.That(tableName, Is.EqualTo("[Schema1].[TableName1]"));
        }

        [Test]
        public void Test_SqlServerProvider_GetFullTableName_Without_Schema()
        {
            // Arrange
            var dp = new SqlDataProvider(c => c
                .TableName(ev => ev.CustomFields["TableName"].ToString()));

            var auditEvent = new AuditEvent()
            {
                CustomFields = new Dictionary<string, object>()
                {
                    { "TableName", "TableName1" },
                }
            };

            // Act
            var tableName = dp.GetFullTableName(auditEvent);

            // Assert
            Assert.That(tableName, Is.EqualTo("[TableName1]"));
        }
    }
}
