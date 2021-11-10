﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.EntityFramework.UnitTest
{
    [TestFixture]
    [Category("Stress")]
    public class EfStressTests
    {
        private object _locker = new object();
        private List<EntityFrameworkEvent> _events = new List<EntityFrameworkEvent>();
        private List<string> _blogTitleList = new List<string>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    lock (_locker)
                    {
                        _events.Add(ev.GetEntityFrameworkEvent());
                    }
                }));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<Entities>().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EF_MultiThread()
        {
            int N = 50;
            _blogTitleList.Clear();
            var tasks = new List<Task>();
            for(int i = 0; i < N; i++)
            {
                tasks.Add(Task.Run(() => SaveFromOneThread()));
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(N*2, _events.Count);
            Assert.AreEqual(N, _blogTitleList.Count);
        }

        private void SaveFromOneThread()
        {
            var blogTitle = Guid.NewGuid().ToString().Substring(0, 20);
            using (var ctx = new BlogsEntities())
            {
                var blog = new Blog()
                {
                    BloggerName = Guid.NewGuid().ToString().Substring(0, 20),
                    Title = blogTitle
                };
                
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();

                var blogId = ctx.Blogs.First(x => x.Title == blogTitle).Id;

                for (int i = 0; i < 5; i++)
                {
                    ctx.Posts.Add(new Post()
                    {
                        BlogId = blogId,
                        DateCreated = DateTime.UtcNow,
                        Title = Guid.NewGuid().ToString().Substring(0, 20),
                        Content = Guid.NewGuid().ToString()
                    });
                }
                ctx.SaveChanges();
            }
            lock(_locker)
            {
                _blogTitleList.Add(blogTitle);
            }
        }
    }
}
