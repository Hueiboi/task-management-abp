using TaskManagement.MongoDB;
using TaskManagement.Samples;
using Xunit;

namespace TaskManagement.MongoDb.Applications;

[Collection(TaskManagementTestConsts.CollectionDefinitionName)]
public class MongoDBSampleAppServiceTests : SampleAppServiceTests<TaskManagementMongoDbTestModule>
{

}
