using Microsoft.Extensions.Localization;
using TaskManagement.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace TaskManagement;

[Dependency(ReplaceServices = true)]
public class TaskManagementBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<TaskManagementResource> _localizer;

    public TaskManagementBrandingProvider(IStringLocalizer<TaskManagementResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
