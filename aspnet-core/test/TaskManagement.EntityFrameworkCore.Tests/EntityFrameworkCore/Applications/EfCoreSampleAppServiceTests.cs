using TaskManagement.Samples;
using Xunit;

namespace TaskManagement.EntityFrameworkCore.Applications;

[Collection(TaskManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<TaskManagementEntityFrameworkCoreTestModule>
{

}
