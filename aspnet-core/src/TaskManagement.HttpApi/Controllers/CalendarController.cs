// aspnet-core\src\TaskManagement.HttpApi\Controllers\CalendarController.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using TaskManagement.Tasks; 
using TaskManagement.Calendar; 

namespace TaskManagement.Controllers;

[Route("api/app/calendar")] 
public class CalendarController : AbpController
{
    private readonly ICalendarAppService _calendarAppService;

    public CalendarController(ICalendarAppService calendarAppService)
    {
        _calendarAppService = calendarAppService;
    }

    [HttpGet("tasks")] 
    public Task<List<TaskDto>> GetCalendarTasksAsync(DateTime? startDate, DateTime? endDate)
    {
        return _calendarAppService.GetCalendarTasksAsync(startDate, endDate);
    }
}