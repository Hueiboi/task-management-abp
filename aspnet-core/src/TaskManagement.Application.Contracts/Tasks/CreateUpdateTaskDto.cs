xusing System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManagement.Tasks
{
public class CreateUpdateTaskDto
    {
        // NOTE: Không cần Id vì sẽ tự động tạo mới, chỉ cần ProjectId để liên kết với dự án
        // ProjectId là bắt buộc để đảm bảo task được liên kết với một dự án cụ thể
        // Các trường khác sẽ được truyền từ phía client
        // Các trường này sẽ được sử dụng để tạo mới một task trong hệ thống, và sẽ được kiểm tra tính hợp lệ trước khi lưu vào database
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        public int Weight { get; set; } = 1;

        // NOTE: Ít nhất 1 người dùng phải được giao nhiệm vụ
        [Required]
        [MinLength(1, ErrorMessage = "TaskManagement::AtLeastOneUserRequired")] 
        public List<Guid> AssignedUserIds { get; set; } = new();
    }
}