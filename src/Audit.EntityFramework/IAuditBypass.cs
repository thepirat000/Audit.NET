using System.Threading.Tasks;

namespace Audit.EntityFramework
{
    internal interface IAuditBypass
    {
        int SaveChangesBypassAudit();
        Task<int> SaveChangesBypassAuditAsync();
    }
}
