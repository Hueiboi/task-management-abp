using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks
{
    // NOTE: Định nghĩa ITaskAppService để quản lý các tác vụ liên quan đến Task
    // bao gồm CRUD, phê duyệt, từ chối và lấy danh sách quá hạn
    // Nhận input từ phía client và trả về kết quả phù hợp (TaskDto), đảm bảo tính nhất quán trong toàn hệ thống
    public interface ITaskAppService : IApplicationService
    {
        Task<TaskDto> GetAsync(Guid id);
        
        Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input);
        
        Task<TaskDto> CreateAsync(CreateUpdateTaskDto input);
        
        Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input);
        
        // NOTE: CHỈ GIỮ LẠI hàm xóa có lý do để thống nhất toàn hệ thống
        Task DeleteAsync(Guid id, string reason);
        
        Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
        
        Task<TaskDto> ApproveAsync(Guid id); 

        // ADD: Định nghĩa hàm từ chối để fix lỗi "does not contain a definition"
        Task RejectAsync(Guid id);

        Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId);
    }
}