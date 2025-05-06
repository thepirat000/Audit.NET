using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
    public class ActivityWithEntityFrameworkTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup().ForAnyContext().Reset();
            using var ctx = new BlogsMemoryContext();
            ctx.Database.EnsureCreated();
            Audit.Core.Configuration.ResetCustomActions();
        }

        /*[TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartInsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]*/
        [TestCase(EventCreationPolicy.Manual)]
        public void ActivityWithEntityFramework_ReuseAuditScopeActivity(EventCreationPolicy eventCreationPolicy)
        {
            // Arrange
            var started = new List<Activity>();
            var stopped = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => started.Add(activity),
                ActivityStopped = activity => stopped.Add(activity)
            };

            ActivitySource.AddActivityListener(listener);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsMemoryContext>(x => x
                    .AuditEventType("EF"))
                .UseOptOut();

            var dataProvider = new ActivityDataProvider()
            {
                AdditionalTags = new(ev => new Dictionary<string, object>()
                {
                    { "tag", "value" }
                }),
                TryUseAuditScopeActivity = true
            };

            Audit.Core.Configuration.Setup()
                .StartActivityTrace()
                .Use(dataProvider)
                .WithCreationPolicy(eventCreationPolicy)
                .WithAction(a => a.OnScopeCreated(scope =>
                {
                    if (eventCreationPolicy == EventCreationPolicy.Manual)
                    {
                        scope.Save();
                    }
                }));

            // Act
            using var context = new BlogsMemoryContext();
            
            var user = new User
            {
                Name = "Test",
                Name2 = "Test2"
            };

            context.Users.Add(user);
            context.SaveChangesGetAudit();
            
            // Assert
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(stopped, Has.Count.EqualTo(1));

            Assert.That(stopped[0].GetTagItem("tag")!.ToString(), Is.EqualTo("value"));
            Assert.That(stopped[0].Source.Name, Is.EqualTo(typeof(AuditScope).FullName!));
        }

        [TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartInsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]
        [TestCase(EventCreationPolicy.Manual)]
        public async Task ActivityWithEntityFramework_ReuseAuditScopeActivityAsync(EventCreationPolicy eventCreationPolicy)
        {
            // Arrange
            var started = new List<Activity>();
            var stopped = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => started.Add(activity),
                ActivityStopped = activity => stopped.Add(activity)
            };

            ActivitySource.AddActivityListener(listener);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsMemoryContext>(x => x
                    .AuditEventType("EF"))
                .UseOptOut();

            var dataProvider = new ActivityDataProvider()
            {
                AdditionalTags = new(ev => new Dictionary<string, object>()
                {
                    { "tag", "value" }
                }),
                TryUseAuditScopeActivity = true
            };

            Audit.Core.Configuration.Setup()
                .StartActivityTrace()
                .Use(dataProvider)
                .WithCreationPolicy(eventCreationPolicy)
                .WithAction(a => a.OnScopeCreated(async scope =>
                {
                    if (eventCreationPolicy == EventCreationPolicy.Manual)
                    {
                        await scope.SaveAsync();
                    }
                }));

            // Act
            await using var context = new BlogsMemoryContext();

            var user = new User
            {
                Name = "Test",
                Name2 = "Test2"
            };

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            // Assert
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(stopped, Has.Count.EqualTo(1));
            Assert.That(stopped[0].GetTagItem("tag")!.ToString(), Is.EqualTo("value"));
            Assert.That(stopped[0].Source.Name, Is.EqualTo(typeof(AuditScope).FullName!));
        }
    }
}
