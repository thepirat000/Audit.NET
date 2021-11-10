﻿using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("LocalDb")]
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
            using (var ctx = new BlogContext("data source=(localDb)\\MSSQLLocaldb;initial catalog=Blogs2;integrated security=true;"))
            {
                var blog = new Blog()
                {
                    Id = id,
                    Title = "Test"
                };
                ctx.Blogs.Add(blog);
                await ctx.SaveChangesAsync();
            }

            Assert.AreEqual(1, evs.Count);
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
            using (var ctx = new BlogContext("data source=(localDb)\\MSSQLLocaldb;initial catalog=Blogs2;integrated security=true;"))
            {
                var blog = new Blog()
                {
                    Id = id,
                    Title = "Test"
                };
                ctx.Blogs.Add(blog);
                await ctx.SaveChangesAsync(default(CancellationToken));
            }

            Assert.AreEqual(1, evs.Count);
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
            using (var ctx = new BlogContext("data source=(localDb)\\MSSQLLocaldb;initial catalog=Blogs2;integrated security=true;"))
            {
                var blog = new Blog()
                {
                    Id = id,
                    Title = "Test"
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, evs.Count);
        }
    }
}
