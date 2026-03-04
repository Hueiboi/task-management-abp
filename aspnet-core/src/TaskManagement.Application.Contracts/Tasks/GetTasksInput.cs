using System;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class GetTasksInput : PagedAndSortedResultRequestDto
    {   
        // NOTE: Có thể thêm các trường lọc khác nếu cần để hỗ trợ việc lọc task theo nhiều tiêu chí khác nhau
        // Lọc task theo dự án
        // Lọc task chính thức hoặc đề xuất
        public Guid? ProjectId { get; set; } 
        public string? FilterText { get; set; }
        public TaskStatus? Status { get; set; }
        public Guid? AssignedUserId { get; set; }
        public bool? IsApproved { get; set; }
    }
}