using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Localization;
using Volo.Abp.Application.Services;

namespace TaskManagement;

public abstract class TaskManagementAppService : ApplicationService
{
    protected TaskManagementAppService()
    {
        LocalizationResource = typeof(TaskManagementResource);
    }
    
}
