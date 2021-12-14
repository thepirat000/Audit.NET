using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Redis.Providers;
using NUnit.Framework;
using StackExchange.Redis;

namespace Audit.IntegrationTest
{
    public class RedisTests
    {
        private const string RedisCnnString = "localhost:6379,allowAdmin=true";

        [Test, Order(10)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public void Redis_String_Basic(int dbIndex)
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsString(s => s
                        .Key(ev => key)
                        .Database(_ => dbIndex)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .WithAction(_ => _.OnEventSaved(scope =>
                {
                    ids.Add(scope.EventId);
                }));
            using (var scope = AuditScope.Create(new AuditScopeOptions()
                {
                    EventType = "Redis_String_Basic"
                }))
            {
                scope.SetCustomField("custom", new List<int>() { 1,2,3,4,5});
            }
            
            var mx = GetMultiplexer();
            var db = mx.GetDatabase(dbIndex);
            var value = db.StringGet(key);
            var evFromApi = (Audit.Core.Configuration.DataProvider as RedisDataProvider).GetEvent(key);
            
            var aev = Configuration.JsonAdapter.Deserialize<AuditEvent>(value);
            Configuration.ResetCustomActions();
            db.KeyDelete(key);

            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(ids[0], ids[1]);
            Assert.AreEqual(evFromApi.EventType, aev.EventType);
            Assert.AreEqual(evFromApi.StartDate, aev.StartDate);
            Assert.AreEqual(evFromApi.EndDate, aev.EndDate);
            Assert.AreEqual("Redis_String_Basic", aev.EventType);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, Configuration.JsonAdapter.ToObject<List<int>>(aev.CustomFields["custom"]));
        }

        [Test]
        public void Redis_String_ExtraTasks()
        {
            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsString(s => s
                        .Key(ev => key)
                        .AttachTask(batch => batch.StringSetAsync(key2, "test"))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_String_ExtraTasks"
            });
            scope.Dispose();

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value1 = db.StringGet(key);
            var value2 = db.StringGet(key2);

            db.KeyDelete(key);
            db.KeyDelete(key2);

            Assert.IsTrue(value1.HasValue);
            Assert.IsTrue(value2.HasValue);
            Assert.AreEqual("test", value2.ToString());
        }

