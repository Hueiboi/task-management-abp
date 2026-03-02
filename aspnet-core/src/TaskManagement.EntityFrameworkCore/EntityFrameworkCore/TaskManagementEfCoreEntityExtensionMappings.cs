using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace TaskManagement.EntityFrameworkCore;

public static class TaskManagementEfCoreEntityExtensionMappings
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        TaskManagementGlobalFeatureConfigurator.Configure();
        TaskManagementModuleExtensionConfigurator.Configure();
    }
}
