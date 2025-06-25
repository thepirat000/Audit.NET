using System;
using Audit.Firestore.Providers;
using Audit.Core.ConfigurationApi;
using Audit.Firestore.ConfigurationApi;

namespace Audit.Core
{
    public static class FirestoreConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in a Google Cloud Firestore database.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="projectId">The Google Cloud project ID.</param>
        /// <param name="collection">The Firestore collection name.</param>
        /// <param name="database">The Firestore database name. Default is "(default)".</param>
        public static ICreationPolicyConfigurator UseFirestore(this IConfigurator configurator, string projectId,
            string collection = "AuditEvents", string database = "(default)")
        {
            Configuration.DataProvider = new FirestoreDataProvider()
            {
                ProjectId = projectId,
                Collection = collection,
                Database = database
            };

            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in a Google Cloud Firestore database.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="config">The Firestore provider configuration.</param>
        public static ICreationPolicyConfigurator UseFirestore(this IConfigurator configurator, Action<IFirestoreProviderConfigurator> config)
        {
            var firestoreConfig = new FirestoreProviderConfigurator();
            config.Invoke(firestoreConfig);

            Configuration.DataProvider = new FirestoreDataProvider()
            {
                ProjectId = firestoreConfig._projectId,
                Database = firestoreConfig._database,
                Collection = firestoreConfig._collection,
                CredentialsFilePath = firestoreConfig._credentialsFilePath,
                CredentialsJson = firestoreConfig._credentialsJson,
                FirestoreDb = firestoreConfig._firestoreDb,
                IdBuilder = firestoreConfig._idBuilder,
                SanitizeFieldNames = firestoreConfig._sanitizeFieldNames
            };

            return new CreationPolicyConfigurator();
        }
    }
} 