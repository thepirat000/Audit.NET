using System.Data.Entity;

namespace Audit.EntityFramework.Full.UnitTest
{
    public class DepartmentContext1 : DbContext
    {
        private static DbContextHelper helper = new DbContextHelper();
        private readonly IAuditDbContext auditDbContext;

        public DepartmentContext1()
            : base("Data Source=localhost;Initial Catalog=DepartmentDatabase1;Integrated Security=True")
        {
            auditDbContext = new DefaultAuditContext(this);
            auditDbContext.IncludeEntityObjects = true;
            helper.SetConfig(auditDbContext);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Department>();
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

        public DepartmentContext2()
            : base("Data Source=localhost;Initial Catalog=DepartmentDatabase2;Integrated Security=True")
        {
            auditDbContext = new DefaultAuditContext(this);
            auditDbContext.IncludeEntityObjects = true;
            helper.SetConfig(auditDbContext);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Department>();
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


