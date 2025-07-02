using System;
using Audit.Core;
using Google.Cloud.Firestore;
// ReSharper disable InconsistentNaming

namespace Audit.Firestore.ConfigurationApi
{
    /// <summary>
    /// Configurator for Firestore data provider
    /// </summary>
    public class FirestoreProviderConfigurator : IFirestoreProviderConfigurator
    {
        internal string _projectId;
        internal string _database = "(default)";
        internal Setting<string> _collection = "AuditEvents";
        internal string _credentialsFilePath;
        internal string _credentialsJson;
        internal Func<FirestoreDb> _firestoreDbFactory;
        internal Func<AuditEvent, string> _idBuilder;
        internal bool _sanitizeFieldNames = false;
        internal bool _excludeNullValues = false;

        public IFirestoreProviderConfigurator ProjectId(string projectId)
        {
            _projectId = projectId;
            return this;
        }

        public IFirestoreProviderConfigurator Database(string database)
        {
            _database = database;
            return this;
        }

        public IFirestoreProviderConfigurator Collection(string collection)
        {
            _collection = collection;
            return this;
        }

        public IFirestoreProviderConfigurator Collection(Func<AuditEvent, string> collectionBuilder)
        {
            _collection = collectionBuilder;
            return this;
        }

        public IFirestoreProviderConfigurator CredentialsFromFile(string credentialsFilePath)
        {
            _credentialsFilePath = credentialsFilePath;
            _credentialsJson = null;
            return this;
        }

        public IFirestoreProviderConfigurator CredentialsFromJson(string credentialsJson)
        {
            _credentialsJson = credentialsJson;
            _credentialsFilePath = null;
            return this;
        }

        public IFirestoreProviderConfigurator FirestoreDb(FirestoreDb firestoreDb)
        {
            _firestoreDbFactory = () => firestoreDb;
            return this;
        }

        public IFirestoreProviderConfigurator FirestoreDb(Func<FirestoreDb> firestoreDbFactory)
        {
            _firestoreDbFactory = firestoreDbFactory;
            return this;
        }

        public IFirestoreProviderConfigurator IdBuilder(Func<AuditEvent, string> idBuilder)
        {
            _idBuilder = idBuilder;
            return this;
        }

        public IFirestoreProviderConfigurator SanitizeFieldNames(bool sanitizeFieldNames = true)
        {
            _sanitizeFieldNames = sanitizeFieldNames;
            return this;
        }

        public IFirestoreProviderConfigurator ExcludeNullValues(bool excludeNullValues = true)
        {
            _excludeNullValues = excludeNullValues;
            return this;
        }
        
    }
} 