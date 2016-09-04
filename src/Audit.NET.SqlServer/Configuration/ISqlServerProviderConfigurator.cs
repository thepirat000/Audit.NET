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
    }
}