using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.SqlServer.Configuration
{
    public class SqlServerProviderConfigurator : ISqlServerProviderConfigurator
    {
        internal string _connectionString = "data source=localhost;initial catalog=Audit;integrated security=true;";
        internal string _tableName = "Event";
        internal string _idColumnName = "Id";
        internal string _jsonColumnName = "Data";
        internal string _lastUpdatedColumnName = null;

        public ISqlServerProviderConfigurator ConnectionString(string connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public ISqlServerProviderConfigurator TableName(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        public ISqlServerProviderConfigurator IdColumnName(string idColumnName)
        {
            _idColumnName = idColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator JsonColumnName(string jsonColumnName)
        {
            _jsonColumnName = jsonColumnName;
            return this;
        }

        public ISqlServerProviderConfigurator LastUpdatedColumnName(string lastUpdatedColumnName)
        {
            _lastUpdatedColumnName = lastUpdatedColumnName;
            return this;
        }

    }
}
