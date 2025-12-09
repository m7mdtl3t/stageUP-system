using System.Threading.Tasks;

namespace VivuqeQRSystem.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityName, string entityId, string details);
    }
}
