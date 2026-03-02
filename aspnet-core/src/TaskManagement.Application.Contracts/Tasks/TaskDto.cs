using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks;

public class TaskDto : AuditedEntityDto<Guid>
{
    public string Title { get; set; }
    public string? Description { get; set; } 
    public TaskStatus Status { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public string? DeletionReason { get; set; }

    public int Weight { get; set; }

    // --- CẬP NHẬT ĐỂ SỬA LỖI CS1061 ---
    // Danh sách ID dùng để Frontend "tick sẵn" nhân viên trong ô chọn
    public List<Guid> AssignedUserIds { get; set; } = new(); 
    
    // Danh sách Tên dùng cho các logic hiển thị chi tiết khác
    public List<string> AssignedUserNames { get; set; } = new(); 
    
    // Chuỗi tên hiển thị nhanh trên bảng (ví dụ: "Employee1, Employee2")
    public string? AssignedUserName { get; set; } 
}