// aspnet-core\src\TaskManagement.Domain\Tasks\AppTask.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks
{
    public class AppTask : FullAuditedAggregateRoot<Guid>
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string? DeletionReason { get; set; }
        
        // Trọng số công việc 
        public int Weight { get; set; }

        public ICollection<TaskAssignment> Assignments { get; set; }

        protected AppTask() 
        {
            Assignments = new Collection<TaskAssignment>();
        }

        public AppTask(Guid id, Guid projectId, string title, TaskStatus status, int weight) : base(id)
        {
            ProjectId = projectId;
            Title = title;
            Status = status; 
            Weight = weight; 
            IsApproved = true; 
            IsRejected = false;
            Assignments = new Collection<TaskAssignment>();
        }

        public void AddAssignment(Guid userId)
        {
            Assignments.Add(new TaskAssignment(Id, userId));
        }

        public void ClearAssignments()
        {
            Assignments.Clear();
        }
    }

    public class TaskAssignment
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }

        public TaskAssignment(Guid taskId, Guid userId)
        {
            TaskId = taskId;
            UserId = userId;
        }
    }
}