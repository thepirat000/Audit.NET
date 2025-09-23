using Audit.Core;
using Audit.IntegrationTest;
using Audit.MySql.Providers;

using NUnit.Framework;

using System.Threading.Tasks;

namespace Audit.MySql.UnitTest
{
    [TestFixture]
    public class MySqlTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_MySqlDataProvider_FluentApi()
        {
            var x = new MySql.Providers.MySqlDataProvider(_ => _
                .ConnectionString("c")
                .IdColumnName("id")
                .JsonColumnName("j")
                .TableName("t"));
            Assert.That(x.ConnectionString, Is.EqualTo("c"));
            Assert.That(x.IdColumnName, Is.EqualTo("id"));
            Assert.That(x.JsonColumnName, Is.EqualTo("j"));
            Assert.That(x.TableName, Is.EqualTo("t"));
        }

        [Test]
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.MySql)]
        public void Test_MySqlDataProvider_Insert()
        {
            Audit.Core.Configuration.Setup()
                .UseMySql(config => config
                    .ConnectionString("Server=localhost; Database=test; Uid=admin; Pwd=admin;")
                    .TableName("event")
                    .IdColumnName("id")
                    .JsonColumnName("data")
                    .CustomColumn("user", ev => ev.Environment.UserName))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var scope = AuditScope.Create("test", null, new { updated = 0 });
            scope.Event.CustomFields["updated"] = 1;
            var id = scope.EventId;
            scope.Dispose();

            var dp = Audit.Core.Configuration.DataProviderAs<MySqlDataProvider>();
            var ev = dp.GetEvent<AuditEvent>(id);
            var ev2 = dp.GetEvent<AuditEvent>("wrong-id");

            Assert.That(ev, Is.Not.Null);
            Assert.That(ev2, Is.Null);
            Assert.That(ev.CustomFields["updated"].ToString(), Is.EqualTo("1"));
        }

        [Test]
        [Category(TestCommon.Category.Integration)]
        [Category(TestCommon.Category.MySql)]
        public async Task Test_MySqlDataProvider_InsertAsync()
        {
            Audit.Core.Configuration.Setup()
                .UseMySql(config => config
                    .ConnectionString("Server=localhost; Database=test; Uid=admin; Pwd=admin;")
                    .TableName("event")
                    .IdColumnName("id")
                    .JsonColumnName("data")
                    .CustomColumn("user", ev => ev.Environment.UserName))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();

            var scope = await AuditScope.CreateAsync("test", null, new { updated = 0 });
            scope.Event.CustomFields["updated"] = 1;
            var id = scope.EventId;
            await scope.DisposeAsync();

            var dp = Audit.Core.Configuration.DataProviderAs<MySqlDataProvider>();
            var ev = await dp.GetEventAsync<AuditEvent>(id);
            var ev2 = await dp.GetEventAsync<AuditEvent>("wrong-id");

            Assert.That(ev, Is.Not.Null);
            Assert.That(ev2, Is.Null);
            Assert.That(ev.CustomFields["updated"].ToString(), Is.EqualTo("1"));
        }
    }
}
