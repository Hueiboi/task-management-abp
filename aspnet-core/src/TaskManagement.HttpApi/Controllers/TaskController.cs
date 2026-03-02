using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace TaskManagement.Tasks
{
    [RemoteService(Name = "TaskManagement")] 
    [Route("api/task")] 
    public class TaskController : AbpController, ITaskAppService
    {
        private readonly ITaskAppService _taskAppService;

        public TaskController(ITaskAppService taskAppService)
        {
            _taskAppService = taskAppService;
        }

        [HttpGet("{id}")]
        public Task<TaskDto> GetAsync(Guid id) => _taskAppService.GetAsync(id);

        [HttpGet]
        public Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input) 
            => _taskAppService.GetListAsync(input);

        [HttpPost]
        public Task<TaskDto> CreateAsync(CreateUpdateTaskDto input) 
            => _taskAppService.CreateAsync(input);

        [HttpPut("{id}")]
        public Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input) 
            => _taskAppService.UpdateAsync(id, input);

        // SỬA: Chữ ký hàm phải khớp hoàn toàn với Interface (Guid, string)
        [HttpDelete("{id}")]
        public Task DeleteAsync(Guid id, string reason) 
            => _taskAppService.DeleteAsync(id, reason);

        [HttpGet("user-lookup")]
        public Task<ListResultDto<UserLookupDto>> GetUserLookupAsync() 
            => _taskAppService.GetUserLookupAsync();

        [HttpPost("{id}/approve")]
        public Task<TaskDto> ApproveAsync(Guid id) 
            => _taskAppService.ApproveAsync(id);

        // THÊM: Triển khai RejectAsync
        [HttpPost("{id}/reject")]
        public Task RejectAsync(Guid id) 
            => _taskAppService.RejectAsync(id);

        [HttpGet("overdue/{projectId}")]
        public Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId) 
            => _taskAppService.GetOverdueListAsync(projectId);
    }
}