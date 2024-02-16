using System;
using System.Security.Cryptography.X509Certificates;
using Audit.Core;

namespace Audit.RavenDB.ConfigurationApi
{
    public class RavenDbProviderStoreConfigurator : IRavenDbProviderStoreConfigurator
    {
        internal string[] _urls = null;
        internal X509Certificate2 _certificate = null;
        internal string _databaseDefault = null;
        internal Setting<string> _database;

        public IRavenDbProviderStoreConfigurator Database(Func<AuditEvent, string> databaseBuilder)
        {
            _database = databaseBuilder;
            return this;
        }

        public IRavenDbProviderStoreConfigurator DatabaseDefault(string database)
        {
            _databaseDefault = database;
            return this;
        }

        public IRavenDbProviderStoreConfigurator Certificate(X509Certificate2 certificate)
        {
            _certificate = certificate;
            return this;
        }

        public IRavenDbProviderStoreConfigurator Urls(params string[] urls)
        {
            _urls = urls;
            return this;
        }
    }
}