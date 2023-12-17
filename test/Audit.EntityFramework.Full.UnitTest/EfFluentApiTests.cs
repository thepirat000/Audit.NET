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
        public void Test_EfDataProvider_FluentApi()
        {
            var ctx = new OtherContextFromDbContext();
            var x = new EntityFrameworkDataProvider(_ => _
                .UseDbContext(ev => ctx)
                .AuditTypeExplicitMapper(cfg => cfg
                    .Map<Blog_1, AuditBlog_1>()
                    .Map<Post_1, AuditPost_1>())
                .IgnoreMatchedProperties(t => t == typeof(string)));

            Assert.That(x.IgnoreMatchedPropertiesFunc(typeof(string)), Is.EqualTo(true));
            Assert.That(x.IgnoreMatchedPropertiesFunc(typeof(int)), Is.EqualTo(false));
            Assert.That(x.DbContextBuilder.Invoke(null), Is.EqualTo(ctx));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Blog_1), null), Is.EqualTo(typeof(AuditBlog_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Post_1), null), Is.EqualTo(typeof(AuditPost_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(AuditBlog_1), null), Is.EqualTo(null));
        }

        [Test]
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


            Assert.That(x.IgnoreMatchedPropertiesFunc(null), Is.EqualTo(true));
            Assert.That(x.DbContextBuilder.Invoke(null), Is.EqualTo(ctx));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Blog_1), null), Is.EqualTo(typeof(AuditBlog_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Post_1), null), Is.EqualTo(typeof(AuditBlog_1)));
            Assert.That(x.AuditEntityAction.Invoke(new AuditEvent(), new EventEntry(), new { Id = 1 }).Result, Is.EqualTo(true));
        }

        [Test]
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


            Assert.That(x.IgnoreMatchedPropertiesFunc(null), Is.EqualTo(true));
            Assert.That(x.DbContextBuilder.Invoke(null), Is.EqualTo(ctx));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Blog_1), null), Is.EqualTo(typeof(AuditBlog_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Post_1), null), Is.EqualTo(typeof(AuditPost_1)));
            Assert.That(x.AuditEntityAction.Invoke(new AuditEvent(), new EventEntry(), new { Id = 1 }).Result, Is.EqualTo(true));
        }

        [Test]
        public void Test_EfDataProvider_FluentApi4()
        {
            var ctx = new OtherContextFromDbContext();
            var x = new EntityFrameworkDataProvider(_ => _
                .UseDbContext(ev => ctx)
                .AuditTypeExplicitMapper(cfg => cfg
                    .Map<Blog_1>(entry => entry.Action == "Update" ? typeof(AuditPost_1) : typeof(AuditBlog_1))
                    .Map<Post_1, AuditPost_1>())
                .IgnoreMatchedProperties(true));

            Assert.That(x.IgnoreMatchedPropertiesFunc(null), Is.EqualTo(true));
            Assert.That(x.DbContextBuilder.Invoke(null), Is.EqualTo(ctx));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Blog_1), new EventEntry() { Action = "Update" }), Is.EqualTo(typeof(AuditPost_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Blog_1), new EventEntry() { Action = "Insert" }), Is.EqualTo(typeof(AuditBlog_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(Post_1), null), Is.EqualTo(typeof(AuditPost_1)));
            Assert.That(x.AuditTypeMapper.Invoke(typeof(AuditBlog_1), null), Is.EqualTo(null));
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
