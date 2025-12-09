using Hangfire;
using Hangfire.Common;

using NUnit.Framework;

using System.Linq;

namespace Audit.Hangfire.UnitTest;

[TestFixture]
public class GlobalConfigurationExtensionsTests
{
    private class DummyGlobalConfiguration : IGlobalConfiguration
    {
        public IGlobalConfiguration UseActivator(JobActivator activator) => this;
        public IGlobalConfiguration UseAuthorizationFilters(params global::Hangfire.Dashboard.IDashboardAuthorizationFilter[] filters) => this;
        public IGlobalConfiguration UseConsole() => this;
        public IGlobalConfiguration UseDashboardHangfireAuthorization() => this;
        public IGlobalConfiguration UseFilter(JobFilterAttribute filter) => this;
        public IGlobalConfiguration UseLogProvider(global::Hangfire.Logging.ILogProvider logProvider) => this;
        public IGlobalConfiguration UseMsmqQueues(params string[] queues) => this;
        public IGlobalConfiguration UseRedisStorage(string connectionString) => this;
        public IGlobalConfiguration UseStorage(JobStorage storage) => this;
    }

    [Test]
    public void AddAuditJobCreationFilter_Adds_Filter_And_Returns_Same_Configuration()
    {
        var global = new DummyGlobalConfiguration();
        
        var result = GlobalConfigurationExtensions.AddAuditJobCreationFilter(global, cfg => cfg.IncludeParameters());

        var filters = GlobalJobFilters.Filters;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(filters, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(filters.FirstOrDefault(f => f.Instance is AuditJobCreationFilterAttribute), Is.Not.Null);
        });
    }

    [Test]
    public void AddAuditJobExecutionFilter_Adds_Filter_And_Returns_Same_Configuration()
    {
        var global = new DummyGlobalConfiguration();

        var result = GlobalConfigurationExtensions.AddAuditJobExecutionFilter(global, cfg => cfg.ExcludeArguments());

        var filters = GlobalJobFilters.Filters;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(filters, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(filters.FirstOrDefault(f => f.Instance is AuditJobExecutionFilterAttribute), Is.Not.Null);
        });
    }
}