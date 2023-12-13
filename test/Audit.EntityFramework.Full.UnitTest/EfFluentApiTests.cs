using System.Data.Entity;
using Audit.Core;
using Audit.EntityFramework;
using Audit.EntityFramework.Providers;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Different.Audit.EntityFramework.Full.UnitTest.Context
{
    [TestFixture]
    public class EfFluentApiTests
    {
        [Test]
        [Category("EF")]
        public void Test_EfDataProvider_FluentApi()
        {
            var ctx = new OtherContextFromDbContext();
            var x = new EntityFrameworkDataProvider(_ => _
                .UseDbContext(ev => ctx)
                .AuditTypeExplicitMapper(cfg => cfg
                    .Map<Blog_1, AuditBlog_1>()
                    .Map<Post_1, AuditPost_1>())
                .IgnoreMatchedProperties(t => t == typeof(string)));

            Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(typeof(string)));
            Assert.AreEqual(false, x.IgnoreMatchedPropertiesFunc(typeof(int)));
            Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
            Assert.AreEqual(typeof(AuditBlog_1), x.AuditTypeMapper.Invoke(typeof(Blog_1), null));
            Assert.AreEqual(typeof(AuditPost_1), x.AuditTypeMapper.Invoke(typeof(Post_1), null));
            Assert.AreEqual(null, x.AuditTypeMapper.Invoke(typeof(AuditBlog_1), null));
        }

        [Test]
        [Category("EF")]
        public void Test_EfDataProvider_FluentApi2()
        {
            var ctx = new OtherContextFromDbContext();
            var x = new EntityFrameworkDataProvider(_ => _
                .UseDbContext(ev => ctx)
                .AuditTypeMapper(t => typeof(AuditBlog_1))
                .AuditEntityAction((ev, ent, obj) =>
                {
                    return (bool)(((dynamic)obj).Id == 1);
                })
                .IgnoreMatchedProperties(true));


            Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(null));
            Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
            Assert.AreEqual(typeof(AuditBlog_1), x.AuditTypeMapper.Invoke(typeof(Blog_1), null));
            Assert.AreEqual(typeof(AuditBlog_1), x.AuditTypeMapper.Invoke(typeof(Post_1), null));
            Assert.AreEqual(true, x.AuditEntityAction.Invoke(new AuditEvent(), new EventEntry(), new { Id = 1 }).Result);
        }

        [Test]
        [Category("EF")]
        public void Test_EfDataProvider_FluentApi3()
        {
            var ctx = new OtherContextFromDbContext();
            var x = new EntityFrameworkDataProvider(_ => _
                .UseDbContext(ev => ctx)
                .AuditTypeNameMapper(s => "Audit" + s)
                .AuditEntityAction((ev, ent, obj) =>
                {
                    return (bool)(((dynamic)obj).Id == 1);
                })
                .IgnoreMatchedProperties(true));


            Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(null));
            Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
            Assert.AreEqual(typeof(AuditBlog_1), x.AuditTypeMapper.Invoke(typeof(Blog_1), null));
            Assert.AreEqual(typeof(AuditPost_1), x.AuditTypeMapper.Invoke(typeof(Post_1), null));
            Assert.AreEqual(true, x.AuditEntityAction.Invoke(new AuditEvent(), new EventEntry(), new { Id = 1 }).Result);
        }

        [Test]
        [Category("EF")]
        public void Test_EfDataProvider_FluentApi4()
        {
            var ctx = new OtherContextFromDbContext();
            var x = new EntityFrameworkDataProvider(_ => _
                .UseDbContext(ev => ctx)
                .AuditTypeExplicitMapper(cfg => cfg
                    .Map<Blog_1>(entry => entry.Action == "Update" ? typeof(AuditPost_1) : typeof(AuditBlog_1))
                    .Map<Post_1, AuditPost_1>())
                .IgnoreMatchedProperties(true));

            Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(null));
            Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
            Assert.AreEqual(typeof(AuditPost_1), x.AuditTypeMapper.Invoke(typeof(Blog_1), new EventEntry() { Action = "Update" }));
            Assert.AreEqual(typeof(AuditBlog_1), x.AuditTypeMapper.Invoke(typeof(Blog_1), new EventEntry() { Action = "Insert" }));
            Assert.AreEqual(typeof(AuditPost_1), x.AuditTypeMapper.Invoke(typeof(Post_1), null));
            Assert.AreEqual(null, x.AuditTypeMapper.Invoke(typeof(AuditBlog_1), null));
        }
    }

    public class OtherContextFromDbContext : DbContext
    {

    }
    public class Post_1
    {
    }

    public class Blog_1
    {
    }

    public class AuditBlog_1
    {
    }

    public class AuditPost_1
    {
    }
}
