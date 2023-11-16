using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audit.EntityFramework.Core.UnitTest
{
#if EF_CORE_8_OR_GREATER
    [AuditDbContext(IncludeEntityObjects = true)]
    public class Context_ComplexTypes : AuditDbContext
    {
        public class Person
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Name { get; set; }
            [Required]
            public required Address Address { get; set; }
        }

        //[ComplexType]
        public record Address
        {
            public string Line1 { get; init; }
            [AuditIgnore]
            public string Line2 { get; init; }
            public string City { get; init; }
            [Required]
            public required Country Country { get; init; }
            public string PostCode { get; init; }
        }

        //[ComplexType]
        public record Country
        {
            public string Name { get; init; }
            public string Alias { get; init; }
        }

        public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var cnnString = TestHelper.GetConnectionString(nameof(Context_ComplexTypes));
                optionsBuilder.UseSqlServer(cnnString).UseLazyLoadingProxies();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().ComplexProperty(e => e.Address).ComplexProperty(a => a.Country);
        }
    }
#endif
    
#if EF_CORE_7_OR_GREATER
    [AuditDbContext(IncludeEntityObjects = true)]
    public class Context_OwnedEntity_ToJson : AuditDbContext
    {
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

        public DbSet<Person> People { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@$"Server=(localdb)\mssqllocaldb;Database={nameof(Context_OwnedEntity_ToJson)};Trusted_Connection=True;ConnectRetryCount=0").UseLazyLoadingProxies();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().OwnsOne(e => e.Address, b => b.ToJson());
        }
    }
#endif

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
            .UseInMemoryDatabase("testing-db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Person>().OwnsMany(x => x.Addresses);
            modelBuilder.Entity<Department>().OwnsOne(x => x.Address);
        }
    }
#endif
}
