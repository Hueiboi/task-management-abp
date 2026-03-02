using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tasks
{
    public interface ITaskRepository : IRepository<AppTask, Guid>
    {
        Task<AppTask> GetTaskByIdAsync(Guid id);
        
        // Bổ sung tham số projectId và isApproved
        Task<List<AppTask>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            Guid? projectId = null, // Lọc theo dự án
            string? filter = null,
            TaskStatus? status = null,
            Guid? assignedUserId = null,
            bool? isApproved = null // Lọc task chính thức hoặc đề xuất
        );

        Task<long> GetTotalCountAsync(
            Guid? projectId = null, 
            string? filter = null, 
            TaskStatus? status = null, 
            Guid? assignedUserId = null,
            bool? isApproved = null
        );

        // Các hàm CRUD tùy chỉnh giữ nguyên
        Task<AppTask> CreateTaskAsync(AppTask task);
        Task<AppTask> UpdateTaskAsync(AppTask task);
        Task DeleteTaskAsync(Guid id);
    }
}