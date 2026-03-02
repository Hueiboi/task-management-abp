using System.Threading.Tasks;

namespace TaskManagement.Data;

public interface ITaskManagementDbSchemaMigrator
{
    Task MigrateAsync();
}
