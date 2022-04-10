using Raven.Client.Documents;
using System;
using System.Security.Cryptography.X509Certificates;
using Audit.Core;

namespace Audit.NET.RavenDB.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the RavenDB data provider
    /// </summary>
    public interface IRavenDbProviderConfigurator
    {
        /// <summary>
        /// Specifies a custom document store instance to use.
        /// </summary>
        /// <param name="documentStore">The instance of the document store to use</param>
        void UseDocumentStore(IDocumentStore documentStore);

        /// <summary>
        /// Specifies the settings for the DocumentStore.
        /// </summary>
        /// <param name="documentStoreSettings">Fluent API for the document store settings</param>
        void WithSettings(Action<IRavenDbProviderStoreConfigurator> documentStoreSettings);

        /// <summary>
        /// Specifies the settings for the DocumentStore.
        /// </summary>
        /// <param name="urls">The initial server URLs to connect</param>
        /// <param name="database">The default database name for the audit events. Null to use the databaseFunc</param>
        /// <param name="certificate">The certificate for the secure connection</param>
        /// <param name="databaseFunc">The session level database to use as a function of the audit event. Null to use the default database</param>
        void WithSettings(string[] urls, string database, X509Certificate2 certificate = null, Func<AuditEvent, string> databaseFunc = null);

        /// <summary>
        /// Specifies the settings for the DocumentStore.
        /// </summary>
        /// <param name="url">The initial server URL to connect</param>
        /// <param name="database">The default database name for the audit events. Null to use the databaseFunc</param>
        /// <param name="certificate">The certificate for the secure connection</param>
        /// /// <param name="databaseFunc">The session level database to use as a function of the audit event. Leave null to use the default database</param>
        void WithSettings(string url, string database, X509Certificate2 certificate = null, Func<AuditEvent, string> databaseFunc = null);

    }
}
