using System;
using Audit.Core;
using Google.Cloud.Firestore;

namespace Audit.Firestore.ConfigurationApi
{
    /// <summary>
    /// Configurator for Firestore data provider
    /// </summary>
    public class FirestoreProviderConfigurator : IFirestoreProviderConfigurator
    {
        internal Setting<string> _projectId;
        internal Setting<string> _database = "(default)";
        internal Setting<string> _collection = "AuditEvents";
        internal string _credentialsFilePath;
        internal string _credentialsJson;
        internal FirestoreDb _firestoreDb;
        internal Func<FirestoreDb> _firestoreDbBuilder;
        internal Func<AuditEvent, string> _idBuilder;
        internal bool _ignoreElementNameRestrictions = true;

        public IFirestoreProviderConfigurator ProjectId(string projectId)
        {
            _projectId = projectId;
            return this;
        }

        public IFirestoreProviderConfigurator ProjectId(Func<AuditEvent, string> projectIdBuilder)
        {
            _projectId = projectIdBuilder;
            return this;
        }

        public IFirestoreProviderConfigurator Database(string database)
        {
            _database = database;
            return this;
        }

        public IFirestoreProviderConfigurator Database(Func<AuditEvent, string> databaseBuilder)
        {
            _database = databaseBuilder;
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
            _firestoreDb = firestoreDb;
            _firestoreDbBuilder = null;
            return this;
        }

        public IFirestoreProviderConfigurator FirestoreDb(Func<FirestoreDb> firestoreDbBuilder)
        {
            _firestoreDbBuilder = firestoreDbBuilder;
            _firestoreDb = null;
            return this;
        }

        public IFirestoreProviderConfigurator IdBuilder(Func<AuditEvent, string> idBuilder)
        {
            _idBuilder = idBuilder;
            return this;
        }

        public IFirestoreProviderConfigurator IgnoreElementNameRestrictions(bool ignoreRestrictions)
        {
            _ignoreElementNameRestrictions = ignoreRestrictions;
            return this;
        }
    }
} 