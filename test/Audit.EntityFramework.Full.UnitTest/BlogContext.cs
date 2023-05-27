using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Audit.EntityFramework.Full.UnitTest
{
    public class BlogContext : AuditDbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("Blogs2");

        public BlogContext() : base(CnnString)
        {
        }
        public BlogContext(string connectionString) : base(connectionString)
        {

        }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Blog> Blogs { get; set; }

        public DbSet<AuditLog> Audits { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();
            modelBuilder.Entity<Blog>()
                .MapToStoredProcedures();
            modelBuilder.Entity<Post>()
                .MapToStoredProcedures();
        }
    }

    public class Blog
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(8)]
        public string Title { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    public class Post
    {
        [Key]
        public int Id { get; set; }
        public int BlogId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public virtual Blog Blog { get; set; }
    }
    public class AuditLog
    {
        [Key]
        public int AuditId { get; set; }
        public string TableName { get; set; }
        public int TablePK { get; set; }
        public string Title { get; set; }

        public DateTime AuditDate { get; set; }
        public string AuditAction { get; set; }
        public string AuditUsername { get; set; }
    }
}

