﻿using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core.Providers.Wrappers;
using Moq;
using System;

namespace Audit.UnitTest
{
    public class DataProviderTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }
        
        [Test]
        public void Test_DataProviderDeferredFactory()
        {
            int getProviderCount = 0;

            IAuditDataProvider GetProvider()
            {
                getProviderCount++;
                return new NullDataProvider();
            }
            
            var options = new AuditScopeOptions(cfg => cfg.DataProviderDeferredFactory(ev => GetProvider()).CreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd));
            Assert.That(getProviderCount, Is.EqualTo(0));
            using (var scope = AuditScope.Create(options))
            {
                Assert.That(getProviderCount, Is.EqualTo(1));
                scope.SetCustomField("custom", "value");
                scope.Save();
                Assert.That(getProviderCount, Is.EqualTo(2));
            }
            Assert.That(getProviderCount, Is.EqualTo(3));
            options = new AuditScopeOptions(_ => _.DataProviderDeferredFactory(ev => GetProvider()).CreationPolicy(EventCreationPolicy.Manual));
            using (var scope = new AuditScope(options))
            {
                Assert.That(getProviderCount, Is.EqualTo(3));
                scope.Save();
                scope.Save();
            }
            Assert.That(getProviderCount, Is.EqualTo(5));
            Audit.Core.Configuration.DataProviderFactory = () => new DeferredDataProvider(ev => GetProvider());
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            {
                Assert.That(getProviderCount, Is.EqualTo(5));
                scope.Discard();
            }
            Assert.That(getProviderCount, Is.EqualTo(5));

            Audit.Core.Configuration.Setup().UseDeferredFactory(_ => GetProvider());
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            {
                Assert.That(getProviderCount, Is.EqualTo(5));
            }
            Assert.That(getProviderCount, Is.EqualTo(6));
        }

        [Test]
        public void Test_DataProviderLazyFactory_ByConfigurator()
        {
            int getProviderCount = 0;

            IAuditDataProvider GetProvider()
            {
                getProviderCount++;
                return new NullDataProvider();
            }

            var options = new AuditScopeOptions(cfg => cfg.DataProviderLazyFactory(() => GetProvider()).CreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd));

            using (var scope = AuditScope.Create(options))
            {
                Assert.That(getProviderCount, Is.EqualTo(1));
                scope.SetCustomField("custom", "value");
                scope.Save();
            }
            
            Assert.That(getProviderCount, Is.EqualTo(1));
        }
        
        [Test]
        public void Test_DataProviderInLazyFactory_BySetup()
        {
            int getProviderCount = 0;

            IAuditDataProvider GetProvider()
            {
                getProviderCount++;
                return new NullDataProvider();
            }
            
            Audit.Core.Configuration.Setup().UseLazyFactory(GetProvider);
            Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            using (var scope = AuditScope.Create("Test", null, new { custom = "value" }))
            {
                scope.Save();
            }

            Assert.That(getProviderCount, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_DataProvider_CancellationToken(bool cancel)
        {
            using var cs = new CancellationTokenSource();
            var events_inserted = new List<AuditEvent>();
            var events_replaced = new List<AuditEvent>();
            bool tokenCancelled_inserted = !cancel;
            bool tokenCancelled_replaced = !cancel;

            var dp = new DynamicAsyncDataProvider(c => c.OnInsert(async (ev, ct) =>
            {
                events_inserted.Add(AuditEvent.FromJson(ev.ToJson()));
                tokenCancelled_inserted = ct.IsCancellationRequested;
                await Task.CompletedTask;
            }).OnReplace(async (id, ev, ct) =>
            {
                events_replaced.Add(AuditEvent.FromJson(ev.ToJson()));
                tokenCancelled_replaced = ct.IsCancellationRequested;
                await Task.CompletedTask;
            }));

            if (cancel)
            {
                cs.Cancel();
            }

            var options = new AuditScopeOptions()
            {
                DataProvider = dp,
                CreationPolicy = EventCreationPolicy.Manual,
                EventType = "Test",
                ExtraFields = new { f = 1 }
            };

            var scope = await AuditScope.CreateAsync(options, cs.Token);
            await scope.SaveAsync(cs.Token);

            scope.SetCustomField("f", 2);
            await scope.SaveAsync(cs.Token);

            Assert.That(tokenCancelled_inserted, Is.EqualTo(cancel));
            Assert.That(tokenCancelled_replaced, Is.EqualTo(cancel));
            Assert.That(events_inserted.Count, Is.EqualTo(1));
            Assert.That(events_replaced.Count, Is.EqualTo(1));
            Assert.That(events_inserted[0].CustomFields["f"].ToString(), Is.EqualTo("1"));
            Assert.That(events_replaced[0].CustomFields["f"].ToString(), Is.EqualTo("2"));
        }

        [Test]
        public void Test_DataProvider_CloneValue_String()
        {
            // Arrange
            var dataProvider = new Mock<AuditDataProvider>(MockBehavior.Loose);
            dataProvider.CallBase = true;


            // Act
            var value = "StringValue";

            var cloned = dataProvider.Object.CloneValue<string>(value, null);

            // Assert
            Assert.That(cloned, Is.EqualTo(value));
        }

        [Test]
        public void Test_DataProvider_CloneValue_Null()
        {
            // Arrange
            var dataProvider = new Mock<AuditDataProvider>(MockBehavior.Loose);
            dataProvider.CallBase = true;

            // Act
            string value = null;

            var cloned = dataProvider.Object.CloneValue<string>(value, null);

            // Assert
            Assert.That(cloned, Is.Null);
        }

        [Test]
        public void Test_DataProvider_CloneValue_ICloneable()
        {
            // Arrange
            var dataProvider = new Mock<AuditDataProvider>(MockBehavior.Loose);
            dataProvider.CallBase = true;


            // Act
            var value = new MyValue() { Id = 1 };
            var cloned = dataProvider.Object.CloneValue<MyValue>(value, null);

            // Assert
            Assert.That(cloned, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cloned, Is.TypeOf<MyValue>());
                Assert.That(((MyValue)cloned).Status, Is.EqualTo("Cloned"));
            });
        }

        [Test]
        public void Test_DataProvider_CloneValue_Json()
        {
            // Arrange
            var dataProvider = new Mock<AuditDataProvider>(MockBehavior.Loose);
            dataProvider.CallBase = true;

            // Act
            var value = new List<string>() { "1", "2" };
            var cloned = dataProvider.Object.CloneValue<List<string>>(value, null);

            // Assert
            Assert.That(cloned, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cloned, Is.TypeOf<List<string>>());
                Assert.That(((List<string>)cloned).Count, Is.EqualTo(2));
            });
        }

        [Test]
        public void Test_DataProvider_CloneValue_Primitive()
        {
            // Arrange
            var dataProvider = new Mock<AuditDataProvider>(MockBehavior.Loose);
            dataProvider.CallBase = true;

            // Act
            var value = 1000L;
            var cloned = dataProvider.Object.CloneValue<long>(value, null);

            // Assert
            Assert.That(cloned, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(cloned, Is.TypeOf<long>());
                Assert.That((long)cloned, Is.EqualTo(value));
            });
        }

        public class MyValue : ICloneable
        {
            public int Id { get; set; }
            public string Status { get; set; }
            public object Clone()
            {
                return new MyValue() { Id = this.Id, Status = "Cloned" };
            }
        }

    }
}
