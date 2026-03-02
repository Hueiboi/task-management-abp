using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core; 
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TaskManagement.Tasks
{
    public class EfCoreTaskRepository : EfCoreRepository<TaskManagementDbContext, AppTask, Guid>, ITaskRepository
    {
        public EfCoreTaskRepository(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
            : base(dbContextProvider) { }

        // Lấy chi tiết Task kèm theo danh sách Assignments để tránh lỗi Lazy Loading
        public async Task<AppTask> GetTaskByIdAsync(Guid id) 
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Include(x => x.Assignments) 
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<AppTask>> GetListAsync(
            int skipCount, 
            int maxResultCount, 
            string sorting, 
            Guid? projectId = null, 
            string? filter = null, 
            TaskStatus? status = null, 
            Guid? assignedUserId = null,
            bool? isApproved = null) 
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Include(x => x.Assignments) // Eager loading cho quan hệ nhiều người
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId)
                .WhereIf(isApproved.HasValue, x => x.IsApproved == isApproved)
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x => 
                    x.Title.Contains(filter) || (x.Description != null && x.Description.Contains(filter)))
                .WhereIf(status.HasValue, x => x.Status == status)
                // FIX LỖI: Kiểm tra User có tồn tại trong danh sách Assignments hay không
                .WhereIf(assignedUserId.HasValue, x => 
                    x.Assignments.Any(a => a.UserId == assignedUserId))
                .OrderBy(string.IsNullOrWhiteSpace(sorting) ? "CreationTime desc" : sorting)
                .PageBy(skipCount, maxResultCount)
                .ToListAsync();
        }

        public async Task<long> GetTotalCountAsync(
            Guid? projectId = null, 
            string? filter = null, 
            TaskStatus? status = null, 
            Guid? assignedUserId = null,
            bool? isApproved = null) 
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .WhereIf(projectId.HasValue, x => x.ProjectId == projectId)
                .WhereIf(isApproved.HasValue, x => x.IsApproved == isApproved)
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x => 
                    x.Title.Contains(filter) || (x.Description != null && x.Description.Contains(filter)))
                .WhereIf(status.HasValue, x => x.Status == status)
                // FIX LỖI: Logic tương tự cho hàm đếm
                .WhereIf(assignedUserId.HasValue, x => 
                    x.Assignments.Any(a => a.UserId == assignedUserId))
                .LongCountAsync();
        }

        public async Task<AppTask> CreateTaskAsync(AppTask task) => await InsertAsync(task, autoSave: true);
        
        public async Task<AppTask> UpdateTaskAsync(AppTask task) => await UpdateAsync(task, autoSave: true);
        
        public async Task DeleteTaskAsync(Guid id) => await DeleteAsync(id, autoSave: true);
    }
}