#if NET461
using Audit.Core;
using Audit.SqlServer.Providers;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.WindowsAzure.Storage.Auth;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audit.IntegrationTest
{
    [TestFixture]
    public class SqlServerTests
    {
        [Test]
        [Category("SQL")]
        public void Test_SqlServer_DbConnection()
        {
            var sqlDp = new SqlDataProvider(_ => _
                    .DbConnection(ev => GetConnection("data source=localhost;initial catalog=Audit;integrated security=true;"))
                    .TableName(ev => "Event")
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

        public static DbConnection GetConnection(string cnnString)
        {
            return new SqlConnection(cnnString);
        }

    }
}

#endif