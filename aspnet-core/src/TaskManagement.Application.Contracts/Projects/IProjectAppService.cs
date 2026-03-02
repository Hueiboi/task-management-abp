using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using TaskManagement.Tasks; // Để dùng UserLookupDto

namespace TaskManagement.Projects
{
    public interface IProjectAppService : IApplicationService
    {
        Task<ProjectDto> GetAsync(Guid id);

        // SỬA: Dùng GetProjectsInput thay vì PagedAndSortedResultRequestDto để đồng bộ
        Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input);

        Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input);

        Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input);

        Task DeleteAsync(Guid id);

        Task<ListResultDto<UserLookupDto>> GetMembersLookupAsync(Guid projectId);

        // THÊM: Định nghĩa hàm lookup sếp để Angular sinh ra Proxy
        Task<ListResultDto<UserLookupDto>> GetProjectManagersLookupAsync();
    }
}