using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.Full.UnitTest
{
    public class DepartmentContext1 : DbContext
    {
        private static DbContextHelper helper = new DbContextHelper();
        private readonly IAuditDbContext auditDbContext;
        public const string CnnString = "Data Source=localhost;Initial Catalog=DepartmentDatabase1;Integrated Security=True;";

        public DbSet<Department> Departments { get; set; }

        public DepartmentContext1()
        {
            auditDbContext = new DefaultAuditContext(this);
            auditDbContext.IncludeEntityObjects = true;
            helper.SetConfig(auditDbContext);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseLazyLoadingProxies();
        }

        public override int SaveChanges()
        {
            return helper.SaveChanges(auditDbContext, () => base.SaveChanges());
        }
    }

    public class DepartmentContext2 : DbContext
    {
        private static DbContextHelper helper = new DbContextHelper();
        private readonly IAuditDbContext auditDbContext;
        public const string CnnString = "Data Source=localhost;Initial Catalog=DepartmentDatabase2;Integrated Security=True;";

        public DbSet<Department> Departments { get; set; }

        public DepartmentContext2()
        {
            auditDbContext = new DefaultAuditContext(this);
            auditDbContext.IncludeEntityObjects = true;
            helper.SetConfig(auditDbContext);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseLazyLoadingProxies();
        }

        public override int SaveChanges()
        {
            return helper.SaveChanges(auditDbContext, () => base.SaveChanges());
        }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
    }

}


