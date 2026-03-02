using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class GetTasksInput : PagedAndSortedResultRequestDto 
    {
        public Guid? ProjectId { get; set; } // Lọc task theo dự án
        public string? FilterText { get; set; }
        public TaskStatus? Status { get; set; }
        public Guid? AssignedUserId { get; set; }
        public bool? IsApproved { get; set; } // Lọc task chính thức hoặc đề xuất
    }
}