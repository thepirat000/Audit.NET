using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class BlogsMemoryContext : AuditDbContext
    {
        public DbSet<User> Users { get; set; }

        public BlogsMemoryContext()
        { }

        public BlogsMemoryContext(DbContextOptions<BlogsMemoryContext> options)
            : base(options)
        { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
            }
        }

    }

    public abstract class EntityBase
    {
        [AuditIgnore]
        public string Password { get; set; }
        [AuditOverride("***")]
        public string Token { get; set; }
    }
    public class User : EntityBase
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}