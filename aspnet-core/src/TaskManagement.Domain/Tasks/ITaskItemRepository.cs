using System;
using System.Collections.Generic;
using System.Threading.Tasks; // NOTE: Thêm using System.Threading.Tasks để hỗ trợ async/await
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tasks
{
    public interface ITaskRepository : IRepository<AppTask, Guid>
    {
        // NOTE: trả về 1 object task bất đồng bộ (async/await)
        // Lấy task theo ID, trả về null nếu không tìm thấy
        Task<AppTask> GetTaskByIdAsync(Guid id);

        // NOTE: Bổ sung tham số projectId và isApproved
        // Lọc theo dự án
        // Lọc trạng thái phê duyệt để phân biệt giữa task chính thức và đề xuất
        // Trả về danh sách task bất đồng bộ với phân trang, sắp xếp và lọc nâng cao
        Task<List<AppTask>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            Guid? projectId = null, 
            string? filter = null,
            TaskStatus? status = null,
            Guid? assignedUserId = null,
            bool? isApproved = null 
        );

        Task<long> GetTotalCountAsync(
            Guid? projectId = null, 
            string? filter = null, 
            TaskStatus? status = null, 
            Guid? assignedUserId = null,
            bool? isApproved = null
        );

        // NOTE: Các hàm CRUD tùy chỉnh giữ nguyên
        Task<AppTask> CreateTaskAsync(AppTask task);
        Task<AppTask> UpdateTaskAsync(AppTask task);
        Task DeleteTaskAsync(Guid id);
    }
}