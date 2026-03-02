using Volo.Abp.Modularity;

namespace TaskManagement;

public abstract class TaskManagementApplicationTestBase<TStartupModule> : TaskManagementTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
