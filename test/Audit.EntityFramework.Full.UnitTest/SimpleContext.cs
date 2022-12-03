using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Audit.EntityFramework.Full.UnitTest
{
    public class SimpleContext : AuditDbContext
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

        public SimpleContext() : base("data source=localhost;initial catalog=SimpleContext;integrated security=true;Encrypt=False;") { } 
       
    }
}