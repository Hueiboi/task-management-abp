using Volo.Abp.Modularity;

namespace TaskManagement;

[DependsOn(
    typeof(TaskManagementDomainModule),
    typeof(TaskManagementTestBaseModule)
)]
public class TaskManagementDomainTestModule : AbpModule
{

}
