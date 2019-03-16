using System;
using Audit.Core;

namespace Audit.SqlServer.Configuration
{
    /// <summary>
    /// Provides a configuration for the Sql Server DB data provider
    /// </summary>
    public interface ISqlServerProviderConfigurator
    {
        /// <summary>
        /// Specifies the Sql Server connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        ISqlServerProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the Sql Server Table Name.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        ISqlServerProviderConfigurator TableName(string tableName);
        /// <summary>
        /// Specifies the column that is the Primary Key (or unique key)
        /// </summary>
        /// <param name="idColumnName">The id column name.</param>
        ISqlServerProviderConfigurator IdColumnName(string idColumnName);
        /// <summary>
        /// Specifies the column where to store the event json data
        /// </summary>
        /// <param name="jsonColumnName">The json data column name.</param>
        ISqlServerProviderConfigurator JsonColumnName(string jsonColumnName);
        /// <summary>
        /// Specifies the column where to store the last updated date.
        /// </summary>
        /// <param name="lastUpdatedColumnName">The last udpated date column name, or NULL to ignore.</param>
        ISqlServerProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName);
        /// <summary>
        /// Specifies the SQL schema where to store the events
        /// </summary>
        /// <param name="schema">The Schema name.</param>
        ISqlServerProviderConfigurator Schema(string schema);
        /// <summary>
        /// Specifies the Sql Server connection string as a function of the audit event.
        /// </summary>
        /// <param name="connectionStringBuilder">The connection string as a function of the audit event.</param>
        ISqlServerProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder);
        /// <summary>
        /// Specifies the Sql Server Table Name as a function of the audit event.
        /// </summary>
        /// <param name="tableNameBuilder">The table name as a function of the audit event.</param>
        ISqlServerProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder);
        /// <summary>
        /// Specifies the column that is the Primary Key (or unique key) as a function of the audit event
        /// </summary>
        /// <param name="idColumnNameBuilder">The id column name as a function of the audit event.</param>
        ISqlServerProviderConfigurator IdColumnName(Func<AuditEvent, string> idColumnNameBuilder);
        /// <summary>
        /// Specifies the column where to store the event json data as a function of the audit event
        /// </summary>
        /// <param name="jsonColumnNameBuilder">The json data column name as a function of the audit event.</param>
        ISqlServerProviderConfigurator JsonColumnName(Func<AuditEvent, string> jsonColumnNameBuilder);
        /// <summary>
        /// Specifies the column where to store the last updated date as a function of the audit event.
        /// </summary>
        /// <param name="lastUpdatedColumnNameBuilder">The last udpated date column name as a function of the audit event, or NULL to ignore.</param>
        ISqlServerProviderConfigurator LastUpdatedColumnName(Func<AuditEvent, string> lastUpdatedColumnNameBuilder);
        /// <summary>
        /// Specifies an extra custom column on the audit log table and the value as a function of the audit event 
        /// </summary>
        /// <param name="columnName">The column name</param>
        /// <param name="value">A function of the audit event that returns the value to insert/update on this column</param>
        ISqlServerProviderConfigurator CustomColumn(string columnName, Func<AuditEvent, object> value);
        /// <summary>
        /// Specifies the SQL schema where to store the events as a function of the audit event
        /// </summary>
        /// <param name="schemaBuilder">The Schema name as a function of the audit event.</param>
        ISqlServerProviderConfigurator Schema(Func<AuditEvent, string> schemaBuilder);
    }
}