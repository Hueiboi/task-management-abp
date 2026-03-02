using Volo.Abp.Settings;

namespace TaskManagement.Settings;

public class TaskManagementSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(TaskManagementSettings.MySetting1));
    }
}
