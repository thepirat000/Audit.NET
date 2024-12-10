using System.Data.Common;

using Microsoft.EntityFrameworkCore;

namespace Audit.SqlServer
{

#if !NET7_0_OR_GREATER
    /// <summary>
    /// Represents the model to use to query the AuditEvent Id and Value with Entity Framework
    /// </summary>
    public class AuditEventValueModel
    {
        /// <summary>
        /// The AuditEvent Value
        /// </summary>
        [System.ComponentModel.DataAnnotations.Key]
        public string Value { get; set; }
    }
#endif

    /// <summary>
    /// Represents the default DbContext for Audit.NET.SqlServer to store the events
    /// </summary>
    public class DefaultAuditDbContext : DbContext
    {
        private string _connectionString = null;
        private DbConnection _dbConnection = null;

        /// <summary>
        /// Creates a new instance of DefaultAuditDbContext using the provided connection string
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <param name="options">The DbContext options</param>
        public DefaultAuditDbContext(string connectionString, DbContextOptions options) : base(options)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Creates a new instance of DefaultAuditDbContext using the provided DB connection
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="options"></param>
        public DefaultAuditDbContext(DbConnection dbConnection, DbContextOptions options) : base(options)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Creates a new instance of DefaultAuditDbContext using the provided connection string
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        public DefaultAuditDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Creates a new instance of DefaultAuditDbContext using the provided DB connection
        /// </summary>
        /// <param name="connection">The DB connection</param>
        public DefaultAuditDbContext(DbConnection connection)
        {
            _dbConnection = connection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_dbConnection != null)
            {
                optionsBuilder.UseSqlServer(_dbConnection);
            }
            else if (_connectionString != null)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }

#if !NET7_0_OR_GREATER
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditEventValueModel>();
        }
#endif
    }
}

