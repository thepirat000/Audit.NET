using System.Data.Entity;

namespace Audit.SqlServer
{
    public partial class Entities
    {
        static Entities()
        {
            Database.SetInitializer<Entities>(null);
        }
    }
}
