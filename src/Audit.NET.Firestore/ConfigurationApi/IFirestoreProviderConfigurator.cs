using System;
using Audit.Core;
using Google.Cloud.Firestore;

namespace Audit.Firestore.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the Firestore data provider
    /// </summary>
    public interface IFirestoreProviderConfigurator
    {
        /// <summary>
        /// Specifies the Google Cloud Project ID.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        IFirestoreProviderConfigurator ProjectId(string projectId);
        
        /// <summary>
        /// Specifies the Firestore database name. Default is "(default)".
        /// </summary>
        /// <param name="database">The database name.</param>
        IFirestoreProviderConfigurator Database(string database);
        
        /// <summary>
        /// Specifies the Firestore collection name.
        /// </summary>
        /// <param name="collection">The collection name.</param>
        IFirestoreProviderConfigurator Collection(string collection);
        
        /// <summary>
        /// Specifies the Firestore collection name using a function.
        /// </summary>
        /// <param name="collectionBuilder">The collection name builder function.</param>
        IFirestoreProviderConfigurator Collection(Func<AuditEvent, string> collectionBuilder);
        
        /// <summary>
        /// Specifies the credentials to use from a JSON file path.
        /// </summary>
        /// <param name="credentialsFilePath">The path to the credentials JSON file.</param>
        IFirestoreProviderConfigurator CredentialsFromFile(string credentialsFilePath);
        
        /// <summary>
        /// Specifies the credentials to use from a JSON string.
        /// </summary>
        /// <param name="credentialsJson">The credentials JSON string.</param>
        IFirestoreProviderConfigurator CredentialsFromJson(string credentialsJson);
        
        /// <summary>
        /// Specifies a custom FirestoreDb instance to use.
        /// </summary>
        /// <param name="firestoreDb">The FirestoreDb instance.</param>
        IFirestoreProviderConfigurator FirestoreDb(FirestoreDb firestoreDb);
        
        /// <summary>
        /// Specifies a function that returns the document ID to use for a given audit event.
        /// By default, it will generate a new document ID automatically.
        /// </summary>
        /// <param name="idBuilder">The ID builder function.</param>
        IFirestoreProviderConfigurator IdBuilder(Func<AuditEvent, string> idBuilder);
        
        /// <summary>
        /// Specifies whether to sanitize field names by replacing dots with underscores.
        /// Default is false.
        /// </summary>
        /// <param name="sanitizeFieldNames">Whether to sanitize field names.</param>
        IFirestoreProviderConfigurator SanitizeFieldNames(bool sanitizeFieldNames);
    }
} 