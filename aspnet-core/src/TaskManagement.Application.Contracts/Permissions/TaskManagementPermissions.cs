namespace TaskManagement.Permissions;

public static class TaskManagementPermissions
{
    public const string GroupName = "TaskManagement";

    // --- QUYỀN QUẢN LÝ DỰ ÁN ---
    public static class Projects
    {
        public const string Default = GroupName + ".Projects";
        public const string Create = Default + ".Create"; // Chỉ dành cho Admin
        public const string Update = Default + ".Update"; // Admin hoặc PM của dự án đó
        public const string Delete = Default + ".Delete"; // Chỉ dành cho Admin
    }

    // --- QUYỀN QUẢN LÝ CÔNG VIỆC ---
    public static class Tasks
    {
        public const string Default = GroupName + ".Tasks";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string UpdateStatus = Default + ".UpdateStatus";
        
        // QUAN TRỌNG: Quyền phê duyệt đề xuất từ User
        public const string Approve = Default + ".Approve"; 
        public const string Denied = Default + ".Denied"; 
    }

    // NHÓM CALENDAR
    public static class Calendar
    {
        public const string Default = GroupName + ".Calendar";
    }
}