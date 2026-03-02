using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using TaskManagement.Tasks; // Để dùng UserLookupDto

namespace TaskManagement.Projects
{
    [RemoteService(Name = "TaskManagement")]
    [Route("api/project")]
    public class ProjectController : AbpController, IProjectAppService
    {
        private readonly IProjectAppService _projectAppService;

        public ProjectController(IProjectAppService projectAppService)
        {
            _projectAppService = projectAppService;
        }

        [HttpGet("{id}")]
        public Task<ProjectDto> GetAsync(Guid id) => _projectAppService.GetAsync(id);

        // SỬA: Chữ ký hàm phải khớp với Interface mới (GetProjectsInput)
        [HttpGet]
        public Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input) 
            => _projectAppService.GetListAsync(input);

        [HttpPost]
        public Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input) 
            => _projectAppService.CreateAsync(input);

        [HttpPut("{id}")]
        public Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input) 
            => _projectAppService.UpdateAsync(id, input);

        [HttpDelete("{id}")]
        public Task DeleteAsync(Guid id) => _projectAppService.DeleteAsync(id);

        [HttpGet("members/{projectId}")]
        public Task<ListResultDto<UserLookupDto>> GetMembersLookupAsync(Guid projectId) 
            => _projectAppService.GetMembersLookupAsync(projectId);

        // THÊM: Endpoint để lấy danh sách Project Manager (Sửa lỗi CS0535)
        [HttpGet("project-managers-lookup")]
        public Task<ListResultDto<UserLookupDto>> GetProjectManagersLookupAsync() 
            => _projectAppService.GetProjectManagersLookupAsync();
    }
}