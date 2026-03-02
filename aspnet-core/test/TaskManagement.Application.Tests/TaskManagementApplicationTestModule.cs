using Volo.Abp.Modularity;

namespace TaskManagement;

[DependsOn(
    typeof(TaskManagementApplicationModule),
    typeof(TaskManagementDomainTestModule)
)]
public class TaskManagementApplicationTestModule : AbpModule
{

}
