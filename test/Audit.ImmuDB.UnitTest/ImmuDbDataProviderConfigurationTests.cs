using System;
using Audit.Core;
using Audit.Core.ConfigurationApi;
using Audit.ImmuDB.Providers;
using Moq;
using NUnit.Framework;

namespace Audit.ImmuDB.UnitTest
{
    [TestFixture]
    public class ImmuDbDataProviderConfigurationTests
    {
        [Test]
        public void ImmuDbDataProvider_Settings_FluentApi()
        {
            var dp = new ImmuDbDataProvider(c => c
                .Database("database")
                .Username("username")
                .Password("password")
                .ClientBuilder(b => b.WithServerUrl("server"))
                .KeySelector(_ => "key")
                .ValueSelector(_ => "value")
                .UseVerifiedMethods()
                .MaxConcurrentWrites(4));

            Assert.That(dp.DatabaseName.GetValue(null), Is.EqualTo("database"));
            Assert.That(dp.Username.GetValue(null), Is.EqualTo("username"));
            Assert.That(dp.Password.GetValue(null), Is.EqualTo("password"));
            Assert.That(dp.KeySelector(new AuditEvent()), Is.EqualTo("key"));
            Assert.That(dp.ValueSelector(new AuditEvent()), Is.EqualTo("value"));
            Assert.That(dp.ClientBuilderAction, Is.Not.Null);
            Assert.That(dp.UseVerifiedMethods, Is.True);
            Assert.That(dp.MaxConcurrentWrites, Is.EqualTo(4));
        }

        [Test]
        public void ImmuDbDataProvider_MaxConcurrentWrites_Default()
        {
            var dp = new ImmuDbDataProvider(c => c.Database("database"));

            Assert.That(dp.MaxConcurrentWrites, Is.EqualTo(ImmuDbDataProvider.DefaultMaxConcurrentWrites));
        }

        [Test]
        public void ImmuDbDataProvider_MaxConcurrentWrites_MustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImmuDbDataProvider(c => c.MaxConcurrentWrites(0)));
        }

        [Test]
        public void UseImmuDb_SetsDataProvider()
        {
            var mockConfigurator = new Mock<IConfigurator>(MockBehavior.Loose);
            
            var result = mockConfigurator.Object.UseImmuDb(cfg => cfg.Database("Test"));

            Assert.That(result, Is.InstanceOf<ICreationPolicyConfigurator>());
            Assert.That(Configuration.DataProvider, Is.TypeOf<ImmuDbDataProvider>());
        }
    }
}