        [Test, Order(10)]
        public async Task Redis_String_Basic_Async()
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsString(s => s
                        .Key(ev => key)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .WithAction(_ => _.OnEventSaved(scope =>
                {
                    ids.Add(scope.EventId);
                }));
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                EventType = "Redis_String_Basic_Async"
            }))
            {
                scope.SetCustomField("custom", new List<int>() { 1, 2, 3, 4, 5 });
                await scope.DisposeAsync();
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value = await db.StringGetAsync(key);
            var evFromApi = await Configuration.DataProvider.GetEventAsync(key);

            var aev = Configuration.JsonAdapter.Deserialize<AuditEvent>(value);
            await db.KeyDeleteAsync(key);
            Configuration.ResetCustomActions();

            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual(ids[0], ids[1]);
            Assert.AreEqual(evFromApi.EventType, aev.EventType);
            Assert.AreEqual(evFromApi.StartDate, aev.StartDate);
            Assert.AreEqual(evFromApi.EndDate, aev.EndDate);
            Assert.AreEqual("Redis_String_Basic_Async", aev.EventType);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, Configuration.JsonAdapter.ToObject<List<int>>(aev.CustomFields["custom"]));
        }

        [Test, Order(10)]
        public void Redis_String_Ttl()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsString(s => s
                        .Key(ev => key)
                        .TimeToLive(TimeSpan.FromSeconds(5))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_String_Ttl"
            }))
            {
                scope.SetCustomField("custom", new List<int>() { 2, 3, 4, 5 });
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value = db.StringGet(key);
            var aev = Configuration.JsonAdapter.Deserialize<AuditEvent>(value);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);
            var value2 = db.StringGet(key);

            Assert.IsFalse(exists);
            Assert.IsFalse(value2.HasValue);

            Assert.AreEqual("Redis_String_Ttl", aev.EventType);
            Assert.AreEqual(new List<int>() { 2, 3, 4, 5 }, Configuration.JsonAdapter.ToObject<List<int>>(aev.CustomFields["custom"]));
        }

        [Test, Order(10)]
        public void Redis_String_CustomSerializer()
        {
            var ids = new List<object>();
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .Serializer(ev => new byte[] { 1, 2, 4, 8 })
                    .Deserializer(b => new AuditEvent() { EventType = "deserializer test" })
                    .AsString(s => s
                        .Key(ev => key)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .WithAction(_ => _.OnEventSaved(scope =>
                {
                    ids.Add(scope.EventId);
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value = (byte[])db.StringGet(key);
            var evFromApi = Configuration.DataProvider.GetEvent(ids[0]);
            db.KeyDelete(key);
            Configuration.ResetCustomActions();

            Assert.AreEqual(2, ids.Count);
            Assert.AreEqual("deserializer test", evFromApi.EventType);
            Assert.AreEqual(new byte[] { 1, 2, 4, 8 }, value);
        }

        [Test, Order(10)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public void Redis_List_Basic(int dbIndex)
        {
            var ids = new List<object>();
            var key = Guid.NewGuid().ToString();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsList(s => s
                        .Key(ev => key)
                        .Database(dbIndex)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                 .WithAction(_ => _.OnEventSaved(scope =>
                 {
                     ids.Add(scope.EventId);
                 }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_List_Basic_1" }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_List_Basic_2" }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase(dbIndex);
            var values = db.ListRange(key);
            var aev1 = Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);
            var aev2 = Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3]);
            var evFromApi = Configuration.DataProvider.GetEvent(ids[0]);
            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.AreEqual(4, ids.Count);
            Assert.AreEqual(4, values.Length);
            Assert.AreEqual(evFromApi.EventType, aev2.EventType);
            Assert.AreEqual(evFromApi.StartDate, aev2.StartDate);
            Assert.AreEqual("Redis_List_Basic_2", aev1.EventType);
            Assert.AreEqual("Redis_List_Basic_1", aev2.EventType);
        }

        [Test]
        public void Redis_List_ExtraTasks()
        {
            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsList(l => l
                        .Key(ev => key)
                        .AttachTask(batch => batch.StringSetAsync(key2, "test"))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var scope = AuditScope.Create("Redis_List_ExtraTasks", null);
            scope.Dispose();

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value1 = db.ListGetByIndex(key, 0);
            var value2 = db.StringGet(key2);

            db.KeyDelete(key);
            db.KeyDelete(key2);

            Assert.IsTrue(value1.HasValue);
            Assert.IsTrue(value2.HasValue);
            Assert.AreEqual("test", value2.ToString());
        }

        [Test, Order(10)]
        public async Task Redis_List_Basic_Async()
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsList(s => s
                        .Key(ev => key)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                 .WithAction(_ => _.OnEventSaved(scope =>
                 {
                     ids.Add(scope.EventId);
                 }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_List_Basic_1" }))
            {
                await scope.DisposeAsync();
            }
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_List_Basic_2" }))
            {
                await scope.DisposeAsync();
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = await db.ListRangeAsync(key);
            var aev1 = Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);
            var aev2 = Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3]);
            var evFromApi = await Configuration.DataProvider.GetEventAsync(ids[0]);
            await db.KeyDeleteAsync(key);
            Core.Configuration.ResetCustomActions();

            Assert.AreEqual(4, ids.Count);
            Assert.AreEqual(4, values.Length);
            Assert.AreEqual(evFromApi.EventType, aev2.EventType);
            Assert.AreEqual(evFromApi.StartDate, aev2.StartDate);
            Assert.AreEqual("Redis_List_Basic_2", aev1.EventType);
            Assert.AreEqual("Redis_List_Basic_1", aev2.EventType);
        }

        [Test, Order(10)]
        public void Redis_List_Ttl()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsList(s => s
                        .Key(ev => key)
                        .TimeToLive(TimeSpan.FromSeconds(5))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_List_Ttl"
            }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key);
            var aev = Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);

            Assert.IsFalse(exists);

            Assert.AreEqual("Redis_List_Ttl", aev.EventType);
        }

        [Test, Order(10)]
        public void Redis_List_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .Serializer(ev => new byte[] { 1, 2, 4, 8 })
                    .AsList(s => s
                        .Key(ev => key)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var scope = AuditScope.Create(new AuditScopeOptions() { }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key);

            db.KeyDelete(key);

            Assert.AreEqual(2, values.Length);
            Assert.AreEqual(new byte[] { 1, 2, 4, 8 }, (byte[])values[0]);
            Assert.AreEqual(new byte[] { 1, 2, 4, 8 }, (byte[])values[1]);
        }

        [Test, Order(10)]
        public void Redis_List_MaxLen()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsList(s => s
                        .Key(ev => key)
                        .MaxLength(3)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            for (int i = 0; i < 5; i++)
            {
                using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = i.ToString() })) { }
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key);

            db.KeyDelete(key);

            Assert.AreEqual(3, values.Length);
            Assert.AreEqual("4", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]).EventType);
            Assert.AreEqual("3", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1]).EventType);
            Assert.AreEqual("2", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[2]).EventType);
        }

        [Test, Order(10)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public void Redis_Hash_Basic(int dbIndex)
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.ResetCustomActions();

            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsHash(h => h
                        .Key(ev => key)
                        .HashField(ev => ev.EventType)
                        .Database(dbIndex)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                 .WithAction(_ => _.OnEventSaved(scope =>
                 {
                     ids.Add(scope.EventId);
                 }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_Hash_Basic_1" }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_Hash_Basic_2" }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_Hash_Basic_1", ExtraFields = new { test = "updated" }}))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase(dbIndex);
            var values = db.HashGetAll(key);
            var v1 = db.HashGet(key, "Redis_Hash_Basic_1");
            var v2 = db.HashGet(key, "Redis_Hash_Basic_2");
            var evFromApi = Configuration.DataProvider.GetEvent(ids[0]);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.AreEqual(6, ids.Count);
            Assert.AreEqual(2, values.Length);
            Assert.AreEqual("Redis_Hash_Basic_1", evFromApi.EventType);
            Assert.IsTrue(v1.HasValue);
            Assert.IsTrue(v2.HasValue);
            Assert.AreEqual("updated", Configuration.JsonAdapter.Deserialize<AuditEvent>(v1).CustomFields["test"].ToString());
        }

        [Test]
        public void Redis_Hash_ExtraTasks()
        {
            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsHash(h => h
                        .Key(ev => key)
                        .HashField(ev => ev.EventType)
                        .AttachTask(batch => batch.StringSetAsync(key2, "test"))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var scope = AuditScope.Create("Redis_Hash_ExtraTasks", null);
            scope.Dispose();

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value1 = db.HashGet(key, "Redis_Hash_ExtraTasks");
            var value2 = db.StringGet(key2);

            db.KeyDelete(key);
            db.KeyDelete(key2);

            Assert.IsTrue(value1.HasValue);
            Assert.IsTrue(value2.HasValue);
            Assert.AreEqual("test", value2.ToString());
        }

        [Test, Order(10)]
        public async Task Redis_Hash_Basic_Async()
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.ResetCustomActions();

            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsHash(h => h
                        .Key(ev => key)
                        .HashField(ev => ev.EventType)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                 .WithAction(_ => _.OnEventSaved(scope =>
                 {
                     ids.Add(scope.EventId);
                 }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_Hash_Basic_1" }))
            {
                await scope.DisposeAsync();
            }
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_Hash_Basic_2" }))
            {
                await scope.DisposeAsync();
            }
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_Hash_Basic_1", ExtraFields = new { test = "updated" } }))
            {
                await scope.DisposeAsync();
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.HashGetAll(key);
            var v1 = db.HashGet(key, "Redis_Hash_Basic_1");
            var v2 = db.HashGet(key, "Redis_Hash_Basic_2");
            var evFromApi = await Configuration.DataProvider.GetEventAsync(ids[0]);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.AreEqual(6, ids.Count);
            Assert.AreEqual(2, values.Length);
            Assert.AreEqual("Redis_Hash_Basic_1", evFromApi.EventType);
            Assert.IsTrue(v1.HasValue);
            Assert.IsTrue(v2.HasValue);
            Assert.AreEqual("updated", Configuration.JsonAdapter.Deserialize<AuditEvent>(v1).CustomFields["test"].ToString());
        }

        [Test, Order(10)]
        public void Redis_Hash_Ttl()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsHash(s => s
                        .Key(ev => key)
                        .HashField(ev => ev.EventType)
                        .TimeToLive(TimeSpan.FromSeconds(5))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_Hash_Ttl"
            }))
            {
            }

            var exists1 = db.KeyExists(key);
            var value = db.HashGet(key, "Redis_Hash_Ttl");
            var aev = Configuration.JsonAdapter.Deserialize<AuditEvent>(value);

            Task.Delay(5500).Wait();

            var exists2 = db.KeyExists(key);

            Assert.IsTrue(exists1);
            Assert.IsFalse(exists2);
            Assert.AreEqual("Redis_Hash_Ttl", aev.EventType);
        }

        [Test, Order(10)]
        public void Redis_Hash_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            var random = new byte[] {15, 4, 9, 22, 10, 14};
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .Serializer(ev => random)
                    .AsHash(s => s
                        .Key(ev => key)
                        .HashField(ev => "x1")))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd); 

            using (var scope = AuditScope.Create(new AuditScopeOptions() { }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value = db.HashGet(key, "x1");

            db.KeyDelete(key);

            Assert.AreEqual(random, (byte[])value);
        }

        [Test, Order(10)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public void Redis_SortedSet_Basic(int dbIndex)
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(h => h
                        .Key(ev => key)
                        .Score(ev => (double)ev.CustomFields["Score"])
                        .Database(dbIndex)))
                 .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                 .WithAction(_ => _.OnEventSaved(scope =>
                 {
                     ids.Add(scope.EventId);
                 }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_Basic_1", ExtraFields = new { Score = 12.34 }}))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_Basic_2", ExtraFields = new { Score = -56.78 } }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase(dbIndex);
            var values = db.SortedSetRangeByRankWithScores(key);
            var evFromApi = Configuration.DataProvider.GetEvent(ids[0]);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.AreEqual(4, ids.Count);
            Assert.AreEqual(4, values.Length);
            Assert.AreEqual(-56.78, values[0].Score);
            Assert.AreEqual("Redis_SortedSet_Basic_1", evFromApi.EventType);
            Assert.AreEqual("Redis_SortedSet_Basic_2", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual(12.34, values[3].Score);
            Assert.AreEqual("Redis_SortedSet_Basic_1", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3].Element).EventType);
        }

        [Test]
        public void Redis_SortedSet_ExtraTasks()
        {
            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(ss => ss
                        .Key(ev => key)
                        .Score(ev => 1)
                        .AttachTask(batch => batch.StringSetAsync(key2, "test"))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var scope = AuditScope.Create("Redis_SortedSet_ExtraTasks", null);
            scope.Dispose();

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value1 = db.SortedSetRangeByScore(key, 1, 1).FirstOrDefault();
            var value2 = db.StringGet(key2);

            db.KeyDelete(key);
            db.KeyDelete(key2);

            Assert.IsTrue(value1.HasValue);
            Assert.IsTrue(value2.HasValue);
            Assert.AreEqual("test", value2.ToString());
        }

        [Test, Order(10)]
        public async Task Redis_SortedSet_Basic_Async()
        {
            var key = Guid.NewGuid().ToString();
            var ids = new List<object>();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(h => h
                        .Key(ev => key)
                        .Score(ev => (double)ev.CustomFields["Score"])))
                 .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                 .WithAction(_ => _.OnEventSaved(scope =>
                 {
                     ids.Add(scope.EventId);
                 }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_SortedSet_Basic_1", ExtraFields = new { Score = 12.34 } }))
            {
                await scope.DisposeAsync();
            }
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_SortedSet_Basic_2", ExtraFields = new { Score = -56.78 } }))
            {
                await scope.DisposeAsync();
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = await db.SortedSetRangeByRankWithScoresAsync(key);
            var evFromApi = await Configuration.DataProvider.GetEventAsync(ids[0]);

            await db.KeyDeleteAsync(key);
            Core.Configuration.ResetCustomActions();

            Assert.AreEqual(4, ids.Count);
            Assert.AreEqual(4, values.Length);
            Assert.AreEqual(-56.78, values[0].Score);
            Assert.AreEqual("Redis_SortedSet_Basic_1", evFromApi.EventType);
            Assert.AreEqual("Redis_SortedSet_Basic_2", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual(12.34, values[3].Score);
            Assert.AreEqual("Redis_SortedSet_Basic_1", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3].Element).EventType);
        }

        [Test, Order(10)]
        public void Redis_SortedSet_Ttl()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(s => s
                        .Key(ev => key)
                        .Score(ev => ev.StartDate.Ticks)
                        .TimeToLive(TimeSpan.FromSeconds(5))))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_SortedSet_Ttl"
            }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByRank(key);
            var aev = Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);

            Assert.IsFalse(exists);
            Assert.AreEqual("Redis_SortedSet_Ttl", aev.EventType);
        }

        [Test, Order(10)]
        public void Redis_SortedSet_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            object id;
            var random = new byte[] { 15, 4, 9, 22, 10, 14 };
            var deserialize = new AuditEvent() { EventType = "test 123" };
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var dp = new RedisDataProviderHelper(RedisCnnString, ev => random, b => deserialize)
                .AsSortedSet(s => s
                    .Key(ev => key)
                    .Score(ev => 1));

            Configuration.Setup().Use(dp);

            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp }))
            {
                scope.Save();
                id = scope.EventId;
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByScore(key, 1, 1);

            db.KeyDelete(key);
            Configuration.ResetCustomActions();

            Assert.AreEqual(random, (byte[])values[0]);
        }

        [Test, Order(10)]
        public void Redis_SortedSet_CappedByScore()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(h => h
                        .Key(ev => key)
                        .Score(ev => (double)ev.CustomFields["Score"])
                        .MaxScore(12.34, true)
                        .MinScore(-100)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByScore_1", ExtraFields = new { Score = 12.34 } }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByScore_2", ExtraFields = new { Score = -56.78 } }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByScore_3", ExtraFields = new { Score = -100.00 } }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByRankWithScores(key);

            db.KeyDelete(key);

            Assert.AreEqual(2, values.Length);
            Assert.AreEqual(-100, values[0].Score);
            Assert.AreEqual(-56.78, values[1].Score);
            Assert.AreEqual("Redis_SortedSet_CappedByScore_3", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByScore_2", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1].Element).EventType);
        }

        [Test, Order(10)]
        public void Redis_SortedSet_CappedByRank_Top()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(h => h
                        .Key(ev => key)
                        .Score(ev => (double)ev.CustomFields["Score"])
                        .MaxRank(3)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_1", ExtraFields = new { Score = 12.34 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_2", ExtraFields = new { Score = -987.65 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_3", ExtraFields = new { Score = -100.00 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_4", ExtraFields = new { Score = -50.55 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_5", ExtraFields = new { Score = 142857.77 } })) { }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByRankWithScores(key);

            db.KeyDelete(key);

            Assert.AreEqual(3, values.Length);
            Assert.AreEqual(-50.55, values[0].Score);
            Assert.AreEqual(12.34, values[1].Score);
            Assert.AreEqual(142857.77, values[2].Score);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_4", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_1", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_5", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[2].Element).EventType);
        }

        [Test, Order(10)]
        public void Redis_SortedSet_CappedByRank_Bottom()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(h => h
                        .Key(ev => key)
                        .Score(ev => (double)ev.CustomFields["Score"])
                        .MaxRank(-3)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_1", ExtraFields = new { Score = 12.34 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_2", ExtraFields = new { Score = -987.65 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_3", ExtraFields = new { Score = -100.00 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_4", ExtraFields = new { Score = -50.55 } })) { }
            using (AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_CappedByRank_5", ExtraFields = new { Score = 142857.77 } })) { }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByRankWithScores(key);

            db.KeyDelete(key);

            Assert.AreEqual(3, values.Length);
            Assert.AreEqual(-987.65, values[0].Score);
            Assert.AreEqual(-100.00, values[1].Score);
            Assert.AreEqual(-50.55, values[2].Score);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_2", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_3", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_4", Configuration.JsonAdapter.Deserialize<AuditEvent>(values[2].Element).EventType);
        }

        [Test, Order(10)]
        public void Redis_PubSub_Basic()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsPubSub(h => h
                        .Channel("mychannel:audit")))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var mx = GetMultiplexer();
            var list = new List<AuditEvent>();
            var subs = mx.GetSubscriber();
            subs.Subscribe("mychannel:audit", (ch, x) =>
            {
                list.Add(Configuration.JsonAdapter.Deserialize<AuditEvent>(x));
            });

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_1" })) {}

            Task.Delay(500).Wait();

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_2" })) {}

            Task.Delay(1000).Wait();

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("Redis_PubSub_Basic_1", list[0].EventType);
            Assert.AreEqual("Redis_PubSub_Basic_2", list[1].EventType);
        }

        [Test, Order(10)]
        public async Task Redis_PubSub_Basic_Async()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsPubSub(h => h
                        .Channel("mychannel:audit")))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var mx = GetMultiplexer();
            var list = new List<AuditEvent>();
            var subs = mx.GetSubscriber();
            await subs.SubscribeAsync("mychannel:audit", (ch, x) =>
            {
                list.Add(Configuration.JsonAdapter.Deserialize<AuditEvent>(x));
            });

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_1" })) { await scope.DisposeAsync(); }
            
            await Task.Delay(500);

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_2" })) { await scope.DisposeAsync(); }

            await Task.Delay(1000);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("Redis_PubSub_Basic_1", list[0].EventType);
            Assert.AreEqual("Redis_PubSub_Basic_2", list[1].EventType);
        }

        [Test, Order(10)]
        public void Redis_PubSub_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            var random = new byte[] { 15, 4, 9, 22, 10, 14, 17, 8 };
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .Serializer(ev => random)
                    .AsPubSub(s => s
                        .Channel(ev => "Redis_PubSub_CustomSerializer:channel")))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var mx = GetMultiplexer();
            var list = new List<RedisValue>();
            var subs = mx.GetSubscriber();
            subs.Subscribe("Redis_PubSub_CustomSerializer:channel", (ch, x) =>
            {
                list.Add(x);
            });

            using (var scope = AuditScope.Create(new AuditScopeOptions() { })) { }

            Task.Delay(500).Wait();

            var db = mx.GetDatabase();
            db.KeyDelete(key);
            Assert.AreEqual(random, (byte[])list[0]);
        }

        [Test, Order(int.MaxValue)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(25)]
        public void Redis_Multithread(int N)
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString + ",connectTimeout=120000")
                    .AsList(s => s
                        .Key(ev => key)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var tasks = new List<Task>(N);
            for (int i = 0; i < N; i++)
            {
                int a = i;
                tasks.Add(Task.Run(() =>
                {
                    using (var scope = AuditScope.Create(new AuditScopeOptions() {EventType = $"Redis_Multithread_{a}"}))
                    {
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key).Select(x => Configuration.JsonAdapter.Deserialize<AuditEvent>(x)).ToList();

            db.KeyDelete(key);

            Assert.AreEqual(N, values.Count);
            for (int a = 0; a < N; a++)
            {
                Assert.IsTrue(values.Any(x => x.EventType == $"Redis_Multithread_{a}"));
            }
        }

        [Test, Order(int.MaxValue)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(25)]
        public async Task Redis_Multithread_Async(int N)
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString + ",connectTimeout=120000")
                    .AsList(s => s
                        .Key(ev => key)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var tasks = new List<Task>(N);
            for (int i = 0; i < N; i++)
            {
                int a = i;
                tasks.Add(Task.Run(async () =>
                {
                    using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = $"Redis_Multithread_{a}" }))
                    {
                        await scope.DisposeAsync();
                    }
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = (await db.ListRangeAsync(key)).Select(x => Configuration.JsonAdapter.Deserialize<AuditEvent>(x)).ToList();

            await db.KeyDeleteAsync(key);

            Assert.AreEqual(N, values.Count);
            for (int a = 0; a < N; a++)
            {
                Assert.IsTrue(values.Any(x => x.EventType == $"Redis_Multithread_{a}"));
            }
        }

        [Test, Order(10)]
        public void Redis_Errors()
        {
            //string 
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsString(_ => { }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            try
            {
                using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = $"Redis_Errors" })) {}
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.ToLower().Contains("redis key"));
            }

            //hash
            key = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsHash(_ => _.Key("petete")))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            try
            {
                using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = $"Redis_Errors" })){}
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.ToLower().Contains("hash field"));
            }

            //ss
            key = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(_ => _.Key("potato")))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            try
            {
                using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = $"Redis_Errors" })) { }
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.ToLower().Contains("score builder"));
            }

            //pubsub
            key = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsPubSub(_ => { }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            try
            {
                using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = $"Redis_Errors" })) { }
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.ToLower().Contains("channel"));
            }

        }

        private ConnectionMultiplexer _multiplexer;

        private ConnectionMultiplexer GetMultiplexer()
        {
            if (_multiplexer == null)
            {
                _multiplexer = ConnectionMultiplexer.Connect(RedisCnnString);
            }
            return _multiplexer;
        }
    }
}
