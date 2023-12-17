using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Redis.Providers;
using NUnit.Framework;
using StackExchange.Redis;

namespace Audit.Redis.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("Redis")]
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
            var evFromApi = Core.Configuration.DataProviderAs<RedisDataProvider>().GetEvent(key);
            
            var aev = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(value);
            Core.Configuration.ResetCustomActions();
            db.KeyDelete(key);

            Assert.That(ids.Count, Is.EqualTo(2));
            Assert.That(ids[1], Is.EqualTo(ids[0]));
            Assert.That(aev.EventType, Is.EqualTo(evFromApi.EventType));
            Assert.That(aev.StartDate, Is.EqualTo(evFromApi.StartDate));
            Assert.That(aev.EndDate, Is.EqualTo(evFromApi.EndDate));
            Assert.That(aev.EventType, Is.EqualTo("Redis_String_Basic"));
            Assert.That(Core.Configuration.JsonAdapter.ToObject<List<int>>(aev.CustomFields["custom"]), Is.EqualTo(new List<int>() { 1, 2, 3, 4, 5 }));
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

            Assert.That(value1.HasValue, Is.True);
            Assert.That(value2.HasValue, Is.True);
            Assert.That(value2.ToString(), Is.EqualTo("test"));
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
            var evFromApi = await Core.Configuration.DataProvider.GetEventAsync(key);

            var aev = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(value);
            await db.KeyDeleteAsync(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(2));
            Assert.That(ids[1], Is.EqualTo(ids[0]));
            Assert.That(aev.EventType, Is.EqualTo(evFromApi.EventType));
            Assert.That(aev.StartDate, Is.EqualTo(evFromApi.StartDate));
            Assert.That(aev.EndDate, Is.EqualTo(evFromApi.EndDate));
            Assert.That(aev.EventType, Is.EqualTo("Redis_String_Basic_Async"));
            Assert.That(Core.Configuration.JsonAdapter.ToObject<List<int>>(aev.CustomFields["custom"]), Is.EqualTo(new List<int>() { 1, 2, 3, 4, 5 }));
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
            var aev = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(value);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);
            var value2 = db.StringGet(key);

            Assert.IsFalse(exists);
            Assert.IsFalse(value2.HasValue);

            Assert.That(aev.EventType, Is.EqualTo("Redis_String_Ttl"));
            Assert.That(Core.Configuration.JsonAdapter.ToObject<List<int>>(aev.CustomFields["custom"]), Is.EqualTo(new List<int>() { 2, 3, 4, 5 }));
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
            var evFromApi = Core.Configuration.DataProvider.GetEvent(ids[0]);
            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(2));
            Assert.That(evFromApi.EventType, Is.EqualTo("deserializer test"));
            Assert.That(value, Is.EqualTo(new byte[] { 1, 2, 4, 8 }));
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
            var aev1 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);
            var aev2 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3]);
            var evFromApi = Core.Configuration.DataProvider.GetEvent(ids[0]);
            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(4));
            Assert.That(values.Length, Is.EqualTo(4));
            Assert.That(aev2.EventType, Is.EqualTo(evFromApi.EventType));
            Assert.That(aev2.StartDate, Is.EqualTo(evFromApi.StartDate));
            Assert.That(aev1.EventType, Is.EqualTo("Redis_List_Basic_2"));
            Assert.That(aev2.EventType, Is.EqualTo("Redis_List_Basic_1"));
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

            Assert.That(value1.HasValue, Is.True);
            Assert.That(value2.HasValue, Is.True);
            Assert.That(value2.ToString(), Is.EqualTo("test"));
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
            var aev1 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);
            var aev2 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3]);
            var evFromApi = await Core.Configuration.DataProvider.GetEventAsync(ids[0]);
            await db.KeyDeleteAsync(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(4));
            Assert.That(values.Length, Is.EqualTo(4));
            Assert.That(aev2.EventType, Is.EqualTo(evFromApi.EventType));
            Assert.That(aev2.StartDate, Is.EqualTo(evFromApi.StartDate));
            Assert.That(aev1.EventType, Is.EqualTo("Redis_List_Basic_2"));
            Assert.That(aev2.EventType, Is.EqualTo("Redis_List_Basic_1"));
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
            var aev = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);

            Assert.IsFalse(exists);

            Assert.That(aev.EventType, Is.EqualTo("Redis_List_Ttl"));
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

            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That((byte[])values[0], Is.EqualTo(new byte[] { 1, 2, 4, 8 }));
            Assert.That((byte[])values[1], Is.EqualTo(new byte[] { 1, 2, 4, 8 }));
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

            Assert.That(values.Length, Is.EqualTo(3));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]).EventType, Is.EqualTo("4"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1]).EventType, Is.EqualTo("3"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[2]).EventType, Is.EqualTo("2"));
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
            var evFromApi = Core.Configuration.DataProvider.GetEvent(ids[0]);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(6));
            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That(evFromApi.EventType, Is.EqualTo("Redis_Hash_Basic_1"));
            Assert.That(v1.HasValue, Is.True);
            Assert.That(v2.HasValue, Is.True);
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(v1).CustomFields["test"].ToString(), Is.EqualTo("updated"));
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

            Assert.That(value1.HasValue, Is.True);
            Assert.That(value2.HasValue, Is.True);
            Assert.That(value2.ToString(), Is.EqualTo("test"));
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
            var evFromApi = await Core.Configuration.DataProvider.GetEventAsync(ids[0]);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(6));
            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That(evFromApi.EventType, Is.EqualTo("Redis_Hash_Basic_1"));
            Assert.That(v1.HasValue, Is.True);
            Assert.That(v2.HasValue, Is.True);
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(v1).CustomFields["test"].ToString(), Is.EqualTo("updated"));
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
            var aev = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(value);

            Task.Delay(5500).Wait();

            var exists2 = db.KeyExists(key);

            Assert.That(exists1, Is.True);
            Assert.IsFalse(exists2);
            Assert.That(aev.EventType, Is.EqualTo("Redis_Hash_Ttl"));
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

            Assert.That((byte[])value, Is.EqualTo(random));
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
            var evFromApi = Core.Configuration.DataProvider.GetEvent(ids[0]);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(4));
            Assert.That(values.Length, Is.EqualTo(4));
            Assert.That(values[0].Score, Is.EqualTo(-56.78));
            Assert.That(evFromApi.EventType, Is.EqualTo("Redis_SortedSet_Basic_1"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType, Is.EqualTo("Redis_SortedSet_Basic_2"));
            Assert.That(values[3].Score, Is.EqualTo(12.34));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3].Element).EventType, Is.EqualTo("Redis_SortedSet_Basic_1"));
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

            Assert.That(value1.HasValue, Is.True);
            Assert.That(value2.HasValue, Is.True);
            Assert.That(value2.ToString(), Is.EqualTo("test"));
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
            var evFromApi = await Core.Configuration.DataProvider.GetEventAsync(ids[0]);

            await db.KeyDeleteAsync(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(ids.Count, Is.EqualTo(4));
            Assert.That(values.Length, Is.EqualTo(4));
            Assert.That(values[0].Score, Is.EqualTo(-56.78));
            Assert.That(evFromApi.EventType, Is.EqualTo("Redis_SortedSet_Basic_1"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType, Is.EqualTo("Redis_SortedSet_Basic_2"));
            Assert.That(values[3].Score, Is.EqualTo(12.34));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[3].Element).EventType, Is.EqualTo("Redis_SortedSet_Basic_1"));
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
            var aev = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);

            Assert.IsFalse(exists);
            Assert.That(aev.EventType, Is.EqualTo("Redis_SortedSet_Ttl"));
        }

        [Test, Order(10)]
        public void Redis_SortedSet_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            object id;
            var random = new byte[] { 15, 4, 9, 22, 10, 14 };
            var deserialize = new AuditEvent() { EventType = "test 123" };
            Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;

            var dp = new RedisDataProviderHelper(RedisCnnString, ev => random, b => deserialize)
                .AsSortedSet(s => s
                    .Key(ev => key)
                    .Score(ev => 1));

            Core.Configuration.Setup().Use(dp);

            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp }))
            {
                scope.Save();
                id = scope.EventId;
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByScore(key, 1, 1);

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That((byte[])values[0], Is.EqualTo(random));
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

            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That(values[0].Score, Is.EqualTo(-100));
            Assert.That(values[1].Score, Is.EqualTo(-56.78));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByScore_3"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByScore_2"));
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

            Assert.That(values.Length, Is.EqualTo(3));
            Assert.That(values[0].Score, Is.EqualTo(-50.55));
            Assert.That(values[1].Score, Is.EqualTo(12.34));
            Assert.That(values[2].Score, Is.EqualTo(142857.77));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByRank_4"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByRank_1"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[2].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByRank_5"));
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

            Assert.That(values.Length, Is.EqualTo(3));
            Assert.That(values[0].Score, Is.EqualTo(-987.65));
            Assert.That(values[1].Score, Is.EqualTo(-100.00));
            Assert.That(values[2].Score, Is.EqualTo(-50.55));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByRank_2"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByRank_3"));
            Assert.That(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[2].Element).EventType, Is.EqualTo("Redis_SortedSet_CappedByRank_4"));
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
            subs.Subscribe(new RedisChannel("mychannel:audit", RedisChannel.PatternMode.Literal), (ch, x) =>
            {
                list.Add(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(x));
            });

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_1" })) {}

            Task.Delay(500).Wait();

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_2" })) {}

            Task.Delay(1000).Wait();

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].EventType, Is.EqualTo("Redis_PubSub_Basic_1"));
            Assert.That(list[1].EventType, Is.EqualTo("Redis_PubSub_Basic_2"));
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
            await subs.SubscribeAsync(new RedisChannel("mychannel:audit", RedisChannel.PatternMode.Literal), (ch, x) =>
            {
                list.Add(Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(x));
            });

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_1" })) { await scope.DisposeAsync(); }
            
            await Task.Delay(500);

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_2" })) { await scope.DisposeAsync(); }

            await Task.Delay(1000);

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].EventType, Is.EqualTo("Redis_PubSub_Basic_1"));
            Assert.That(list[1].EventType, Is.EqualTo("Redis_PubSub_Basic_2"));
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
            subs.Subscribe(new RedisChannel("Redis_PubSub_CustomSerializer:channel", RedisChannel.PatternMode.Literal), (ch, x) =>
            {
                list.Add(x);
            });

            using (var scope = AuditScope.Create(new AuditScopeOptions() { })) { }

            Task.Delay(500).Wait();

            var db = mx.GetDatabase();
            db.KeyDelete(key);
            Assert.That((byte[])list[0], Is.EqualTo(random));
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
            var values = db.ListRange(key).Select(x => Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(x)).ToList();

            db.KeyDelete(key);

            Assert.That(values.Count, Is.EqualTo(N));
            for (int a = 0; a < N; a++)
            {
                Assert.That(values.Any(x => x.EventType == $"Redis_Multithread_{a}"), Is.True);
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
            var values = (await db.ListRangeAsync(key)).Select(x => Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(x)).ToList();

            await db.KeyDeleteAsync(key);

            Assert.That(values.Count, Is.EqualTo(N));
            for (int a = 0; a < N; a++)
            {
                Assert.That(values.Any(x => x.EventType == $"Redis_Multithread_{a}"), Is.True);
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
                Assert.That(ae.Message.ToLower().Contains("redis key"), Is.True);
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
                Assert.That(ae.Message.ToLower().Contains("hash field"), Is.True);
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
                Assert.That(ae.Message.ToLower().Contains("score builder"), Is.True);
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
                Assert.That(ae.Message.ToLower().Contains("channel"), Is.True);
            }

        }

        [Test, Order(10)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public void Redis_Stream_Basic(int dbIndex)
        {
            var ids = new List<object>();
            var key = Guid.NewGuid().ToString();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConfigurationOptions(ConfigurationOptions.Parse(RedisCnnString))
                    .AsStream(s => s
                        .Key(ev => key)
                        .Database(dbIndex)
                        .DefaultAuditEventFieldName("MyAuditEvent")
                        .WithCustomField("EventType", ev => ev.EventType)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(_ => _.OnEventSaved(scope =>
                {
                    ids.Add(scope.EventId);
                }));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_Stream_Basic_1" }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_Stream_Basic_2" }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase(dbIndex);
            var values = db.StreamRange(key);

            Assert.That(ids.Count, Is.EqualTo(2));
            Assert.That(values.Length, Is.EqualTo(2));

            var evType1 = values[0]["EventType"].ToString();
            var evType2 = values[1]["EventType"].ToString();

            var aev1 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]["MyAuditEvent"]);
            var aev2 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1]["MyAuditEvent"]);

            var evFromApi1 = Core.Configuration.DataProvider.GetEvent(ids[0]);
            var evFromApi2 = Core.Configuration.DataProvider.GetEvent(ids[1]);
            var evFromApiNotExists = Core.Configuration.DataProvider.GetEvent("0-0");

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(evFromApiNotExists, Is.Null);
            Assert.That(aev1.EventType, Is.EqualTo(evType1));
            Assert.That(aev2.EventType, Is.EqualTo(evType2));
            Assert.That(aev1.EventType, Is.EqualTo(evFromApi1.EventType));
            Assert.That(aev1.StartDate, Is.EqualTo(evFromApi1.StartDate));
            Assert.That(aev2.EventType, Is.EqualTo(evFromApi2.EventType));
            Assert.That(aev2.StartDate, Is.EqualTo(evFromApi2.StartDate));
            Assert.That(aev1.EventType, Is.EqualTo("Redis_Stream_Basic_1"));
            Assert.That(aev2.EventType, Is.EqualTo("Redis_Stream_Basic_2"));
        }

        [Test, Order(10)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        public async Task Redis_Stream_BasicAsync(int dbIndex)
        {
            var ids = new List<object>();
            var key = Guid.NewGuid().ToString();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConfigurationOptions(ConfigurationOptions.Parse(RedisCnnString))
                    .AsStream(s => s
                        .Key(ev => key)
                        .Database(dbIndex)
                        .DefaultAuditEventFieldName("MyAuditEvent")
                        .WithCustomField("EventType", ev => ev.EventType)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(_ => _.OnEventSaved(scope =>
                {
                    ids.Add(scope.EventId);
                }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_Stream_Basic_1" }))
            {
            }
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { EventType = "Redis_Stream_Basic_2" }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase(dbIndex);
            var values = await db.StreamRangeAsync(key);

            Assert.That(ids.Count, Is.EqualTo(2));
            Assert.That(values.Length, Is.EqualTo(2));

            var evType1 = values[0]["EventType"].ToString();
            var evType2 = values[1]["EventType"].ToString();

            var aev1 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[0]["MyAuditEvent"]);
            var aev2 = Core.Configuration.JsonAdapter.Deserialize<AuditEvent>(values[1]["MyAuditEvent"]);

            var evFromApi1 = await Core.Configuration.DataProvider.GetEventAsync(ids[0]);
            var evFromApi2 = await Core.Configuration.DataProvider.GetEventAsync(ids[1]);
            var evFromApiNotExists = await Core.Configuration.DataProvider.GetEventAsync("0-0");

            await db.KeyDeleteAsync(key);
            Core.Configuration.ResetCustomActions();

            Assert.That(evFromApiNotExists, Is.Null);
            Assert.That(aev1.EventType, Is.EqualTo(evType1));
            Assert.That(aev2.EventType, Is.EqualTo(evType2));
            Assert.That(aev1.EventType, Is.EqualTo(evFromApi1.EventType));
            Assert.That(aev1.StartDate, Is.EqualTo(evFromApi1.StartDate));
            Assert.That(aev2.EventType, Is.EqualTo(evFromApi2.EventType));
            Assert.That(aev2.StartDate, Is.EqualTo(evFromApi2.StartDate));
            Assert.That(aev1.EventType, Is.EqualTo("Redis_Stream_Basic_1"));
            Assert.That(aev2.EventType, Is.EqualTo("Redis_Stream_Basic_2"));
        }

        [Test, Order(10)]
        public void Redis_Stream_MaxLen()
        {
            var ids = new List<object>();
            var key = Guid.NewGuid().ToString();
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConfigurationOptions(ConfigurationOptions.Parse(RedisCnnString))
                    .AsStream(s => s
                        .Key(ev => key)
                        .MaxLength(5, false)
                        .WithCustomField("EventType", ev => ev.EventType)))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(_ => _.OnEventSaved(scope =>
                {
                    ids.Add(scope.EventId);
                }));

            for (int i = 0; i < 10; i++)
            {
                AuditScope.Log($"Redis_Stream_MaxLen_{i}", new { number = i });
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.StreamRange(key);

            Assert.That(ids.Count, Is.EqualTo(10));
            Assert.That(values.Length, Is.EqualTo(5));

            db.KeyDelete(key);
            Core.Configuration.ResetCustomActions();
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
