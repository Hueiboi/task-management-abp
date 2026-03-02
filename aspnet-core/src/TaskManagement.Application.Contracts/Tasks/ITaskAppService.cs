using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks
{
    public interface ITaskAppService : IApplicationService
    {
        Task<TaskDto> GetAsync(Guid id);
        
        Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input);
        
        Task<TaskDto> CreateAsync(CreateUpdateTaskDto input);
        
        Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input);
        
        // CHỈ GIỮ LẠI hàm xóa có lý do để thống nhất toàn hệ thống
        Task DeleteAsync(Guid id, string reason);
        
        Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
        
        Task<TaskDto> ApproveAsync(Guid id); 

        // THÊM: Định nghĩa hàm từ chối để fix lỗi "does not contain a definition"
        Task RejectAsync(Guid id);

        Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId);
    }
}