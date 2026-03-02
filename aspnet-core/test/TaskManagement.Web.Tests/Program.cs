using Microsoft.AspNetCore.Builder;
using TaskManagement;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();

builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("TaskManagement.Web.csproj");
await builder.RunAbpModuleAsync<TaskManagementWebTestModule>(applicationName: "TaskManagement.Web" );

public partial class Program
{
}
