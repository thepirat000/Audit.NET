using System;
using System.Threading.Tasks;
using Audit.Core;
using StackExchange.Redis;

namespace Audit.Redis.Providers
{
    /// <summary>
    /// Sends the audit events to a Redis PubSub channel
    /// </summary>
    public class RedisProviderPubSub : RedisProviderHandler
    {
        protected Func<AuditEvent, string> ChannelBuilder { get; set; }

        /// <summary>
        /// Creates new redis provider that uses Redis PubSub channel to send the events.
        /// </summary>
        /// <param name="configurationOptions">The redis configuration options.
        /// </param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="channelBuilder">A function that returns the Redis PubSub Channel to use.</param>
        public RedisProviderPubSub(ConfigurationOptions configurationOptions, Func<AuditEvent, byte[]> serializer,
            Func<AuditEvent, string> channelBuilder)
            : base(configurationOptions, null, null, serializer, null, null, null)
        {
            ChannelBuilder = channelBuilder;
        }

        /// <summary>
        /// Creates new redis provider that uses Redis PubSub channel to send the events.
        /// </summary>
        /// <param name="connectionString">The redis connection string. https://stackexchange.github.io/StackExchange.Redis/Configuration
        /// </param>
        /// <param name="serializer">Custom serializer to store/send the data on/to the redis server. Default is the audit event serialized as JSon encoded as UTF-8.</param>
        /// <param name="channelBuilder">A function that returns the Redis PubSub Channel to use.</param>
        public RedisProviderPubSub(string connectionString, Func<AuditEvent, byte[]> serializer,
            Func<AuditEvent, string> channelBuilder)
            : base(connectionString, null, null, serializer, null, null, null)
        {
            ChannelBuilder = channelBuilder;
        }

        public override object Insert(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            Publish(eventId, auditEvent);
            return eventId;
        }

        public override async Task<object> InsertAsync(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            await PublishAsync(eventId, auditEvent);
            return eventId;
        }

        public override void Replace(string key, object subKey, AuditEvent auditEvent)
        {
            // values cannot be updated on a pubsub. This will send a new message.
            Publish((Guid)subKey, auditEvent);
        }

        public override async Task ReplaceAsync(string key, object subKey, AuditEvent auditEvent)
        {
            // values cannot be updated on a pubsub. This will send a new message.
            await PublishAsync((Guid)subKey, auditEvent);
        }

        private void Publish(Guid eventId, AuditEvent auditEvent)
        {
            if (ChannelBuilder == null)
            {
                throw new ArgumentException("The channel was not provided");
            }
            auditEvent.CustomFields[RedisEventIdField] = eventId;
            var channel = ChannelBuilder.Invoke(auditEvent);
            var sub = Context.Value.GetSubscriber();
            var value = GetValue(auditEvent);
            sub.Publish(channel, value);
        }

        private async Task PublishAsync(Guid eventId, AuditEvent auditEvent)
        {
            if (ChannelBuilder == null)
            {
                throw new ArgumentException("The channel was not provided");
            }
            auditEvent.CustomFields[RedisEventIdField] = eventId;
            var channel = ChannelBuilder.Invoke(auditEvent);
            var sub = Context.Value.GetSubscriber();
            var value = GetValue(auditEvent);
            await sub.PublishAsync(channel, value);
        }
    }
}