using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using TaskManagement.Permissions;
using TaskManagement.Projects;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Tasks
{
    [Authorize]
    public class TaskAppService : ApplicationService, ITaskAppService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;
        private readonly IRepository<Project, Guid> _projectRepository;

        // NOTE: Inject thêm 3 repository để truy xuất dữ liệu liên quan đến Task, User và Project
        public TaskAppService(
            ITaskRepository repository,
            IRepository<IdentityUser, Guid> userRepository,
            IRepository<Project, Guid> projectRepository)
        {
            _taskRepository = repository;
            _userRepository = userRepository;
            _projectRepository = projectRepository;
        }

        public async Task<TaskDto> GetAsync(Guid id)
        {
            // NOTE: WithDetailsAsync load thông tin kèm Assignments để kiểm tra quyền và hiển thị danh sách người được giao
            // Không có sẽ null
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var task = await queryable.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) throw new UserFriendlyException(L["TaskManagement::TaskNotFound"]);

            // Check quyền xem task: only Boss can view
            var canView = await CanViewTask(task);
            if (!canView) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);

            // Map task sang TaskDto tự động những gì giống nhau
            // sau đó lấy danh sách userId được giao và truy xuất tên để hiển thị
            var dto = ObjectMapper.Map<AppTask, TaskDto>(task);

            // Query DB lấy các User có Id nằm trong list assignedUserIds
            // Tương đương SQL: WHERE Id IN (Guid1, Guid2, Guid3...)
            var assignedUserIds = task.Assignments.Select(a => a.UserId).ToList();
            var users = await _userRepository.GetListAsync(u => assignedUserIds.Contains(u.Id));

            // NOTE: Phần liên quan đến User phải query thêm rồi gán tay vì ObjectMapper không thể tự biết cách lấy tên từ Id!
            // gán danh sách userId và userName vào DTO để trả về cho client
            // lấy từ list gán vào DTO, sau đó dùng string.Join để nối tên thành chuỗi hiển thị
            dto.AssignedUserIds = assignedUserIds;
            dto.AssignedUserNames = users.Select(u => u.UserName).ToList();
            dto.AssignedUserName = string.Join(", ", dto.AssignedUserNames);
            
            return dto;
        }

        public async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            // NOTE: Load kèm assigments => kiểm tra quyền và tránh lazy loading n+1 query
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var currentUserId = CurrentUser.Id;
            // Kiểm tra quyền xem
            var isBoss = await IsBossOfProject(input.ProjectId ?? Guid.Empty);

            // Không có quyền => chỉ xem task của mình
            if (!isBoss)
            {
                queryable = queryable.Where(t => 
                    t.Assignments.Any(a => a.UserId == currentUserId) || 
                    t.CreatorId == currentUserId);
            }
            // Lọc theo projectId và trạng thái duyệt
            // mặc định true nếu không truyền
            queryable = queryable.Where(t => t.ProjectId == input.ProjectId && t.IsApproved == (input.IsApproved ?? true));

            // Lọc theo keyword nếu có truyền vào
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                queryable = queryable.Where(t => t.Title.Contains(input.FilterText));
            }

            // Đếm tổng khi phân trang - FE hiển thị theo tổng
            // Sắp xếp + phân trang - PageBy tự tính/skip từ SkipCount và MaxResultCount
            var totalCount = await AsyncExecuter.CountAsync(queryable);
            var tasks = await AsyncExecuter.ToListAsync(queryable.OrderBy(input.Sorting ?? "CreationTime DESC").PageBy(input.SkipCount, input.MaxResultCount));
            
            var taskDtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

            // NOTE: Vì Mapper không tự map thuộc tính khác được (AssignedUserName,..)
            // Nếu list dài -> gây n+1 query do mỗi task phải query 1 lần nếu query trong loop

            // FIX: Cách tối ưu: Dùng LINQ chain gom tất cả userId cần thiết lại, query 1 lần sau đó map từ list đã có sẵn trong memory
            // Bước 1: Gom tất cả userId từ tất cả tasks lại 
            // SelectMany → gộp tất cả thành 1 list phẳng thay vì từng list riêng lẻ -> Kết quả: List<Guid>
            var allUserIds = tasks.SelectMany(t => t.Assignments.Select(a => a.UserId))
                                  .Distinct()
                                  .ToList();
            // Bước 2: Query 1 lần - lấy TẤT CẢ users liên quan
            var allUsers = await _userRepository.GetListAsync(u => allUserIds.Contains(u.Id));

            // Bước 3: Loop trên taskDtos như cũ, nhưng KHÔNG query DB nữa
            // Chỉ lọc từ allUsers đã có sẵn trong memory
            foreach (var dto in taskDtos)
            {
                var taskObj = tasks.First(t => t.Id == dto.Id);
                var assignedIds = taskObj.Assignments.Select(a => a.UserId).ToList();

                // Thay vì query DB → lọc từ allUsers trong memory
                var userNames = allUsers.Where(u => assignedIds.Contains(u.Id)).Select(u => u.UserName);

                dto.AssignedUserIds = assignedIds;
                dto.AssignedUserName = userNames.Any() ? string.Join(", ", userNames) : L["Unassigned"]; // localizer dịch theo ngôn ngữ hiện tại
            }

            return new PagedResultDto<TaskDto>(totalCount, taskDtos);
        }

        public async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            // Kiểm tra trùng lặp - tính năng khá thừa vì khó có task nào trùng hoàn toàn
            // Nếu trùng có thể là task dạng weekly report chẳng hạn

            var isBoss = await IsBossOfProject(input.ProjectId);
            // Kiểm tra thêm quyền Approve chung của hệ thống (Ví dụ Admin hệ thống)
            var hasApprovePermission = await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Tasks.Approve);
            
            if (!isBoss)
            {
                var projectMembers = await _projectRepository.WithDetailsAsync(p => p.Members);
                var isMember = projectMembers.Any(p => p.Id == input.ProjectId && p.Members.Any(m => m.UserId == CurrentUser.Id));
                if (!isMember) throw new UserFriendlyException(L["TaskManagement::NoPermissionToCreateTask"]);
            }

            var task = new AppTask(
                    GuidGenerator.Create(),
                    input.ProjectId,
                    input.Title,
                    input.Status, 
                    input.Weight  
                );

            task.Description = input.Description;
            task.DueDate = input.DueDate;
            
            task.IsApproved = isBoss || hasApprovePermission;

            foreach (var userId in input.AssignedUserIds)
            {
                task.AddAssignment(userId);
            }

            await _taskRepository.InsertAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task<TaskDto> ApproveAsync(Guid id)
        {   
            // Lấy task theo id, chỉ boss được duyệt
            var task = await _taskRepository.GetAsync(id);
            if (!await IsBossOfProject(task.ProjectId)) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);

            // Duyệt thì thay đổi trạng thái 
            task.IsApproved = true;
            task.IsRejected = false;

            // Cập nhật vào trạng thái sau duyệt
            await _taskRepository.UpdateAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task RejectAsync(Guid id)
        {   
            // Tương tự Approve nhưng ngược lại
            var task = await _taskRepository.GetAsync(id);
            if (!await IsBossOfProject(task.ProjectId)) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);

            task.IsRejected = true;
            await _taskRepository.UpdateAsync(task);
        }

        public async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            // Chọn ra task đầu tiên tìm được hoặc giá trị mặc định 
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var task = await queryable.FirstOrDefaultAsync(t => t.Id == id);
            
            if (task == null) throw new UserFriendlyException(L["TaskManagement::TaskNotFound"]);

            // Các biến boolean (phân quyền)
            bool isBoss = await IsBossOfProject(task.ProjectId);
            bool isAssignedToMe = task.Assignments.Any(a => a.UserId == CurrentUser.Id);
            bool isCreator = task.CreatorId == CurrentUser.Id;
            
            // Không phải Boss
            if (!isBoss)
            {
                // Task đã được duyệt
                if (task.IsApproved)
                {
                    // Kiểm tra xem có được giao task này không? 
                    // Không -> không có quyền
                    // Không thể edit sau khi đã được duyệt
                    if (!isAssignedToMe) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);
                    if (input.Title != task.Title || input.Description != task.Description) 
                        throw new UserFriendlyException(L["TaskManagement::CannotEditContentAfterApproval"]);
                }
                // Task chưa được duyệt 
                else
                {   
                    // Người dùng có phải người tạo không?
                    // Không -> không có quyền
                    // Không thể thay đổi trạng thái khi đã được duyệt
                    if (!isCreator) throw new UserFriendlyException(L["TaskManagement::NoPermission"]);
                    if (input.Status != task.Status) 
                        throw new UserFriendlyException(L["TaskManagement::CannotChangeStatusBeforeApproval"]);
                }
            }

            task.Title = input.Title;
            task.Description = input.Description;
            task.Status = input.Status;
            task.Weight = input.Weight;
            task.DueDate = input.DueDate; 
            
            // Nếu là boss -> gỡ hết assignments
            // Duyệt qua danh sách user và gán phân công mới
            if (isBoss)
            {
                task.ClearAssignments();
                foreach (var userId in input.AssignedUserIds) task.AddAssignment(userId);
            }

            await _taskRepository.UpdateAsync(task);
            return ObjectMapper.Map<AppTask, TaskDto>(task);
        }

        public async Task DeleteAsync(Guid id, string reason)
        {
            // Lấy task theo id
            var task = await _taskRepository.GetAsync(id);
            if (string.IsNullOrWhiteSpace(reason)) throw new UserFriendlyException(L["TaskManagement::DeletionReasonRequired"]);

            // Kiểm tra quyền theo project
            bool isBoss = await IsBossOfProject(task.ProjectId);

            // Nếu không phải Boss -> chỉ được xóa nếu chưa duyệt và là người tạo
            if (!isBoss)
            {
                if (task.IsApproved) throw new UserFriendlyException(L["TaskManagement::NoPermissionToDeleteApprovedTask"]);
                if (task.CreatorId != CurrentUser.Id) throw new UserFriendlyException(L["TaskManagement::No Permission"]);
            }
            // Nếu là Boss -> không xoá task đã hoàn thành
            else
            {
                if (task.Status == TaskStatus.Completed) throw new UserFriendlyException(L["TaskManagement::CannotDeleteCompletedTask"]);
            }

            // NOTE: Gán lý do xóa vào trường DeletionReason trước khi xóa để lưu lại thông tin
            // Cập nhật trước khi xóa thì mới lưu được reason
            task.DeletionReason = reason;
            await _taskRepository.UpdateAsync(task); 
            await _taskRepository.DeleteAsync(id);
        }

        public async Task<PagedResultDto<TaskDto>> GetOverdueListAsync(Guid projectId)
        {
            // Lấy danh sách task quá hạn theo projectId
            var queryable = await _taskRepository.WithDetailsAsync(t => t.Assignments);
            var tasks = await queryable.Where(t => t.ProjectId == projectId && t.DueDate < Clock.Now).ToListAsync();
            
            var dtos = ObjectMapper.Map<List<AppTask>, List<TaskDto>>(tasks);

            // NOTE: Tương tự GetListAsync, gom tất cả userId từ tất cả tasks lại để query 1 lần duy nhất
            // Từ tất cả tasks, lấy ra toàn bộ userId của người được giao → gộp thành 1 list phẳng, bỏ trùng
            var allUserIds = tasks.SelectMany(t => t.Assignments.Select(a => a.UserId))
                                  .Distinct()
                                  .ToList();

            // 2. Dùng list userId đó query DB 1 lần duy nhất lấy thông tin user
            var allUsers = await _userRepository.GetListAsync(u => allUserIds.Contains(u.Id));

            foreach (var dto in dtos)
            {
                var taskObj = tasks.First(t => t.Id == dto.Id);
                var assignedIds = taskObj.Assignments.Select(a => a.UserId).ToList();

                // 3.Với mỗi task, lọc từ list user đã có trong memory → không query DB nữa
                var userNames = allUsers.Where(user => assignedIds.Contains(user.Id)).Select(u => u.UserName);

                if (assignedIds.Any())
                { 
                    dto.AssignedUserName = string.Join(", ", userNames);
                    dto.AssignedUserIds = assignedIds;
                }
                else
                {
                    dto.AssignedUserName = L["Unassigned"];
                }
            }
            return new PagedResultDto<TaskDto>(dtos.Count, dtos);
        }

        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            // Tìm kiếm user -> trả về list người dùng theo id, username
            var users = await _userRepository.GetListAsync();
            return new ListResultDto<UserLookupDto>(users.Select(u => new UserLookupDto { Id = u.Id, UserName = u.UserName }).ToList());
        }

        // Hàm kiểm tra xem người dùng hiện tại có phải là Project Manager
        private async Task<bool> IsBossOfProject(Guid projectId)
        {
            if (projectId == Guid.Empty) return false;
            var project = await _projectRepository.GetAsync(projectId);
            return project.ProjectManagerId == CurrentUser.Id || 
                await AuthorizationService.IsGrantedAsync(TaskManagementPermissions.Projects.Create);
        }

        // Hàm kiểm tra xem người dùng có quyền xem task hay không: Boss có thể xem tất cả
        // còn lại chỉ xem được nếu là người được giao hoặc người tạo
        private async Task<bool> CanViewTask(AppTask task)
        {
            if (await IsBossOfProject(task.ProjectId)) return true;
            return task.Assignments.Any(a => a.UserId == CurrentUser.Id) || task.CreatorId == CurrentUser.Id;
        }
    }
}