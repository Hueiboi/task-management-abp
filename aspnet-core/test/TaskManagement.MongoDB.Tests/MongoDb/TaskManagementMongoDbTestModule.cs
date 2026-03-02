using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace TaskManagement.MongoDB;

[DependsOn(
    typeof(TaskManagementApplicationTestModule),
    typeof(TaskManagementMongoDbModule)
)]
public class TaskManagementMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbConnectionOptions>(options =>
        {
            options.ConnectionStrings.Default = TaskManagementMongoDbFixture.GetRandomConnectionString();
        });
    }
}
