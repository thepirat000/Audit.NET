using Audit.Core;
using Raven.Client.Documents;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Audit.RavenDB.ConfigurationApi
{
    public class RavenDbProviderConfigurator : IRavenDbProviderConfigurator
    {
        internal RavenDbProviderStoreConfigurator _storeConfig = new RavenDbProviderStoreConfigurator();
        internal IDocumentStore _documentStore;

        public void UseDocumentStore(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public void WithSettings(Action<IRavenDbProviderStoreConfigurator> documentStoreSettings)
        {
            documentStoreSettings.Invoke(_storeConfig);
        }

        public void WithSettings(string[] urls, string databaseDefault, X509Certificate2 certificate = null, Func<AuditEvent, string> databaseFunc = null)
        {
            _storeConfig._urls = urls;
            _storeConfig._databaseDefault = databaseDefault;
            _storeConfig._certificate = certificate;
            _storeConfig._database = databaseFunc;
        }

        public void WithSettings(string url, string databaseDefault, X509Certificate2 certificate = null, Func<AuditEvent, string> databaseFunc = null)
        {
            _storeConfig._urls = new [] { url };
            _storeConfig._databaseDefault = databaseDefault;
            _storeConfig._certificate = certificate;
            _storeConfig._database = databaseFunc;
        }
    }
}