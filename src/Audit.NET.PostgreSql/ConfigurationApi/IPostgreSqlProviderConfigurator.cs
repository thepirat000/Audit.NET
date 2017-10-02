namespace Audit.PostgreSql.Configuration
{
    public enum DataType
    {
        /// <summary>JSON data type</summary>
        JSON = 0,
        /// <summary>JSONB data type</summary>
        JSONB = 1,
        /// <summary>Any valid character string type (text, char, varchar)</summary>
        String = 2
    }

    /// <summary>
    /// Provides a configuration for the PostgreSQL Server DB data provider
    /// </summary>
    public interface IPostgreSqlProviderConfigurator
    {
        /// <summary>
        /// Specifies the connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        IPostgreSqlProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the Table Name. Default is "event".
        /// </summary>
        /// <param name="tableName">The table name.</param>
        IPostgreSqlProviderConfigurator TableName(string tableName);
        /// <summary>
        /// Specifies the column that is the Primary Key (or unique key). Default is "id".
        /// </summary>
        /// <param name="idColumnName">The id column name.</param>
        IPostgreSqlProviderConfigurator IdColumnName(string idColumnName);
        /// <summary>
        /// Specifies the column where to store the event data. Default is the column named "data" assuming type JSON.
        /// </summary>
        /// <param name="dataColumnName">The data column name.</param>
        /// <param name="dataColumnType">The data column type.</param>
        IPostgreSqlProviderConfigurator DataColumn(string dataColumnName, DataType dataColumnType = DataType.JSON);
        /// <summary>
        /// Specifies the column where to store the last updated date. NULL to ignore. Default is NULL.
        /// </summary>
        /// <param name="lastUpdatedColumnName">The last udpated date column name, or NULL to ignore.</param>
        IPostgreSqlProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName);
        /// <summary>
        /// Specifies the schema where to store the events. NULL to ignore. Default is NULL.
        /// </summary>
        /// <param name="schema">The Schema name.</param>
        IPostgreSqlProviderConfigurator Schema(string schema);
    }
}