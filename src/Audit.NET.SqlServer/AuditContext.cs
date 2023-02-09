namespace Audit.SqlServer
{
#if NET45
    using System.Data.Entity;
    using System.Data.Common;

    internal class AuditContext : DbContext
    {
        public AuditContext(string connectionString, bool setNullInitializer)
            : base(connectionString)
        {
            if (setNullInitializer)
            {
                Database.SetInitializer<AuditContext>(null);
            }
        }
        public AuditContext(DbConnection connection, bool setNullInitializer, bool contextOwnsConnection) : base(connection, contextOwnsConnection)
        {
            if (setNullInitializer)
            {
                Database.SetInitializer<AuditContext>(null);
            }
        }
    }
#elif NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1 || NET5_0 || NET6_0 || NET7_0
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;
    
    internal class DynamicIdModel
    {
        [Key]
        public string Id { get; set; }

    }

    internal class AuditContext : DbContext
    {
        public DbSet<DynamicIdModel> FakeIdSet { get; set; }
        public string _connectionString;
        public AuditContext(string connectionString, DbContextOptions options) : base(options)
        {
            _connectionString = connectionString;
        }
        public AuditContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_connectionString != null)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }
    }
#endif
}

