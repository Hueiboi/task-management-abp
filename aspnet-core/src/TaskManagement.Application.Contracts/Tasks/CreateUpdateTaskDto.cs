using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManagement.Tasks
{
public class CreateUpdateTaskDto
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        public int Weight { get; set; } = 1;
        
        [Required]
        [MinLength(1, ErrorMessage = "TaskManagement::AtLeastOneUserRequired")] 
        public List<Guid> AssignedUserIds { get; set; } = new();
    }
}