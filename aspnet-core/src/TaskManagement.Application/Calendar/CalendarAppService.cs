// aspnet-core\src\TaskManagement.Application\Calendar\CalendarAppService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Projects;
using TaskManagement.Tasks;
using TaskManagement.Permissions;

namespace TaskManagement.Calendar;

[Authorize]
[RemoteService(IsEnabled = true)] // Ép hệ thống nhận diện
public class CalendarAppService : ApplicationService, ICalendarAppService
{
    private readonly IRepository<AppTask, Guid> _taskRepository;
    private readonly IRepository<Project, Guid> _projectRepository;

    public CalendarAppService(
        IRepository<AppTask, Guid> taskRepository,
        IRepository<Project, Guid> projectRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
    }

    // Nhận trực tiếp startDate và endDate
    public async Task<List<TaskDto>> GetCalendarTasksAsync(DateTime? startDate, DateTime? endDate)
    {
        var currentUserId = CurrentUser.Id;

        // Load tasks kèm Assignments, chỉ lấy task đã duyệt và có DueDate
        var taskQuery = await _taskRepository.WithDetailsAsync(t => t.Assignments);
        var queryable = taskQuery.Where(t => t.IsApproved && t.DueDate != null);

        // Lọc theo khoảng thời gian nếu có truyền vào
        if (startDate.HasValue)
            queryable = queryable.Where(t => t.DueDate >= startDate.Value);
        if (endDate.HasValue)
            queryable = queryable.Where(t => t.DueDate <= endDate.Value);

        // Dùng constant thay vì hardcode string — tránh typo, dễ maintain
        bool isAdmin = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Approve);

        if (!isAdmin)
        {
            // Không phải Admin → chỉ thấy task của project mình quản lý
            // hoặc task được giao trực tiếp cho mình
            var projectQuery = await _projectRepository.GetQueryableAsync();
            var managedProjectIds = projectQuery
                .Where(p => p.ProjectManagerId == currentUserId)
                .Select(p => p.Id);

            queryable = queryable.Where(t =>
                managedProjectIds.Contains(t.ProjectId) ||
                t.Assignments.Any(a => a.UserId == currentUserId)
            );
        }

        var tasks = await AsyncExecuter.ToListAsync(queryable);
        var taskDtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

        // FIX N+1: Gom tất cả projectId → query 1 lần thay vì query trong vòng lặp
        var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
        var projects = await _projectRepository.GetListAsync(p => projectIds.Contains(p.Id));

        // Gán tên Project vào AssignedUserName để hiển thị trên Calendar
        // NOTE: Dùng tạm field AssignedUserName để tránh thay đổi DTO lúc này
        foreach (var dto in taskDtos)
        {
            var project = projects.FirstOrDefault(p => p.Id == dto.ProjectId);
            dto.AssignedUserName = project?.Name;
        }

        return taskDtos;
    }
}