using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

namespace Audit.EntityFramework.Full.UnitTest
{
    public class WorkContext : DbContext
    {
        private static DbContextHelper helper = new DbContextHelper();
        private readonly IAuditDbContext auditDbContext;

        public WorkContext()
            : base("Data Source=localhost;Initial Catalog=WorkDatabase;Integrated Security=True")
        {
            auditDbContext = new DefaultAuditContext(this);
            auditDbContext.IncludeEntityObjects = true;
            helper.SetConfig(auditDbContext);
            

            Database.SetInitializer(new MyInitializer());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>();

            modelBuilder.ComplexType<Address>();
        }

        public override int SaveChanges()
        {
            return helper.SaveChanges(auditDbContext, () => base.SaveChanges());
        }
    }

    public class MyInitializer : DropCreateDatabaseAlways<WorkContext>
    {
        protected override void Seed(WorkContext context)
        {
            context.Set<Employee>().Add(new Employee
            {
                Id = 1,
                Name = "John",
                Address = new Address
                {
                    City = "City 1",
                    Number = "1",
                    Street = "Street 1"
                }
            });
        }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
    }
    public class Address
    {
        public string Number { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
    }
}
