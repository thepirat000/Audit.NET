using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class EfCore21Tests
    {
        [OneTimeSetUp]
        public void Init()
        {
        }

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EFTransactionScope()
        {
            var list = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                    {
                        list.Add(ev.GetEntityFrameworkEvent());
                    }));
            
            var guid = Guid.NewGuid().ToString();
            Blog blog1;
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew,
                new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted}))
            {

                using (var connection = new SqlConnection(BlogsContext.CnnString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM dbo.Blogs Where Title = '1' OR Title = '2'";
                    command.ExecuteNonQuery();
                }

                using (var context = new BlogsContext())
                {
                    context.Blogs.Add(new Blog {BloggerName = guid, Title = "1"});
                    context.SaveChanges();

                    context.Blogs.Add(new Blog {BloggerName = guid, Title = "2"});
                    context.SaveChanges();

                    blog1 = context.Blogs
                        .FirstOrDefault(b => b.BloggerName == guid);

                    scope.Complete();

                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        context.Blogs.FirstOrDefault();
                    }, "Should have been thrown InvalidOperationException since scope is completed");
                }
            }
            
            Assert.AreEqual(2, list.Count);
            Assert.IsNotNull(blog1);
            Assert.IsTrue(list[0].AmbientTransactionId.Length > 2);
            Assert.AreEqual(list[0].AmbientTransactionId, list[1].AmbientTransactionId);
        }
    }
}