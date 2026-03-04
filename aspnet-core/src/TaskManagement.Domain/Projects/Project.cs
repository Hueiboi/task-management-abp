using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities; 

namespace TaskManagement.Projects
{
    public class Project : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; }
        public Guid ProjectManagerId { get; set; } 
        public float Progress { get; set; } 
        public virtual ICollection<ProjectMember> Members { get; protected set; }

        // NOTE: Constructor protected để chỉ có thể tạo mới thông qua Factory hoặc Repository, đảm bảo tính toàn vẹn của dữ liệu
        protected Project() { }

        public Project(Guid id, string name, Guid projectManagerId) : base(id)
        {
            Name = name;
            ProjectManagerId = projectManagerId;
            Members = new Collection<ProjectMember>();
        }
    }

    // NOTE: Thực thể phụ để lưu danh sách người tham gia
    public class ProjectMember : Entity
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }

        public override object[] GetKeys() => new object[] { ProjectId, UserId };
    }
}