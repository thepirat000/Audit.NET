using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Stores the audit events in a Redis Sorted Set
    /// </summary>
    public class RedisProviderSortedSet : RedisProviderHandler
    {
        protected Func<AuditEvent, double> ScoreBuilder { get; set; }
        protected Func<AuditEvent, double> MaxScoreBuilder { get; set; }
        protected bool MaxScoreExclusive { get; set; }
        protected Func<AuditEvent, double> MinScoreBuilder { get; set; }
        protected bool MinScoreExclusive { get; set; }
        protected Func<AuditEvent, long> MaxRankBuilder { get; set; }

        /// <summary>
        /// Creates new redis provider that uses a Redis Sorted Set to store the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="keyBuilder">A function that returns the Redis Key to use</param>
        /// <param name="timeToLive">The Time To Live for the Redis Key. NULL for no TTL.</param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="deserializer">Custom deserializer to retrieve events from the redis server. Default is the audit event deserialized from UTF-8 JSon.</param>
        /// <param name="scoreBuilder">A function that returns the score for the sorted set member.</param>
        /// <param name="maxScoreBuilder">A function that returns the maximum score allowed for the sorted set members.</param>
        /// <param name="maxScoreExclusive">Indicates if the maximum is an Exclusive range. Default is Inclusive.</param>
        /// <param name="minScoreBuilder">A function that returns the minimum score allowed for the sorted set members.</param>
        /// <param name="minScoreExclusive">Indicates if the minimum is an Exclusive range. Default is Inclusive.</param>
        /// <param name="maxRankBuilder">A function that returns the maximum rank allowed for a capped collection. Greater than zero to maintain the top M scored elements. Less than zero to maintain the bottom -M scored elements.</param>
        /// <param name="dbIndexBuilder">A function that returns the database ID to use.</param>
        /// <param name="extraTasks">A list of extra redis commands to execute.</param>
        public RedisProviderSortedSet(string connectionString, Func<AuditEvent, string> keyBuilder,
            TimeSpan? timeToLive, 
            Func<AuditEvent, byte[]> serializer,
            Func<byte[], AuditEvent> deserializer,
            Func<AuditEvent, double> scoreBuilder, Func<AuditEvent, double> maxScoreBuilder = null, bool maxScoreExclusive = false, 
            Func<AuditEvent, double> minScoreBuilder = null, bool minScoreExclusive = false,
            Func<AuditEvent, long> maxRankBuilder = null,
            Func<AuditEvent, int> dbIndexBuilder = null,
            List<Func<IBatch, AuditEvent, Task>> extraTasks = null)
            : base(connectionString, keyBuilder, timeToLive, serializer, deserializer, dbIndexBuilder, extraTasks)
        {
            ScoreBuilder = scoreBuilder;
            MaxScoreBuilder = maxScoreBuilder;
            MinScoreBuilder = minScoreBuilder;
            MaxScoreExclusive = maxScoreExclusive;
            MinScoreExclusive = minScoreExclusive;
            MaxRankBuilder = maxRankBuilder;
        }

        public override object Insert(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            SortedSetAdd(eventId, auditEvent);
            return eventId;
        }

        public override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            await SortedSetAddAsync(eventId, auditEvent);
            return eventId;
        }

        public override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            // SortedSet values cannot be properly updated. This will insert a new member to the list.
            SortedSetAdd((Guid)subKey, auditEvent);
        }

        public override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            // SortedSet values cannot be properly updated. This will insert a new member to the list.
            await SortedSetAddAsync((Guid)subKey, auditEvent);
        }

        public override T Get<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            foreach (var item in db.SortedSetRangeByRank(key))
            {
                if (item.HasValue)
                {
                    var auditEvent = FromValue<T>(item);
                    if (auditEvent != null && subKey.ToString().Equals(auditEvent.CustomFields[RedisEventIdField]?.ToString()))
                    {
                        return auditEvent;
                    }
                }
            }
            return null;
        }

        public override async Task<T> GetAsync<T>(string key, object subKey)
        {
            var db = GetDatabase(null);
            foreach (var item in await db.SortedSetRangeByRankAsync(key))
            {
                if (item.HasValue)
                {
                    var auditEvent = FromValue<T>(item);
                    if (auditEvent != null && subKey.ToString().Equals(auditEvent.CustomFields[RedisEventIdField]?.ToString()))
                    {
                        return auditEvent;
                    }
                }
            }
            return null;
        }

        private void SortedSetAdd(Guid eventId, AuditEvent auditEvent)
        {
            var tasks = ExecSortedSetAdd(eventId, auditEvent);
            Task.WaitAll(tasks);
        }

        private async Task SortedSetAddAsync(Guid eventId, AuditEvent auditEvent)
        {
            var tasks = ExecSortedSetAdd(eventId, auditEvent);
            await Task.WhenAll(tasks);
        }

        private Task[] ExecSortedSetAdd(Guid eventId, AuditEvent auditEvent)
        {
            if (ScoreBuilder == null)
            {
                throw new ArgumentException("The score builder was not provided");
            }
            var score = ScoreBuilder.Invoke(auditEvent);
            auditEvent.CustomFields[RedisEventIdField] = eventId;
            var tasks = new List<Task>();
            var key = GetKey(auditEvent);
            var value = GetValue(auditEvent);
            var batch = GetDatabase(auditEvent).CreateBatch();
            tasks.Add(batch.SortedSetAddAsync(key, value, score));
            if (TimeToLive.HasValue)
            {
                tasks.Add(batch.KeyExpireAsync(key, TimeToLive));
            }
            // trim by scores
            if (MinScoreBuilder != null)
            {
                double stop = MinScoreBuilder.Invoke(auditEvent);
                var exclude = MinScoreExclusive ? Exclude.None : Exclude.Stop;
                tasks.Add(batch.SortedSetRemoveRangeByScoreAsync(key, double.NegativeInfinity, stop, exclude));
            }
            if (MaxScoreBuilder != null)
            {
                double start = MaxScoreBuilder.Invoke(auditEvent);
                var exclude = MaxScoreExclusive ? Exclude.None : Exclude.Start;
                tasks.Add(batch.SortedSetRemoveRangeByScoreAsync(key, start, double.PositiveInfinity, exclude));
            }
            // trim by ranks
            if (MaxRankBuilder != null)
            {
                long max = MaxRankBuilder.Invoke(auditEvent);
                if (max > 0)
                {
                    tasks.Add(batch.SortedSetRemoveRangeByRankAsync(key, 0, -(max + 1)));
                }
                else
                {
                    tasks.Add(batch.SortedSetRemoveRangeByRankAsync(key, -max, -1));
                }
            }
            OnBatchExecuting(batch, tasks, auditEvent);
            batch.Execute();
            return tasks.ToArray();
        }
    }
}