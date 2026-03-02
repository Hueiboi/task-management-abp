using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Projects;

public class CreateUpdateProjectDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public Guid ProjectManagerId { get; set; }
        public List<Guid> MemberIds { get; set; } = new();
    }