#if NETCOREAPP2_0
using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Audit.IntegrationTest
{
    [TestFixture(Category = "EF")]
    public class EntityFrameworkTests_Core2_0
    {
        [OneTimeSetUp]
        public void Init()
        {
            var sql = @"drop table posts; drop table blogs; create table blogs ( Id int identity(1,1) not null primary key, BloggerName nvarchar(max), Title nvarchar(max) );
                        create table posts ( Id int identity(1,1) not null primary key, Title nvarchar(max), DateCreated datetime, Content nvarchar(max), BlogId int not null constraint FK_P_B foreign key references Blogs (id) );";
            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.ExecuteSqlCommand(sql);
            }
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
            Assert.IsTrue(ev.Entries.Single(e => e.Entity is BlogEx).ColumnValues.ContainsKey("BlogId"));
            Assert.AreEqual(ev.Entries.Single(e => e.Entity is Blog).PrimaryKey.Single().Value, ev.Entries.Single(e => e.Entity is BlogEx).ColumnValues["BlogId"]);
            Assert.AreEqual("this guy", ev.Entries.Single(e => e.Entity is BlogEx).ColumnValues["BloggerName"]);
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

    }


}
#endif