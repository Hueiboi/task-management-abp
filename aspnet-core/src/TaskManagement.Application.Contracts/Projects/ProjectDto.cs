using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Projects
{
    public class ProjectDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid ProjectManagerId { get; set; }
        public string? ProjectManagerName { get; set; }
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public float Progress { get; set; }

        public int MemberCount { get; set; }
        public List<Guid> MemberIds { get; set; } = new();
    }
}