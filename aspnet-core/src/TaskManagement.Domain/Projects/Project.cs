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
        
        // Quản lý dự án (Bắt buộc)
        public Guid ProjectManagerId { get; set; }
        
        // Tiến độ dự án (tính toán dựa trên Task hoàn thành)
        public float Progress { get; set; }

        // Danh sách thành viên tham gia dự án
        public virtual ICollection<ProjectMember> Members { get; protected set; }

        protected Project() { }

        public Project(Guid id, string name, Guid projectManagerId) : base(id)
        {
            Name = name;
            ProjectManagerId = projectManagerId;
            Members = new Collection<ProjectMember>();
        }
    }

    // Thực thể phụ để lưu danh sách người tham gia
    public class ProjectMember : Entity
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }

        public override object[] GetKeys() => new object[] { ProjectId, UserId };
    }
}