using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace TaskManagement.Data;

/* This is used if database provider does't define
 * ITaskManagementDbSchemaMigrator implementation.
 */
public class NullTaskManagementDbSchemaMigrator : ITaskManagementDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
