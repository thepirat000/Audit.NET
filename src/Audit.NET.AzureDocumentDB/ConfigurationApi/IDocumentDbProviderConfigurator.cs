using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

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
        /// Specifies the Azure DocumentDB Client.
        /// </summary>
        /// <param name="documentClient">The configured document client object.</param>
        IDocumentDbProviderConfigurator DocumentClient(IDocumentClient documentClient);
    }
}