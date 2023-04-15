﻿#if EF_CORE_3_OR_GREATER
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using System.Threading;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class SaveChangesDisposalTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new InMemoryContext(null).Database.EnsureCreated();
        }

        [Test]
        public void Test_EF_Core_ContextDisposedAfterSave()
        {
            // Audit should not throw when the DbContext is disposed by SaveChanges
            var inserted = new List<EntityFrameworkEvent>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(ev.GetEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Func<InMemoryContext, Task> disposeContext = ctx =>
            {
                ctx.Dispose();
                return Task.CompletedTask;
            };

            using (var context = new InMemoryContext(disposeContext))
            {
                var user = new User() { Name = "Test" };
                context.Users.Add(user);
                context.SaveChanges();
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.IsNotNull(inserted[0]);
        }

        [Test]
        public async Task Test_EF_Core_ContextDisposedAfterSaveAsync()
        {
            // Audit should not throw when the DbContext is disposed by SaveChanges
            var inserted = new List<EntityFrameworkEvent>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(ev.GetEntityFrameworkEvent())))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Func<InMemoryContext, Task> disposeContext = async ctx =>
            {
                await ctx.DisposeAsync();
            };

            using (var context = new InMemoryContext(disposeContext))
            {
                var user = new User() { Name = "Test" };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.IsNotNull(inserted[0]);
        }

    }

    public class InMemoryContext : DbContext
    {
        private readonly DbContextHelper _helper = new DbContextHelper();
        private readonly IAuditDbContext _auditContext;

        public DbSet<User> Users { get; set; }

        private readonly Func<InMemoryContext, Task> _doAfterSaveChanges;

        public InMemoryContext(Func<InMemoryContext, Task> doAfterSaveChanges)
        {
            _auditContext = new DefaultAuditContext(this);
            _helper.SetConfig(_auditContext);

            _doAfterSaveChanges = doAfterSaveChanges;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemoryContext;Trusted_Connection=True;ConnectRetryCount=0");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return _helper.SaveChanges(_auditContext, () =>
            {
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                _doAfterSaveChanges(this);
                return result;
            });
        }
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _helper.SaveChangesAsync(_auditContext, async () =>
            {
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                await _doAfterSaveChanges(this);
                return result;
            }, cancellationToken);
        }

    }
}
#endif