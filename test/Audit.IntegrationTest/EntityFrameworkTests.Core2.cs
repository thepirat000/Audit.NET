#if NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0
using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.IntegrationTest
{
    [TestFixture(Category = "EF")]
    public class EntityFrameworkTests_Core2_0
    {
        [OneTimeSetUp]
        public void Init()
        {
            var sql1 = @"if exists (select * from sysobjects where name = 'posts') drop table posts; if exists (select * from sysobjects where name = 'blogs') drop table blogs; create table blogs ( Id int identity(1,1) not null primary key, BloggerName nvarchar(max), Title nvarchar(max) );
                        create table posts ( Id int identity(1,1) not null primary key, Title nvarchar(max), DateCreated datetime, Content nvarchar(max), BlogId int not null constraint FK_P_B foreign key references Blogs (id) );";
            var sql2 = @"if exists (select * from sysobjects where name = 'child') drop table child; if exists (select * from sysobjects where name = 'parent') drop table parent; CREATE TABLE [Parent] (	Id BIGINT IDENTITY(1,1) NOT NULL, [Name] nvarchar(Max) NOT NULL, CONSTRAINT PK_Parent PRIMARY KEY (Id));
                        CREATE TABLE [Child] ( Id BIGINT IDENTITY(1,1) NOT NULL, [Name] nvarchar(Max) NOT NULL, [Period_Start] datetime NOT NULL, [Period_End] datetime NOT NULL, [ParentId] bigint NOT NULL, CONSTRAINT PK_Child PRIMARY KEY (Id), Constraint FK_Child_Parent Foreign Key ([ParentId]) References Parent(Id));";
            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.ExecuteSqlRaw(sql1);
            }
            var connectionString = "data source=localhost;initial catalog=ParentChild;integrated security=true;";
            using (var ctx = new ApplicationDbContext(SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).EnableSensitiveDataLogging().Options))
            {
                ctx.Database.ExecuteSqlRaw(sql2);
            }
        }

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EF_OwnedEntities()
        {
            var events = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x.OnInsertAndReplace(eve =>
                {
                    events.Add(eve.GetEntityFrameworkEvent());
                }));
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext(_ => _.IncludeEntityObjects(true));
            using (var ctx = new MyContext())
            {
                var newBlog = new Blog()
                {
                    Title = "This is Test 1",
                    More = new BlogEx()
                    {
                        BloggerName = "this guy"
                    },
                    Posts = new List<Post>()
                    {
                        new Post()
                        {
                            Title = "post title", Content = "content", DateCreated = DateTime.Now
                        }
                    }
                };
                ctx.Blogs.Add(newBlog);
                ctx.SaveChanges();
                var a = newBlog.Id;
            }
            var ev = events.FirstOrDefault();

            Assert.AreEqual(1, events.Count);
            Assert.NotNull(ev);
            Assert.AreEqual(3, ev.Entries.Count);
            Assert.IsTrue((int)ev.Entries.Single(e => e.Entity is Blog).PrimaryKey.Single().Value > 0);
            Assert.AreEqual(ev.Entries.Single(e => e.Entity is Blog).PrimaryKey.Single().Value, 
                            ev.Entries.Single(e => e.Entity is BlogEx).PrimaryKey.Single().Value);
            Assert.IsTrue(ev.Entries.Single(e => e.Entity is BlogEx).ColumnValues.ContainsKey("Id"));
            Assert.AreEqual(ev.Entries.Single(e => e.Entity is Blog).PrimaryKey.Single().Value, ev.Entries.Single(e => e.Entity is BlogEx).ColumnValues["Id"]);
            Assert.AreEqual("this guy", ev.Entries.Single(e => e.Entity is BlogEx).ColumnValues["BloggerName"]);
        }

        [Test]
        public async Task Test_EF_OwnedEntities_WithFK()
        {
            var events = new List<EntityFrameworkEvent>();
            long childId = -1;
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x.OnInsertAndReplace(eve =>
                {
                    events.Add(eve.GetEntityFrameworkEvent());
                }));
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext(_ => _.IncludeEntityObjects(true));
            var connectionString = "data source=localhost;initial catalog=ParentChild;integrated security=true";
            using (var _dbContext = new ApplicationDbContext(SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).EnableSensitiveDataLogging().Options))
            {
                _dbContext.Database.EnsureCreated();
                var parent = new Parent { Name = "PARENT 1" };
                var child = new Child { Name = "CHILD 1", Period = new Period { Start = new DateTime(2017, 1, 1), End = new DateTime(2018, 1, 1) } };
                parent.Children = new List<Child> { child };

                await _dbContext.Parents.AddAsync(parent);
                await _dbContext.SaveChangesAsync();
                childId = child.Id;
            }
            var ev = events.FirstOrDefault();

            Assert.AreEqual(1, events.Count);
            Assert.NotNull(ev);
            Assert.AreEqual(3, ev.Entries.Count);
            Assert.IsTrue(childId > 0);
            Assert.AreEqual(childId, (long)ev.Entries.Single(e => e.Entity is Child).PrimaryKey.Single().Value);
            Assert.AreEqual(childId, ev.Entries.Single(e => e.Entity is Period).PrimaryKey.Single().Value);
            Assert.IsTrue(ev.Entries.Single(e => e.Entity is Period).ColumnValues.ContainsKey("Id"));
            Assert.AreEqual(childId, ev.Entries.Single(e => e.Entity is Period).ColumnValues["Id"]);
        }


        public class Blog
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public BlogEx More { get; set; }
            public virtual ICollection<Post> Posts { get; set; }
        }
        public class BlogEx
        {
            public string BloggerName { get; set; }
        }

        public class Post
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public DateTime DateCreated { get; set; }
            public string Content { get; set; }
            public int BlogId { get; set; }
            public virtual Blog Blog { get; set; }
        }

        public class MyContext : AuditDbContext
        {
            public MyContext() : base(GetOptions("data source=localhost;initial catalog=Blogs;integrated security=true;"))
            {
            }
            public DbSet<Blog> Blogs { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Blog>().ToTable("Blogs");
                modelBuilder.Entity<Blog>().HasKey(k => k.Id);
                modelBuilder.Entity<Blog>().OwnsOne(x => x.More, x => x.Property(a => a.BloggerName).HasColumnName("BloggerName"));

                modelBuilder.Entity<Post>().ToTable("Posts");
                modelBuilder.Entity<Post>().HasKey(k => k.Id);
                modelBuilder.Entity<Post>().HasOne(p => p.Blog)
                            .WithMany(b => b.Posts)
                            .HasForeignKey(p => p.BlogId);

            }

            private static DbContextOptions GetOptions(string connectionString)
            {
                return SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).EnableSensitiveDataLogging().Options;
            }
        }

        //---

        public class ApplicationDbContext : Audit.EntityFramework.AuditDbContext // DbContext
        {

            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }
            public ApplicationDbContext(DbContextOptions options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                base.OnModelCreating(builder);

                builder.Entity<Child>(x =>
                {
                    x.OwnsOne(c => c.Period);
                    x.ToTable("Child");
                });

                builder.Entity<Parent>(x =>
                {
                    x.Property(c => c.Name).IsRequired();
                    x.HasMany<Child>(c => c.Children);
                    x.ToTable("Parent");
                });
            }


            public DbSet<Parent> Parents { get; set; }
        }

        public class Child
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public Period Period { get; set; }
        }
        public class Parent
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public List<Child> Children { get; set; }
        }
        public class Period
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }

    }


}
#endif


