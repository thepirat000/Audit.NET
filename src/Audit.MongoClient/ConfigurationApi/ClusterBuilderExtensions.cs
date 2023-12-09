using System;
using Audit.MongoClient.ConfigurationApi;
using MongoDB.Driver.Core.Configuration;

namespace Audit.MongoClient
{
    public static class ClusterBuilderExtensions
    {
        /// <summary>
        /// Adds an Event Subscriber to the MongoDB ClusterBuilder, using the given audit configuration.
        /// </summary>
        /// <param name="clusterBuilder">The cluster builder</param>
        /// <param name="config">The audit configuration. Null to use the default configuration</param>
        public static ClusterBuilder AddAuditSubscriber(this ClusterBuilder clusterBuilder, Action<IAuditMongoConfigurator> config = null)
        {
            clusterBuilder.Subscribe(new MongoAuditEventSubscriber(config));
            return clusterBuilder;
        }
    }
}