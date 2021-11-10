using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audit.EntityFramework.Core.UnitTest
{
    [AuditDbContext(Mode = AuditOptionMode.OptIn)]
    public class BlogsContext : AuditDbContext
    {
        public const string CnnString = "data source=localhost;initial catalog=Blogs3;integrated security=true;";

        public override bool AuditDisabled { get; set; }

        private readonly ILoggerFactory _loggerFactory;

        public BlogsContext() { }

        public BlogsContext(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(_loggerFactory);
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.EnableSensitiveDataLogging();
#if !NETCOREAPP1_0
            optionsBuilder.UseLazyLoadingProxies();
#endif
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        public DbSet<BlogAudit> BlogsAudits { get; set; }
        public DbSet<PostAudit> PostsAudits { get; set; }
        public DbSet<CommonAudit> CommonAudits { get; set; }
    }

    public abstract class BaseEntity
    {
        public virtual int Id { get; set; }
    }
    public interface IAuditEntity
    {
        string AuditAction { get; set; }
        DateTime AuditDate { get; set; }
        string AuditUser { get; set; }
        string Exception { get; set; }
    }
    [Table("Blogs", Schema="dbo")]
    [AuditInclude]
    public class Blog : BaseEntity
    {
        [Key]
        public override int Id { get; set; }
        [MaxLength(25)]
        public virtual string Title { get; set; }
        public virtual string BloggerName { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    [Table("Posts", Schema = "dbo")]
    [AuditInclude]
    public class Post : BaseEntity
    {
        [Key]
        public override int Id { get; set; }
        [MaxLength(20)]
        public virtual string Title { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual string Content { get; set; }
        public virtual int BlogId { get; set; }
        public virtual Blog Blog { get; set; }
    }
    [Table("PostsAudits", Schema = "dbo")]
    [AuditInclude]
    public class PostAudit : IAuditEntity
    {
        public virtual int PostId { get; set; }
        public virtual string Title { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual string Content { get; set; }
        public virtual int BlogId { get; set; }

        [Key]
        public virtual int PostAuditId { get; set; }
        public virtual string AuditAction { get; set; }
        public virtual DateTime AuditDate { get; set; }
        public virtual string AuditUser { get; set; }
        public virtual string Exception { get; set; }
    }
    [Table("BlogsAudits", Schema = "dbo")]
    [AuditInclude]
    public class BlogAudit : IAuditEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public virtual int BlogAuditId { get; set; }
        public virtual int BlogId { get; set; }
        public virtual string Title { get; set; }
        public virtual string BloggerName { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
        public virtual string AuditAction { get; set; }
        public virtual DateTime AuditDate { get; set; }
        public virtual string AuditUser { get; set; }
        public virtual string Exception { get; set; }

    }
    [Table("CommonAudits", Schema = "dbo")]
    public class CommonAudit : IAuditEntity
    {
        [Key]
        public int CommonAuditId { get; set; }

        public virtual string EntityType { get; set; }
        public virtual int EntityId { get; set; }
        public virtual string Title { get; set; }
        public virtual string Group { get; set; }


        public virtual string AuditAction { get; set; }
        public virtual DateTime AuditDate { get; set; }
        public virtual string AuditUser { get; set; }
        public virtual string Exception { get; set; }

    }
}