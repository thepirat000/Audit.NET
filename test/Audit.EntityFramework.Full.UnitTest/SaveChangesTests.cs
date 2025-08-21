using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Audit.IntegrationTest;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.SqlServer)]
    public class SaveChangesTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogContext>().Reset();
        }

        [Test]
        public async Task Test_EF_SaveChangesAsyncOverride()
        {
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var id = new Random().Next();
            using (var ctx = new BlogContext(TestHelper.GetConnectionString("Blogs2")))
            {
                var blog = new Blog()
                {
                    Id = id,
                    Title = "Test"
                };
                ctx.Blogs.Add(blog);
                await ctx.SaveChangesAsync();
            }

            Assert.That(evs.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_EF_SaveChangesAsyncCancellationTokenOverride()
        {
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var id = new Random().Next();
            using (var ctx = new BlogContext(TestHelper.GetConnectionString("Blogs2")))
            {
                var blog = new Blog()
                {
                    Id = id,
                    Title = "Test"
                };
                ctx.Blogs.Add(blog);
                await ctx.SaveChangesAsync(default(CancellationToken));
            }

            Assert.That(evs.Count, Is.EqualTo(1));
        }

        [Test]
        public void Test_EF_SaveChangesOverride()
        {
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var id = new Random().Next();
            using (var ctx = new BlogContext(TestHelper.GetConnectionString("Blogs2")))
            {
                var blog = new Blog()
                {
                    Id = id,
                    Title = "Test"
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }

            Assert.That(evs.Count, Is.EqualTo(1));
        }
    }
}
