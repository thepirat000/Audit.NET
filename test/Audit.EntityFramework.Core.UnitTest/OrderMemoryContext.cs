using Microsoft.EntityFrameworkCore;
using System;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class OrderMemoryContext : AuditDbContext
    {
        public class Order
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [AuditIgnore]
        public class AuditLog
        {
            public int Id { get; set; }
            public string AuditData { get; internal set; }
            public string EntityType { get; internal set; }
            public DateTime AuditDate { get; internal set; }
            public string AuditUser { get; internal set; }
            public string TablePk { get; internal set; }
        }

        [AuditIgnore]
        public class OrderAudit : IAudit
        {
            public int Id { get; set; }
            public DateTime AuditDate { get; set; }
            public string UserName { get; set; }
            public string AuditAction { get; set; }
        }
        public class Orderline
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [AuditIgnore]
        public class OrderlineAudit : IAudit
        {
            public int Id { get; set; }
            public DateTime AuditDate { get; set; }
            public string UserName { get; set; }
            public string AuditAction { get; set; }
        }
        public interface IAudit
        {
            DateTime AuditDate { get; set; }
            string UserName { get; set; }
            string AuditAction { get; set; }
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Orderline> Orderlines { get; set; }
        public DbSet<Product> Products { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<OrderAudit> OrderAudits { get; set; }
        public DbSet<OrderlineAudit> OrderlineAudits { get; set; }

        public OrderMemoryContext()
        { }

        public OrderMemoryContext(DbContextOptions<OrderMemoryContext> options)
            : base(options)
        { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(TestHelper.GetConnectionString("OrderMemoryContext.InMemory"));
            }
        }
    }

}