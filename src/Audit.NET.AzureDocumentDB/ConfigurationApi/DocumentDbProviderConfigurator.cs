using System;
using Audit.Core;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Audit.AzureDocumentDB.ConfigurationApi
{
    public class DocumentDbProviderConfigurator : IDocumentDbProviderConfigurator
    {
        internal Func<AuditEvent, string> _connectionStringBuilder = _ => string.Empty;
        internal Func<AuditEvent, string> _authKeyBuilder = _ => null;
        internal Func<AuditEvent, string> _databaseBuilder = _ => "Audit";
        internal Func<AuditEvent, string> _collectionBuilder = _ => "Events";
        internal Func<AuditEvent, ConnectionPolicy> _connectionPolicyBuilder = _ => null;
        internal IDocumentClient _documentClient = null;

        public IDocumentDbProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionStringBuilder = _ => connectionString;
            return this;
        }

        public IDocumentDbProviderConfigurator Database(string database)
        {
            _databaseBuilder = _ => database;
            return this;
        }

        public IDocumentDbProviderConfigurator Collection(string collection)
        {
            _collectionBuilder = _ => collection;
            return this;
        }

        public IDocumentDbProviderConfigurator AuthKey(string authKey)
        {
            _authKeyBuilder = _ => authKey;
            return this;
        }

        public IDocumentDbProviderConfigurator ConnectionPolicy(ConnectionPolicy connectionPolicy)
        {
            _connectionPolicyBuilder = _ => connectionPolicy;
            return this;
        }
        public IDocumentDbProviderConfigurator DocumentClient(IDocumentClient documentClient)
        {
            _documentClient = documentClient;
            return this;
        }

        public IDocumentDbProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _connectionStringBuilder = connectionStringBuilder;
            return this;
        }

        public IDocumentDbProviderConfigurator Database(Func<AuditEvent, string> databaseBuilder)
        {
            _databaseBuilder = databaseBuilder;
            return this;
        }

        public IDocumentDbProviderConfigurator Collection(Func<AuditEvent, string> collectionBuilder)
        {
            _collectionBuilder = collectionBuilder;
            return this;
        }

        public IDocumentDbProviderConfigurator AuthKey(Func<AuditEvent, string> authKeyBuilder)
        {
            _authKeyBuilder = authKeyBuilder;
            return this;
        }

        public IDocumentDbProviderConfigurator ConnectionPolicy(Func<AuditEvent, ConnectionPolicy> connectionPolicyBuilder)
        {
            _connectionPolicyBuilder = connectionPolicyBuilder;
            return this;
        }
    }
}