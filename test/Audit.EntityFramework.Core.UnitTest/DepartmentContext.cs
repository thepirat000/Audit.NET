﻿using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class DepartmentContext1 : DbContext
    {
        private static DbContextHelper helper = new DbContextHelper();
        private readonly IAuditDbContext auditDbContext;
        public static string CnnString = TestHelper.GetConnectionString("DepartmentDatabase1");

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

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return helper.SaveChanges(auditDbContext, () => base.SaveChanges(acceptAllChangesOnSuccess));
        }
    }

    public class DepartmentContext2 : DbContext
    {
        private static DbContextHelper helper = new DbContextHelper();
        private readonly IAuditDbContext auditDbContext;
        public static string CnnString = TestHelper.GetConnectionString("DepartmentDatabase2");

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

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return helper.SaveChanges(auditDbContext, () => base.SaveChanges(acceptAllChangesOnSuccess));
        }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
    }

}


