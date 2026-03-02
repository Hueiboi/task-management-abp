using Xunit;

namespace TaskManagement.EntityFrameworkCore;

[CollectionDefinition(TaskManagementTestConsts.CollectionDefinitionName)]
public class TaskManagementEntityFrameworkCoreCollection : ICollectionFixture<TaskManagementEntityFrameworkCoreFixture>
{

}
