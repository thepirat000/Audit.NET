using System;
using Audit.Core;

namespace Audit.NET.AmazonQLDB.ConfigurationApi
{
    /// <summary>
    /// Provides a Table level configuration for AmazonQLDB provider
    /// </summary>
    public interface IAmazonQldbProviderTableConfigurator
    {
        /// <summary>
        /// Specify a constant table name to use
        /// </summary>
        /// <param name="tableName">The table name</param>
        IAmazonQldbProviderAttributeConfigurator Table(string tableName);
        /// <summary>
        /// Specify a table name that is a function of the audit event
        /// </summary>
        /// <param name="tableNameBuilder">The table name builder</param>
        IAmazonQldbProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder);
    }

}
