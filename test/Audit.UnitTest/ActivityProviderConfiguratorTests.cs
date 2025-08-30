using System;
using System.Collections.Generic;
using System.Diagnostics;

using Audit.Core;
using Audit.Core.ConfigurationApi;

using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class ActivityProviderConfiguratorTests
    {
        [Test]
        public void Source_String_SetsSourceNameAndNullsVersion()
        {
            var cfg = new ActivityProviderConfigurator();

            var result = cfg.Source("MySource");

            Assert.That(cfg._sourceName.GetValue(null), Is.EqualTo("MySource"));
            Assert.That(cfg._sourceVersion.GetValue(null), Is.Null);
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void Source_StringString_SetsSourceNameAndVersion()
        {
            var cfg = new ActivityProviderConfigurator();

            var result = cfg.Source("MySource", "1.2.3");

            Assert.That(cfg._sourceName.GetValue(null), Is.EqualTo("MySource"));
            Assert.That(cfg._sourceVersion.GetValue(null), Is.EqualTo("1.2.3"));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void Source_Func_SetsSourceNameFuncAndNullsVersion()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, string> nameFunc = ev => "DynamicName";

            var result = cfg.Source(nameFunc);

            Assert.That(cfg._sourceName.GetValue(new AuditEvent()), Is.EqualTo("DynamicName"));
            Assert.That(cfg._sourceVersion.GetValue(null), Is.Null);
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void Source_FuncFunc_SetsSourceNameAndVersionFuncs()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, string> nameFunc = ev => "DynamicName";
            Func<AuditEvent, string> versionFunc = ev => "2.0";

            var result = cfg.Source(nameFunc, versionFunc);

            Assert.That(cfg._sourceName.GetValue(new AuditEvent()), Is.EqualTo("DynamicName"));
            Assert.That(cfg._sourceVersion.GetValue(new AuditEvent()), Is.EqualTo("2.0"));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void ActivityName_String_SetsActivityName()
        {
            var cfg = new ActivityProviderConfigurator();

            var result = cfg.ActivityName("MyActivity");

            Assert.That(cfg._activityName.GetValue(null), Is.EqualTo("MyActivity"));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void ActivityName_Func_SetsActivityNameFunc()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, string> nameFunc = ev => "DynamicActivity";

            var result = cfg.ActivityName(nameFunc);

            Assert.That(cfg._activityName.GetValue(new AuditEvent()), Is.EqualTo("DynamicActivity"));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void ActivityKind_Kind_SetsActivityKind()
        {
            var cfg = new ActivityProviderConfigurator();

            var result = cfg.ActivityKind(ActivityKind.Server);

            Assert.That(cfg._activityKind.GetValue(null), Is.EqualTo(ActivityKind.Server));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void ActivityKind_Func_SetsActivityKindFunc()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, ActivityKind> kindFunc = ev => ActivityKind.Client;

            var result = cfg.ActivityKind(kindFunc);

            Assert.That(cfg._activityKind.GetValue(new AuditEvent()), Is.EqualTo(ActivityKind.Client));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void IncludeDefaultTags_Bool_SetsIncludeDefaultTags()
        {
            var cfg = new ActivityProviderConfigurator();

            var result = cfg.IncludeDefaultTags(true);

            Assert.That(cfg._includeDefaultTags.GetValue(null), Is.True);
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void IncludeDefaultTags_Func_SetsIncludeDefaultTagsFunc()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, bool> includeFunc = ev => false;

            var result = cfg.IncludeDefaultTags(includeFunc);

            Assert.That(cfg._includeDefaultTags.GetValue(new AuditEvent()), Is.False);
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void AdditionalTags_SetsAdditionalTagsFunc()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, Dictionary<string, object>> tagsFunc = ev => new() { { "k", 1 } };

            var result = cfg.AdditionalTags(tagsFunc);

            Assert.That(cfg._additionalTags(new AuditEvent()), Contains.Key("k").WithValue(1));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void OnActivityCreated_SetsAction()
        {
            var cfg = new ActivityProviderConfigurator();
            Action<Activity, AuditEvent> action = (a, e) => { };

            var result = cfg.OnActivityCreated(action);

            Assert.That(cfg._onActivityCreated, Is.SameAs(action));
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void TryUseAuditScopeActivity_Bool_SetsTryUseAuditScopeActivity()
        {
            var cfg = new ActivityProviderConfigurator();

            var result = cfg.TryUseAuditScopeActivity(true);

            Assert.That(cfg._tryUseAuditScopeActivity.GetValue(null), Is.True);
            Assert.That(result, Is.SameAs(cfg));
        }

        [Test]
        public void TryUseAuditScopeActivity_Func_SetsTryUseAuditScopeActivityFunc()
        {
            var cfg = new ActivityProviderConfigurator();
            Func<AuditEvent, bool> tryFunc = ev => false;

            var result = cfg.TryUseAuditScopeActivity(tryFunc);

            Assert.That(cfg._tryUseAuditScopeActivity.GetValue(new AuditEvent()), Is.False);
            Assert.That(result, Is.SameAs(cfg));
        }
    }
}