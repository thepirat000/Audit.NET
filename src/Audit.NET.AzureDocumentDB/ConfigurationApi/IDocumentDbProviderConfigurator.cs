using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;

namespace Audit.AzureDocumentDB.ConfigurationApi
{
    public interface IDocumentDbProviderConfigurator
    {
        /// <summary>
        /// Specifies the Azure Document DB connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        IDocumentDbProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the Azure Document DB database name.
        /// </summary>
        /// <param name="database">The database name.</param>
        IDocumentDbProviderConfigurator Database(string database);
        /// <summary>
        /// Specifies the Azure Document DB collection name.
        /// </summary>
        /// <param name="collection">The collection name.</param>
        IDocumentDbProviderConfigurator Collection(string collection);
        /// <summary>
        /// Specifies the Azure Document DB Auth Key.
        /// </summary>
        /// <param name="authKey">The auth key.</param>
        IDocumentDbProviderConfigurator AuthKey(string authKey);
        /// <summary>
        /// Specifies the Azure DocumentDB Client Connection Policy
        /// </summary>
        /// <param name="connectionPolicy">The connection policy.</param>
        IDocumentDbProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy);
        /// <summary>
        /// Specifies the Azure Document DB connection string builder.
        /// </summary>
        /// <param name="connectionStringBuilder">The connection string builder.</param>
        IDocumentDbProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder);
        /// <summary>
        /// Specifies the Azure Document DB database name builder.
        /// </summary>
        /// <param name="databaseBuilder">The database name builder.</param>
        IDocumentDbProviderConfigurator Database(Func<AuditEvent, string> databaseBuilder);
        /// <summary>
        /// Specifies the Azure Document DB collection name builder.
        /// </summary>
        /// <param name="collectionBuilder">The collection name builder.</param>
        IDocumentDbProviderConfigurator Collection(Func<AuditEvent, string> collectionBuilder);
        /// <summary>
        /// Specifies the Azure Document DB Auth Key builder.
        /// </summary>
        /// <param name="authKeyBuilder">The auth key builder.</param>
        IDocumentDbProviderConfigurator AuthKey(Func<AuditEvent, string> authKeyBuilder);
        /// <summary>
        /// Specifies the Azure DocumentDB Client Connection Policy builder
        /// </summary>
        /// <param name="connectionPolicyBuilder">The connection policy builder.</param>
        IDocumentDbProviderConfigurator ConnectionPolicy(Func<AuditEvent, ConnectionPolicy> connectionPolicyBuilder);
        /// <summary>
        /// Specifies the Azure DocumentDB Client.
        /// </summary>
        /// <param name="documentClient">The configured document client object.</param>
        IDocumentDbProviderConfigurator DocumentClient(IDocumentClient documentClient);
    }
}