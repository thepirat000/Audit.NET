using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audit.EntityFramework.CodeFirst.UnitTest
{
    public class Blog
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    public class Post
    {
        [Key]
        public int Id { get; set; }
        public int BlogId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public virtual Blog Blog { get; set; }
    }
    public class AuditLog
    {
        [Key]
        public int AuditId { get; set; }
        public string TableName { get; set; }
        public int TablePK { get; set; }
        public string Title { get; set; }

        public DateTime AuditDate { get; set; }
        public string AuditAction { get; set; }
        public string AuditUsername { get; set; }
    }
    public class BlogContext : AuditDbContext
    {
        public BlogContext() : base("data source=localhost;initial catalog=Blogs2;integrated security=true;")
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Blog> Blogs { get; set; }

        public DbSet<AuditLog> Audits { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();
            modelBuilder.Entity<Blog>()
                .MapToStoredProcedures();
            modelBuilder.Entity<Post>()
                .MapToStoredProcedures();
        }
    }

    [TestFixture]
    [Category("LocalDb")]
    public class EfTestsCodeFirst
    {
        [SetUp]
        public void SetUp()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogContext>().Reset();

            using (var ctx = new BlogContext())
            {
                ctx.Database.CreateIfNotExists();
            }
        }

        [Test]
        public void Test_EF_MapMultipleTypesToSameAuditType()
        {
            var guid = Guid.NewGuid().ToString();
            var title = Guid.NewGuid().ToString();

            var list = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .UseDbContext<BlogContext>()
                    .AuditTypeExplicitMapper(m => m
                        .Map<Blog, AuditLog>((blog, audit) =>
                        {
                            // Action for Blog -> Audit
                            audit.TableName = "Blog";
                            audit.TablePK = blog.Id;
                            audit.Title = blog.Title;
                        })
                        .Map<Post, AuditLog>((post, audit) =>
                        {
                            // Action for Post -> Audit
                            audit.TableName = "Post";
                            audit.TablePK = post.Id;
                            audit.Title = post.Title;
                        })
                        .AuditEntityAction<AuditLog>((evt, entry, audit) =>
                        {
                            // Common action
                            audit.AuditDate = DateTime.UtcNow;
                            audit.AuditAction = entry.Action;
                            audit.AuditUsername = Environment.UserName;
                        })));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogContext>()
                .UseOptOut();

            using (var context = new BlogContext())
            {
                context.Blogs.Add(new Blog { Title = title });
                context.SaveChanges();

                var blog = context.Blogs.First(b => b.Title == title);

                context.Posts.Add(new Post() { BlogId = blog.Id, Blog = blog, Title = title, Content = guid });
                context.SaveChanges();

                var post = context.Posts.First(b => b.Content == guid);

                var audits = context.Audits.Where(a => a.Title == title).OrderBy(a => a.AuditDate).ToList();

                Assert.AreEqual(2, audits.Count);
                Assert.AreEqual("Blog", audits[0].TableName);
                Assert.AreEqual(blog.Id, audits[0].TablePK);
                Assert.AreEqual(blog.Title, audits[0].Title);

                Assert.AreEqual("Post", audits[1].TableName);
                Assert.AreEqual(post.Id, audits[1].TablePK);
                Assert.AreEqual(post.Title, audits[1].Title);

                Assert.AreEqual(Environment.UserName, audits[0].AuditUsername);
                Assert.AreEqual(Environment.UserName, audits[1].AuditUsername);
            }

        }

    }
}
