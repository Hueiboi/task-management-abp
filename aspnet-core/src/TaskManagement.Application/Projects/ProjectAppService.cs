// aspnet-core\src\TaskManagement.Application\Projects\ProjectAppService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore; 
using TaskManagement.Tasks;

namespace TaskManagement.Projects;

[Authorize]
public class ProjectAppService : ApplicationService, IProjectAppService
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Tasks.AppTask, Guid> _taskRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IdentityUserManager _userManager;

    public ProjectAppService(
        IRepository<Project, Guid> projectRepository,
        IRepository<Tasks.AppTask, Guid> taskRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IdentityUserManager userManager)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _userManager = userManager;
    }

    public async Task<ProjectDto> GetAsync(Guid id)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null) 
            throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        await CheckProjectAccessAsync(project);
        
        var dto = ObjectMapper.Map<Project, ProjectDto>(project);
        
        // SỬ DỤNG HÀM TÍNH TOÁN RIÊNG
        await CalculateProjectStatsAsync(dto);

        var manager = await _userRepository.FindAsync(project.ProjectManagerId);
        dto.ProjectManagerName = manager?.UserName ?? "Unknown";
        dto.MemberIds = project.Members.Select(m => m.UserId).ToList();
        
        return dto;
    }

    public async Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);

        // 1. Áp dụng bộ lọc tìm kiếm
        queryable = queryable.WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), 
            x => x.Name.Contains(input.FilterText) || (x.Description != null && x.Description.Contains(input.FilterText)));

        // 2. Phân quyền truy cập
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (!isAdmin)
        {
            queryable = queryable.Where(p => 
                p.ProjectManagerId == currentUserId || 
                p.Members.Any(m => m.UserId == currentUserId));
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var projects = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(input.Sorting ?? "CreationTime DESC").PageBy(input.SkipCount, input.MaxResultCount)
        );

        var projectDtos = ObjectMapper.Map<List<Project>, List<ProjectDto>>(projects);

        foreach (var dto in projectDtos)
        {
            var projectEntity = projects.First(p => p.Id == dto.Id);
            
            // Gán thông tin thành viên
            dto.MemberIds = projectEntity.Members.Select(m => m.UserId).ToList();
            dto.MemberCount = dto.MemberIds.Count;

            // SỬ DỤNG HÀM TÍNH TOÁN RIÊNG THEO TRỌNG SỐ
            await CalculateProjectStatsAsync(dto);
            
            var manager = await _userRepository.FindAsync(dto.ProjectManagerId);
            dto.ProjectManagerName = manager?.UserName;
        }

        return new PagedResultDto<ProjectDto>(totalCount, projectDtos);
    }

    /// <summary>
    /// Hàm riêng biệt để tính toán tiến độ dựa trên TRỌNG SỐ (Weight)
    /// </summary>
    private async Task CalculateProjectStatsAsync(ProjectDto dto)
    {
        var tasks = await _taskRepository.GetListAsync(t => t.ProjectId == dto.Id && t.IsApproved);
        
        dto.TaskCount = tasks.Count;
        dto.CompletedTaskCount = tasks.Count(t => t.Status == Tasks.TaskStatus.Completed);

        if (dto.TaskCount > 0)
        {
            // Tổng trọng số của tất cả các task
            int totalWeight = tasks.Sum(t => t.Weight);
            
            // Tổng trọng số của các task đã xong
            int completedWeight = tasks
                .Where(t => t.Status == Tasks.TaskStatus.Completed)
                .Sum(t => t.Weight);

            // Tiến độ (%) = (Trọng số đã xong / Tổng trọng số) * 100
            dto.Progress = totalWeight > 0 
                ? (float)Math.Round((double)completedWeight / totalWeight * 100, 2) 
                : 0;
        }
        else
        {
            dto.Progress = 0;
        }
    }

    public async Task<ListResultDto<UserLookupDto>> GetProjectManagersLookupAsync()
    {
        var pmUsers = await _userManager.GetUsersInRoleAsync("Project manager");
        return new ListResultDto<UserLookupDto>(
            pmUsers.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList()
        );
    }

    [Authorize(TaskManagementPermissions.Projects.Create)]
    public async Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input)
    {
        var project = new Project(GuidGenerator.Create(), input.Name, input.ProjectManagerId) { Description = input.Description };
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }
        await _projectRepository.InsertAsync(project, autoSave: true);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (!isAdmin && project.ProjectManagerId != CurrentUser.Id)
            throw new UserFriendlyException(L["TaskManagement::NoPermissionToEditProject"]);

        project.Name = input.Name;
        project.Description = input.Description;
        project.ProjectManagerId = input.ProjectManagerId;

        project.Members.Clear();
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }

        await _projectRepository.UpdateAsync(project, autoSave: true);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    [Authorize(TaskManagementPermissions.Projects.Create)]
    public async Task DeleteAsync(Guid id) => await _projectRepository.DeleteAsync(id);

    public async Task<ListResultDto<UserLookupDto>> GetMembersLookupAsync(Guid projectId)
    {
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        await CheckProjectAccessAsync(project);

        var memberIds = project.Members.Select(m => m.UserId).ToList();
        memberIds.Add(project.ProjectManagerId); 

        var users = await _userRepository.GetListAsync(u => memberIds.Contains(u.Id));
        return new ListResultDto<UserLookupDto>(users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList());
    }

    private async Task CheckProjectAccessAsync(Project project)
    {
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (isAdmin) return;
        if (project.ProjectManagerId != CurrentUser.Id && !project.Members.Any(m => m.UserId == CurrentUser.Id))
            throw new UserFriendlyException(L["TaskManagement::NoPermissionToAccessProject"]);
    }
}