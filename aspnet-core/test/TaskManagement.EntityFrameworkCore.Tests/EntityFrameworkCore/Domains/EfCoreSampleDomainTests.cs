using TaskManagement.Samples;
using Xunit;

namespace TaskManagement.EntityFrameworkCore.Domains;

[Collection(TaskManagementTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<TaskManagementEntityFrameworkCoreTestModule>
{

}
