using System;
using System.Security.Cryptography.X509Certificates;
using Audit.Core;

namespace Audit.RavenDB.ConfigurationApi
{
    public interface IRavenDbProviderStoreConfigurator
    {
        /// <summary>
        /// The document store connection Url(s)
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        IRavenDbProviderStoreConfigurator Urls(params string[] urls);

        /// <summary>
        /// Authentication certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        IRavenDbProviderStoreConfigurator Certificate(X509Certificate2 certificate);

        /// <summary>
        /// Specifies the default database which the Client will work against.
        /// This is the default database used when DatabaseSession is not provided or is null.
        /// </summary>
        /// <param name="database">The default database name.</param>
        IRavenDbProviderStoreConfigurator DatabaseDefault(string database);

        /// <summary>
        /// Specifies the RavenDB database name to use at the session level.
        /// </summary>
        /// <param name="database">A function of the audit event that returns the database name to use.</param>
        IRavenDbProviderStoreConfigurator Database(Func<AuditEvent, string> database);
    }
}