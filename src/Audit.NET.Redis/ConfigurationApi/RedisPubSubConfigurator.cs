using System;
using Audit.Core;

namespace Audit.Redis.Configuration
{
    internal class RedisPubSubConfigurator : IRedisPubSubConfigurator
    {
        internal Func<AuditEvent, string> _channelBuilder;

        public IRedisPubSubConfigurator Channel(string channel)
        {
            _channelBuilder = ev => channel;
            return this;
        }

        public IRedisPubSubConfigurator Channel(Func<AuditEvent, string> channelBuilder)
        {
            _channelBuilder = channelBuilder;
            return this;
        }
    }
}