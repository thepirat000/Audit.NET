using System;
using Audit.MongoClient.ConfigurationApi;
using MongoDB.Driver;

namespace Audit.MongoClient
{
    public static class MongoClientSettingsExtensions
    {
        /// <summary>
        /// Adds an Event Subscriber to the MongoClientSetting's ClusterBuilder, using the given audit configuration.
        /// </summary>
        /// <param name="clientSettings">The client settings instance</param>
        /// <param name="config">The audit configuration. Null to use the default configuration</param>
        public static MongoClientSettings AddAuditSubscriber(this MongoClientSettings clientSettings, Action<IAuditMongoConfigurator> config = null)
        {

            if (clientSettings.ClusterConfigurator == null)
            {
                clientSettings.ClusterConfigurator = cc => cc.AddAuditSubscriber(config);
            }
            else
            {
                throw new ArgumentException("Adding an Audit Subscriber to MongoClientSettings is not possible due to an existing ClusterConfigurator. Instead, utilize the AddAuditSubscriber function provided by the ClusterBuilder.");
            }
            return clientSettings;
        }
    }
}