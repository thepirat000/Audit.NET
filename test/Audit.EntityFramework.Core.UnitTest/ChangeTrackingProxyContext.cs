#if EF_CORE_5 || EF_CORE_6
using Microsoft.EntityFrameworkCore;
using System;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class ChangeTrackingProxyContext : AuditDbContext
    {
        public class Customer
        {
            public virtual int Id { get; set; }
            public virtual string CustomerName { get; set; }
        }
        public class AuditLog
        {
            public virtual int Id { get; set; }
            public virtual string Table { get; set; }
            public virtual string Action { get; set; }
            public virtual string CustomerName { get; set; }
            public virtual DateTime DateTime { get; set; }
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseChangeTrackingProxies();
            optionsBuilder.UseInMemoryDatabase("ChangeTrackingProxy");
        }
    }
}
#endif