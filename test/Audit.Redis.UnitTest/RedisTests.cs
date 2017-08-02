using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Redis.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Audit.IntegrationTest
{
    public class RedisTests
    {
        private const string RedisCnnString = "localhost:6379,allowAdmin=true";

        [Test, Order(10)]
        public void Redis_String_Basic()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsString(s => s
                        .Key(ev => key)));

            using (var scope = AuditScope.Create(new AuditScopeOptions()
                {
                    EventType = "Redis_String_Basic"
                }))
            {
                scope.SetCustomField("custom", new List<int>() { 1,2,3,4,5});
            }
            
            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value = db.StringGet(key);
            var aev = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(value);
            db.KeyDelete(key);

            Assert.AreEqual("Redis_String_Basic", aev.EventType);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5 }, (aev.CustomFields["custom"] as JToken).ToObject<List<int>>());
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
                        .TimeToLive(TimeSpan.FromSeconds(5))));

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
            var aev = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(value);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);
            var value2 = db.StringGet(key);

            Assert.IsFalse(exists);
            Assert.IsFalse(value2.HasValue);

            Assert.AreEqual("Redis_String_Ttl", aev.EventType);
            Assert.AreEqual(new List<int>() { 2, 3, 4, 5 }, (aev.CustomFields["custom"] as JToken).ToObject<List<int>>());
        }

        [Test, Order(10)]
        public void Redis_String_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .Serializer(ev => new byte[] { 1, 2, 4, 8 })
                    .AsString(s => s
                        .Key(ev => key)));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var value = (byte[])db.StringGet(key);

            db.KeyDelete(key);

            Assert.AreEqual(new byte[] { 1, 2, 4, 8 }, value);
        }

        [Test, Order(10)]
        public void Redis_List_Basic()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsList(s => s
                        .Key(ev => key)));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_List_Basic_1" }))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_List_Basic_2" }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key);
            var aev1 = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(values[0]);
            var aev2 = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(values[1]);
            db.KeyDelete(key);

            Assert.AreEqual(2, values.Length);
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
                        .TimeToLive(TimeSpan.FromSeconds(5))));

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_List_Ttl"
            }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key);
            var aev = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(values[0]);

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
                        .Key(ev => key)));

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
                        .MaxLength(3)));

            for (int i = 0; i < 5; i++)
            {
                using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = i.ToString() })) { }
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.ListRange(key);

            db.KeyDelete(key);

            Assert.AreEqual(3, values.Length);
            Assert.AreEqual("4", JsonConvert.DeserializeObject<AuditEvent>(values[0]).EventType);
            Assert.AreEqual("3", JsonConvert.DeserializeObject<AuditEvent>(values[1]).EventType);
            Assert.AreEqual("2", JsonConvert.DeserializeObject<AuditEvent>(values[2]).EventType);
        }

        [Test, Order(10)]
        public void Redis_Hash_Basic()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsHash(h => h
                        .Key(ev => key)
                        .HashField(ev => ev.EventType)));

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
            var db = mx.GetDatabase();
            var values = db.HashGetAll(key);
            var v1 = db.HashGet(key, "Redis_Hash_Basic_1");
            var v2 = db.HashGet(key, "Redis_Hash_Basic_2");

            db.KeyDelete(key);
            
            Assert.AreEqual(2, values.Length);
            Assert.IsTrue(v1.HasValue);
            Assert.IsTrue(v2.HasValue);
            Assert.AreEqual("updated", JsonConvert.DeserializeObject<AuditEvent>(v1).CustomFields["test"]);
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
                        .TimeToLive(TimeSpan.FromSeconds(5))));

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
            var aev = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(value);

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
                        .HashField(ev => "x1")));

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
        public void Redis_SortedSet_Basic()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsSortedSet(h => h
                        .Key(ev => key)
                        .Score(ev => (double)ev.CustomFields["Score"])));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_Basic_1", ExtraFields = new { Score = 12.34 }}))
            {
            }
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_SortedSet_Basic_2", ExtraFields = new { Score = -56.78 } }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByRankWithScores(key);

            db.KeyDelete(key);

            Assert.AreEqual(2, values.Length);
            Assert.AreEqual(-56.78, values[0].Score);
            Assert.AreEqual("Redis_SortedSet_Basic_2", JsonConvert.DeserializeObject<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual(12.34, values[1].Score);
            Assert.AreEqual("Redis_SortedSet_Basic_1", JsonConvert.DeserializeObject<AuditEvent>(values[1].Element).EventType);
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
                        .TimeToLive(TimeSpan.FromSeconds(5))));

            using (var scope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = "Redis_SortedSet_Ttl"
            }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByRank(key);
            var aev = Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEvent>(values[0]);

            Task.Delay(5500).Wait();

            var exists = db.KeyExists(key);

            Assert.IsFalse(exists);
            Assert.AreEqual("Redis_SortedSet_Ttl", aev.EventType);
        }

        [Test, Order(10)]
        public void Redis_SortedSet_CustomSerializer()
        {
            var key = Guid.NewGuid().ToString();
            var random = new byte[] { 15, 4, 9, 22, 10, 14 };

            var dp = new RedisDataProviderHelper(RedisCnnString, ev => random)
                .AsSortedSet(s => s
                    .Key(ev => key)
                    .Score(ev => 1));

            using (var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = dp }))
            {
            }

            var mx = GetMultiplexer();
            var db = mx.GetDatabase();
            var values = db.SortedSetRangeByScore(key, 1, 1);

            db.KeyDelete(key);

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
                        .MinScore(-100)));

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
            Assert.AreEqual("Redis_SortedSet_CappedByScore_3", JsonConvert.DeserializeObject<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByScore_2", JsonConvert.DeserializeObject<AuditEvent>(values[1].Element).EventType);
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
                        .MaxRank(3)));

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
            Assert.AreEqual("Redis_SortedSet_CappedByRank_4", JsonConvert.DeserializeObject<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_1", JsonConvert.DeserializeObject<AuditEvent>(values[1].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_5", JsonConvert.DeserializeObject<AuditEvent>(values[2].Element).EventType);
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
                        .MaxRank(-3)));

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
            Assert.AreEqual("Redis_SortedSet_CappedByRank_2", JsonConvert.DeserializeObject<AuditEvent>(values[0].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_3", JsonConvert.DeserializeObject<AuditEvent>(values[1].Element).EventType);
            Assert.AreEqual("Redis_SortedSet_CappedByRank_4", JsonConvert.DeserializeObject<AuditEvent>(values[2].Element).EventType);
        }

        [Test, Order(10)]
        public void Redis_PubSub_Basic()
        {
            var key = Guid.NewGuid().ToString();
            Core.Configuration.Setup()
                .UseRedis(redis => redis
                    .ConnectionString(RedisCnnString)
                    .AsPubSub(h => h
                        .Channel("mychannel:audit")));

            var mx = GetMultiplexer();
            var list = new List<AuditEvent>();
            var subs = mx.GetSubscriber();
            subs.Subscribe("mychannel:audit", (ch, x) =>
            {
                list.Add(JsonConvert.DeserializeObject<AuditEvent>(x));
            });

            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_1" })) {}
            using (var scope = AuditScope.Create(new AuditScopeOptions() { EventType = "Redis_PubSub_Basic_2" })) { }

            Task.Delay(1000).Wait();

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
                        .Channel(ev => "Redis_PubSub_CustomSerializer:channel")));

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
                        .Key(ev => key)));

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
            var values = db.ListRange(key).Select(x => JsonConvert.DeserializeObject<AuditEvent>(x)).ToList();

            db.KeyDelete(key);

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
                    .AsString(_ => { })); // no key provided
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
                    .AsHash(_ => _.Key("petete"))); // no hash field provided
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
                    .AsSortedSet(_ => _.Key("potato"))); // no score builder provided
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
                    .AsPubSub(_ => { })); // no channel provided
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
