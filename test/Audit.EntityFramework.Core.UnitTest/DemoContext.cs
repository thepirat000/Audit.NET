using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Text;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class DemoContext : AuditDbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("Demo");

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PettyCashTransaction> Pettys { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Tenant>().ToTable("Tenant");
            builder.Entity<Employee>().ToTable("Employee");
            builder.Entity<PettyCashTransaction>().ToTable("PettyCashTransaction");

            builder.Entity<Tenant>()
                .HasKey(t => t.Id);

            builder.Entity<Employee>()
                .HasKey(t => new { t.Id, t.TenantId });

            builder.Entity<PettyCashTransaction>()
                .HasKey(t => new { t.Id });

            builder.Entity<PettyCashTransaction>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => new { t.EmployeeId, t.TenantId });

            builder.Entity<PettyCashTransaction>()
                .HasOne(t => t.Trustee)
                .WithMany()
                .HasForeignKey(t => new { t.TrusteeId, t.TenantId })
#if EF_CORE_3_OR_GREATER
                .OnDelete(DeleteBehavior.NoAction);
#else
                ;
#endif
        }
    }

    public class Tenant
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class Employee
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; }
    }
    public class PettyCashTransaction
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int TrusteeId { get; set; }
        public int TenantId { get; set; }

        public Employee Employee { get; set; }
        public Employee Trustee { get; set; }
    }
}
