using TaskManagement.Samples;
using Xunit;

namespace TaskManagement.MongoDB.Domains;

[Collection(TaskManagementTestConsts.CollectionDefinitionName)]
public class MongoDBSampleDomainTests : SampleDomainTests<TaskManagementMongoDbTestModule>
{

}
