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

    // Hàm GetAsync đã được cập nhật để tính toán khối lượng công việc dựa trên trọng số của các task
    public async Task<ProjectDto> GetAsync(Guid id)
    {
        // Sử dụng WithDetails để load luôn danh sách thành viên của dự án, tránh N+1 query
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null) 
            throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        // Kiểm tra quyền truy cập trước khi trả về dữ liệu
        await CheckProjectAccessAsync(project);
        
        var dto = ObjectMapper.Map<Project, ProjectDto>(project);
        
        // Hàm tính toán khối lượng công việc
        await CalculateProjectStatsAsync(dto);

        // Gán tên người quản lý dự án (Mapper không tự động map được)
        var manager = await _userRepository.FindAsync(project.ProjectManagerId);
        dto.ProjectManagerName = manager?.UserName ?? "Unknown";
        dto.MemberIds = project.Members.Select(m => m.UserId).ToList();
        
        return dto;
    }

    public async Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);

        // Áp dụng bộ lọc tìm kiếm những dự án có tên hoặc mô tả chứa chuỗi tìm kiếm (nếu có)
        queryable = queryable.WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), 
            x => x.Name.Contains(input.FilterText) || (x.Description != null && x.Description.Contains(input.FilterText)));
        
        //  Phân quyền truy cập
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        // NOTE: Nếu không phải admin, chỉ trả về những dự án mà người dùng hiện tại là quản lý hoặc thành viên
        if (!isAdmin)
        {
            queryable = queryable.Where(p => 
                p.ProjectManagerId == currentUserId || 
                p.Members.Any(m => m.UserId == currentUserId));
        }

        // Đếm tổng records TRƯỚC khi phân trang — dùng cho FE hiển thị "tổng X trang"
        // AsyncExecuter là wrapper của ABP để execute IQueryable bất đồng bộ
        var totalCount = await AsyncExecuter.CountAsync(queryable);

        // Sắp xếp + phân trang rồi mới execute query xuống DB
        // OrderBy: mặc định sort theo CreationTime giảm dần nếu không truyền
        // PageBy: tự tính OFFSET và LIMIT từ SkipCount + MaxResultCount
        var projects = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(input.Sorting ?? "CreationTime DESC")
                     .PageBy(input.SkipCount, input.MaxResultCount)
        );

        var projectDtos = ObjectMapper.Map<List<Project>, List<ProjectDto>>(projects);

        foreach (var dto in projectDtos)
        {
            var projectEntity = projects.First(p => p.Id == dto.Id);
            
            // Gán thông tin thành viên
            dto.MemberIds = projectEntity.Members.Select(m => m.UserId).ToList();
            dto.MemberCount = dto.MemberIds.Count;

            await CalculateProjectStatsAsync(dto);
            
            var manager = await _userRepository.FindAsync(dto.ProjectManagerId);
            dto.ProjectManagerName = manager?.UserName;
        }

        return new PagedResultDto<ProjectDto>(totalCount, projectDtos);
    }

    // Hàm tính toán khối lượng công việc dựa trên trọng số của các task
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

    // Hàm lấy danh sách Project Manager để hiển thị dropdown khi tạo/sửa dự án
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
        //  Tạo object mới, description gán qua object initializer vì nó là property nullable
        var project = new Project(
            GuidGenerator.Create(),
            input.Name,
            input.ProjectManagerId
            ) { Description = input.Description };
        // Thêm thành viên vào dự án sau khi đã có Id của dự án để tránh lỗi foreign key
        // Thêm từng member theo danh sách MemberIds truyền vào
        foreach (var userId in input.MemberIds)
        {
            project.Members.Add(new ProjectMember { ProjectId = project.Id, UserId = userId });
        }
        await _projectRepository.InsertAsync(project, autoSave: true);
        return ObjectMapper.Map<Project, ProjectDto>(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
    {
        // Sử dụng WithDetails để load luôn danh sách thành viên của dự án, tránh N+1 query khi cập nhật
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        // Kiểm tra quyền truy cập trước khi cho phép cập nhật
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (!isAdmin && project.ProjectManagerId != CurrentUser.Id)
            throw new UserFriendlyException(L["TaskManagement::NoPermissionToEdit"]);

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

    [Authorize(TaskManagementPermissions.Projects.Delete)]
    public async Task DeleteAsync(Guid id) => await _projectRepository.DeleteAsync(id);

    // Hàm lấy danh sách thành viên của dự án để hiển thị dropdown khi tạo/sửa task
    public async Task<ListResultDto<UserLookupDto>> GetMembersLookupAsync(Guid projectId)
    {
        // Sử dụng WithDetails để load luôn danh sách thành viên của dự án
        var queryable = await _projectRepository.WithDetailsAsync(p => p.Members);
        var project = await queryable.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) throw new UserFriendlyException(L["TaskManagement::ProjectNotFound"]);

        // Kiểm tra quyền truy cập trước khi trả về danh sách thành viên
        // Chỉ admin, PM hoặc thành viên của dự án mới có quyền xem danh sách thành viên
        await CheckProjectAccessAsync(project);

        // Lấy danh sách userId của tất cả thành viên của dự án
        // Thêm cả Project Manager vào danh sách để giao diện có thể chọn PM làm người thực hiện task 
        var memberIds = project.Members.Select(m => m.UserId).ToList();
        memberIds.Add(project.ProjectManagerId);

        // Lấy thông tin user từ repository dựa trên danh sách userId đã lấy được
        // ProjectMember và TaskAssignment đều chỉ lưu UserId 
        // muốn có tên thì luôn phải query thêm _userRepository. 
        var users = await _userRepository.GetListAsync(u => memberIds.Contains(u.Id));
        return new ListResultDto<UserLookupDto>(users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList());
    }

    // Kiểm tra quyền truy cập
    // Nếu là admin thì có quyền truy cập tất cả dự án
    // Nếu không phải admin thì chỉ có quyền truy cập những dự án mà người dùng hiện tại là quản lý hoặc thành viên
    private async Task CheckProjectAccessAsync(Project project)
    {
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        if (isAdmin) return;
        if (project.ProjectManagerId != CurrentUser.Id && !project.Members.Any(m => m.UserId == CurrentUser.Id))
            throw new UserFriendlyException(L["TaskManagement::NoPermissionToAccess"]);
    }
}