using Volo.Abp.Application.Dtos;

namespace TaskManagement.Projects
{
    public class GetProjectsInput : PagedAndSortedResultRequestDto
    {
        public string? FilterText { get; set; }
    }
}