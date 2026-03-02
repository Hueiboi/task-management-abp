// aspnet-core\src\TaskManagement.Application.Contracts\Calendar\ICalendarAppService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Tasks;
using Volo.Abp; 
using Volo.Abp.Application.Services;

namespace TaskManagement.Calendar;

[RemoteService(IsEnabled = true)] 
public interface ICalendarAppService : IApplicationService
{
    Task<List<TaskDto>> GetCalendarTasksAsync(DateTime? startDate, DateTime? endDate);
}