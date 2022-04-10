using System;
using System.Security.Cryptography.X509Certificates;
using Audit.Core;
using Raven.Client.Json.Serialization;

namespace Audit.NET.RavenDB.ConfigurationApi
{
    public class RavenDbProviderStoreConfigurator : IRavenDbProviderStoreConfigurator
    {
        internal string[] _urls = null;
        internal X509Certificate2 _certificate = null;
        internal string _databaseDefault = null;
        internal Func<AuditEvent, string> _databaseFunc;

        public IRavenDbProviderStoreConfigurator Database(Func<AuditEvent, string> database)
        {
            _databaseFunc = database;
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