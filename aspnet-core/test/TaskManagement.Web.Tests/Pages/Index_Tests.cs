using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace TaskManagement.Pages;

public class Index_Tests : TaskManagementWebTestBase
{
    [Fact]
    public async Task Welcome_Page()
    {
        var response = await GetResponseAsStringAsync("/");
        response.ShouldNotBeNull();
    }
}
