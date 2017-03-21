namespace Audit.MySql.Configuration
{
    /// <summary>
    /// Provides a configuration for the MySql Server DB data provider
    /// </summary>
    public interface IMySqlServerProviderConfigurator
    {
        /// <summary>
        /// Specifies the MySQL connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        IMySqlServerProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the MySQL Table Name.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        IMySqlServerProviderConfigurator TableName(string tableName);
        /// <summary>
        /// Specifies the column that is the Primary Key (or unique key)
        /// </summary>
        /// <param name="idColumnName">The id column name.</param>
        IMySqlServerProviderConfigurator IdColumnName(string idColumnName);
        /// <summary>
        /// Specifies the column where to store the event json data
        /// </summary>
        /// <param name="jsonColumnName">The json data column name.</param>
        IMySqlServerProviderConfigurator JsonColumnName(string jsonColumnName);
    }
}