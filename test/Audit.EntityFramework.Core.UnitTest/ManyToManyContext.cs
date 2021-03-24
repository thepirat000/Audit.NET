using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audit.EntityFramework.Core.UnitTest
{
    [AuditDbContext(IncludeEntityObjects = true, ExcludeTransactionId = true, Mode = AuditOptionMode.OptOut)]
    public class ManyToManyContext : AuditDbContext
    {
        [AuditIgnore]
        public class Audit_Post : BaseAudit
        {
            public new string Id { get; set; }
            public string Action { get; set; }
            public int PostId { get; set; }
            public string Name { get; set; }
        }

        public abstract class BaseAudit
        {
            public int Id { get; set; }
            public string AuditAction { get; set; }
        }

        [AuditIgnore]
        public class Audit_Tag
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Action { get; set; }
            public int TagId { get; set; }
            public string Text { get; set; }
        }
        [AuditIgnore]
        public class Audit_PostTag
        {
            public int Id { get; set; }
            public string Extra { get; set; }

            public string Action { get; set; }
            public int TagsId { get; set; }
            public string PostsId { get; set; }
        }

        public class Post
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public string Id { get; set; }
            public string Name { get; set; }
            public ICollection<Tag> Tags { get; set; }
        }

        public class Tag
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int Id { get; set; }
            public string Text { get; set; }
            public ICollection<Post> Posts { get; set; }
        }


        public const string CnnString = "data source=localhost;initial catalog=ManyToMany;integrated security=true;";

        public DbSet<Audit_PostTag> Audit_PostTags { get; set; }
        public DbSet<Audit_Post> Audit_Posts { get; set; }
        public DbSet<Audit_Tag> Audit_Tags { get; set; }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Tag> Tags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
        }

    }


}