using System.Threading.Tasks;

namespace Audit.EntityFramework
{
    public interface IAuditBypass
    {
        int SaveChangesBypassAudit();
        Task<int> SaveChangesBypassAuditAsync();
    }
}
