using Volo.Abp.Localization;

namespace TaskManagement
{
    [LocalizationResourceName("TaskManagement")]
    public enum TaskStatus
    {
        New = 0,
        InProgress = 1,
        Completed = 2
    }
}
