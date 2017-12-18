using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Audit.EntityFramework;


namespace DataBaseService
{
    [Table("Entities")]
    public class DBEntity : DBEntityBase
    {
        public string Name2 { get; set; }

        public DBEntity()
        {
        }
    }

    [Table("Entities3")]
    public class DBEntity3 : DBEntity
    {
        public string Name3 { get; set; }

        public DBEntity3()
        {
        }
    }

    public abstract class SomeBaseNotMapped
    {

    }
    [Table("Another")]
    public class AnotherEntity : SomeBaseNotMapped
    {
        [Key]
        public string AnotherColumn { get; set; }

        public AnotherEntity()
        {
        }
    }

    [Table("EntitiesBase")]
    public class DBEntityBase
    {
        [Key]
        public int ID { get; set; }

        public TimeSpan? Timeout { get; set; }
        public long? Ticks { get; set; }

        public string Name { get; set; }

        public DBEntityBase()
        {
        }
    }

    [AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = true, IncludeIndependantAssociations = true, AuditEventType = "{database}_{context}")]
    public class DataBaseContext : Audit.EntityFramework.AuditDbContext //DbContext
    {
        public void FixEfProviderServicesProblem()
        {
            //The Entity Framework provider type 'System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer'
            //for the 'System.Data.SqlClient' ADO.NET provider could not be loaded. 
            //Make sure the provider assembly is available to the running application. 
            //See http://go.microsoft.com/fwlink/?LinkId=260882 for more information.

            var instance = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
        }

        public DataBaseContext() : base("AuditTest")
        {
            this.Configuration.LazyLoadingEnabled = false;
        }

        public DbSet<DBEntityBase> Entities { get; set; }

        public DbSet<AnotherEntity> AnotherEntities { get; set; }
    }

    public class DBInitializer : System.Data.Entity.CreateDatabaseIfNotExists<DataBaseContext>
    {
    }
}