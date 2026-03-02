using Volo.Abp.Account;
using Volo.Abp.AutoMapper; 
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace TaskManagement;

[DependsOn(
    typeof(TaskManagementDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(TaskManagementApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpAutoMapperModule) 
    )]
public class TaskManagementApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            // Đăng ký Profile để ABP biết cách thực hiện ánh xạ
            options.AddMaps<TaskManagementApplicationModule>();
        });
    }
}