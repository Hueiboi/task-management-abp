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
        
        var taskQuery = await _taskRepository.WithDetailsAsync(t => t.Assignments);
        var queryable = taskQuery.Where(t => t.IsApproved && t.DueDate != null);

        // Lọc trực tiếp bằng biến
        if (startDate.HasValue)
            queryable = queryable.Where(t => t.DueDate >= startDate.Value);
        if (endDate.HasValue)
            queryable = queryable.Where(t => t.DueDate <= endDate.Value);

        bool isAdmin = await AuthorizationService.IsGrantedAsync("TaskManagement.Tasks.Approve");

        if (!isAdmin)
        {
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

        foreach (var dto in taskDtos)
        {
            var project = await _projectRepository.FindAsync(dto.ProjectId);
            dto.AssignedUserName = project?.Name;
        }

        return taskDtos;
    }
}