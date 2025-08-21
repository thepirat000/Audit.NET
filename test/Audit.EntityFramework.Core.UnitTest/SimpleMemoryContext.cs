using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework.Core.UnitTest
{
    public class SimpleMemoryContext : AuditDbContext
    {
        [AuditInclude]
        public class Car
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string Name { get; set; }

            public int? BrandId { get; set; }
            [ForeignKey(nameof(BrandId))]
            public Brand Brand { get; set; }
        }

        [AuditInclude]
        public class Brand
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<Brand> Brands { get; set; }

        public SimpleMemoryContext() { }
        public SimpleMemoryContext(DbContextOptions<OrderMemoryContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("SimpleMemoryContext.InMemory");
            }
        }
    }
}