namespace Audit.SqlServer
{
#if NET45
    using System.Data.Entity;

    public class AuditDbContext : DbContext
    {

        public AuditDbContext(string connectionString)
            : base(connectionString)
        {
        }
    }
#elif NETCOREAPP1_0
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;
    
    public class DynamicIdModel
    {
        [Key]
        public string Id { get; set; }

    }

    public class AuditDbContext : DbContext
    {
        public DbSet<DynamicIdModel> FakeIdSet { get; set; }
        public string _connectionString;
        public AuditDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
#endif
}

