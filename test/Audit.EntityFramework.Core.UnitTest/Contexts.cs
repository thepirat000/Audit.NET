using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audit.EntityFramework.Core.UnitTest
{
#if EF_CORE_5_OR_GREATER
    [AuditDbContext(IncludeEntityObjects = true)]
    public class Context_OwnedEntity : AuditDbContext
    {
        public class Department
        {
            public Address Address { get; set; }
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class Person
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
            public Address Address { get; set; }
        }
        public class Address
        {
            public string City { get; set; }
            public string Street { get; set; }
        }

        public DbSet<Department> Departments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0").UseLazyLoadingProxies();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>().OwnsOne(x => x.Address);
            modelBuilder.Entity<Person>().OwnsOne(x => x.Address);
        }
    }

    [AuditDbContext(IncludeEntityObjects = true)]
    public class Context_ManyToMany : AuditDbContext
    {
        public class Person
        {
            public virtual ICollection<Department> Departments { get; set; }
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class Department
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
            public virtual ICollection<Person> Persons { get; set; }
        }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0").UseLazyLoadingProxies();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().HasMany<Department>(x => x.Departments).WithMany(x => x.Persons);
        }
    }
#endif

#if EF_CORE_3_OR_GREATER
    [AuditDbContext(IncludeEntityObjects = true)]
    public class OwnedSingleMultiple_Context : AuditDbContext
    {
        public class Department
        {
            public Address Address { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class Person
        {
            public List<Address> Addresses { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Address
        {
            public string City { get; set; }
            public string Street { get; set; }
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Person> Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
            //.UseSqlServer("data source=localhost;initial catalog=OwnedSingleMultiple;integrated security=true;Encrypt=False;");
            .UseInMemoryDatabase("testing-db");
            //.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().OwnsMany(x => x.Addresses);
            modelBuilder.Entity<Department>().OwnsOne(x => x.Address);
        }
    }
#endif
}
