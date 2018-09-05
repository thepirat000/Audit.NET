using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    /// <summary>
    /// Provides a Table level configuration for DynamoDB provider
    /// </summary>
    public interface IDynamoProviderTableConfigurator
    {
        /// <summary>
        /// Specify a constant table name to use
        /// </summary>
        /// <param name="tableName">The table name</param>
        IDynamoProviderAttributeConfigurator Table(string tableName);
        /// <summary>
        /// Specify a table name that is a function of the audit event
        /// </summary>
        /// <param name="tableNameBuilder">The table name builder</param>
        IDynamoProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder);
    }

}
