using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class DemoContext : AuditDbContext
    {
        public const string CnnString = "data source=localhost;initial catalog=Demo;integrated security=true;";

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PettyCashTransaction> Pettys { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder _)
        {
            _.Entity<Tenant>().ToTable("Tenant");
            _.Entity<Employee>().ToTable("Employee");
            _.Entity<PettyCashTransaction>().ToTable("PettyCashTransaction");

            _.Entity<Tenant>()
                .HasKey(t => t.Id);

            _.Entity<Employee>()
                .HasKey(t => new { t.Id, t.TenantId });

            _.Entity<PettyCashTransaction>()
                .HasKey(t => new { t.Id });

            _.Entity<PettyCashTransaction>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => new { t.EmployeeId, t.TenantId });

            _.Entity<PettyCashTransaction>()
                .HasOne(t => t.Trustee)
                .WithMany()
                .HasForeignKey(t => new { t.TrusteeId, t.TenantId });
        }
    }

    public class Tenant
    {
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
