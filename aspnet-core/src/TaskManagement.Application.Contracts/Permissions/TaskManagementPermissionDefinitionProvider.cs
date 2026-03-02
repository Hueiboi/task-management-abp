using TaskManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace TaskManagement.Permissions;

public class TaskManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(TaskManagementPermissions.GroupName, L("Permission:TaskManagement"));

        // 1. Định nghĩa nhóm quyền DỰ ÁN (Projects)
        var projectsPermission = myGroup.AddPermission(
            TaskManagementPermissions.Projects.Default, 
            L("Permission:Projects"));
            
        projectsPermission.AddChild(TaskManagementPermissions.Projects.Create, L("Permission:Projects.Create"));
        projectsPermission.AddChild(TaskManagementPermissions.Projects.Update, L("Permission:Projects.Update"));
        projectsPermission.AddChild(TaskManagementPermissions.Projects.Delete, L("Permission:Projects.Delete"));

        // 2. Định nghĩa nhóm quyền CÔNG VIỆC (Tasks)
        var tasksPermission = myGroup.AddPermission(
            TaskManagementPermissions.Tasks.Default, 
            L("Permission:Tasks"));

        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Create, L("Permission:Tasks.Create"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Update, L("Permission:Tasks.Update"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Delete, L("Permission:Tasks.Delete"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.UpdateStatus, L("Permission:Tasks.UpdateStatus"));
        
        // Đăng ký quyền Phê duyệt đề xuất
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Approve, L("Permission:Tasks.Approve"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Denied, L("Permission:Tasks.Denied"));

        // 3. CẬP NHẬT: Định nghĩa nhóm quyền LỊCH (Calendar)
        myGroup.AddPermission(
            TaskManagementPermissions.Calendar.Default, 
            L("Permission:Calendar"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TaskManagementResource>(name);
    }
}