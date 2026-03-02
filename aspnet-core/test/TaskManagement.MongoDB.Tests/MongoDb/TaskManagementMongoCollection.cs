using Xunit;

namespace TaskManagement.MongoDB;

[CollectionDefinition(TaskManagementTestConsts.CollectionDefinitionName)]
public class TaskManagementMongoCollection : TaskManagementMongoDbCollectionFixtureBase
{

}
