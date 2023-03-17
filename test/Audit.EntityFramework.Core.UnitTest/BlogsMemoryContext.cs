using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audit.EntityFramework.Core.UnitTest
{

    public class BlogsMemoryContext : AuditDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserAudit> UserAudits { get; set; }

        public BlogsMemoryContext()
        { }

        public BlogsMemoryContext(DbContextOptions<BlogsMemoryContext> options)
            : base(options)
        { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(e =>
            {
                e.Property(x => x.Name2)
                    .HasColumnName("NAME_2");
            });
        }
    }

    public abstract class EntityBase
    {
        [AuditIgnore]
        public string Password { get; set; }
        [AuditOverride("***")]
        public string Token { get; set; }
    }
    public class User : EntityBase
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Name2 { get; set; }
    }
    [AuditIgnore]
    public class UserAudit 
    {
        [Key]
        public int AuditId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Name2 { get; set; }
        public string AuditUser { get; set; }
        public string Action { get; set; }
    }

}