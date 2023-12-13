namespace Audit.SqlServer
{
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
}

