// Angular core
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { LocalizationService, ListService, PagedResultDto, CoreModule, PermissionService, ConfigStateService, CurrentUserDto } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
// Ng-Zorro Ant Design components
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzSliderModule } from 'ng-zorro-antd/slider';
// ABP proxy services API & DTO
import { TaskService } from '../proxy/tasks/task.service';
import { TaskDto, TaskStatus, UserLookupDto } from '../proxy/tasks/models';
import { ProjectService } from '../proxy/projects/project.service';
import { Router, ActivatedRoute } from '@angular/router';

// Component decorator với cấu hình
@Component({
  selector: 'app-task',
  standalone: true, // Không cần ngModule riêng, có thể sử dụng trực tiếp
  templateUrl: './task.html', // Template riêng cho component này
  styleUrls: ['../style/global.scss'],
  providers: [ListService], // Serivce tự xử lý pagination, sorting, filtering
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzDrawerModule, NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzModalModule, NzFormModule, NzInputModule, NzSelectModule, NzProgressModule,
    NzToolTipModule, NzAvatarModule, NzDatePickerModule, NzSpinModule, NzDividerModule,
    NzCheckboxModule, NzSliderModule
  ],
})

// Tương đương C# DI
export class TaskComponent implements OnInit {
  public readonly list = inject(ListService); // ListService để quản lý danh sách
  private taskService = inject(TaskService); // Task API
  private projectService = inject(ProjectService); // Project API
  private fb = inject(FormBuilder); // FormBuilder để tạo Reactive Form
  private message = inject(NzMessageService); // Toast notification
  private router = inject(Router); // Router để điều hướng
  private route = inject(ActivatedRoute); // Lấy query params
  private permissionService = inject(PermissionService); // Kiểm tra quyền FE
  private configState = inject(ConfigStateService); // Lấy thông tin cấu hình, user hiện tại
  private localizationService = inject(LocalizationService); // Localization

  // Property và state của component
  weightMarks: any = { 1: '1', 5: '5', 10: '10' }; // Trọng số cho slider trọng số

  // Data từ API
  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 }; // Danh sách task chính
  allTasksForStats: TaskDto[] = []; 
  overdueTasks: TaskDto[] = []; // Tasks quá hạn
  pendingTasks: TaskDto[] = []; // Tasks chờ duyệt
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;
  currentUser: CurrentUserDto;
  projectId: string;
  projectName: string = '';
  projectProgress: number = 0;

  // UI state
  loading = false;
  saving = false;
  isModalOpen = false;
  isEditMode = false;
  isOverdueModalOpen = false;
  isPendingModalOpen = false;
  isReasonModalOpen = false;
  
  selectedTaskId: string | null = null;
  deletionReason: string = '';
  duplicateErrorMessage: boolean = false;

  drawerWidth = 400; 
  isResizing = false;

  hasCreatePermission = false;
  hasApprovePermission = false;

  // Các biến liên quan đến lọc, phân trang, sắp xếp
  filterText = '';
  sorting = 'CreationTime DESC';
  pageIndex = 1;
  pageSize = 10;

  // Stats hiển thị
  totalCount = 0;
  inProgressCount = 0;
  completedCount = 0;
  pendingCount = 0;
  totalWeight = 0;

  showOnlyUncompletedOverdue = true;
  form!: FormGroup;

  // Chạy ngay khi component được khởi tạo (tương tự useEffect(() => {}, []) trong React))
  ngOnInit(): void {
    this.projectId = this.route.snapshot.queryParams['projectId']; // Lấy projectId từ query params
    if (!this.projectId) { this.goBack(); return; } // Nếu không có projectId, quay về trang dự án

    // Check quyền và ẩn/hiện các chức năng tương ứng
    this.currentUser = this.configState.getOne('currentUser');
    this.hasCreatePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Create'); // Kiểm tra quyền tạo task
    this.hasApprovePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Approve'); // Kiểm tra quyền duyệt task

    // Khởi tạo form và load dữ liệu
    this.buildForm();
    this.loadProjectInfo();
    this.loadUsers();
    this.loadTasks();
    this.loadOverdueTasks();
    this.loadPendingTasks();
  }

  // Chỉ show task quá hạn
  get filteredOverdueTasks() {
    if (this.showOnlyUncompletedOverdue) {
      return this.overdueTasks.filter(t => t.status !== TaskStatus.Completed);
    }
    return this.overdueTasks;
  }

  // Điều hướng về trang dự án 
  goBack(): void { this.router.navigate(['/projects']); }

  // Resize 
  startResize(event: MouseEvent): void {
    this.isResizing = true;
    event.preventDefault();
    document.addEventListener('mousemove', this.onMouseMove);
    document.addEventListener('mouseup', this.onMouseUp);
  }

  // Tính toán chiều rộng mới dựa trên vị trí chuột, giới hạn min/max, và cập nhật drawerWidth
  onMouseMove = (event: MouseEvent) => {
    if (!this.isResizing) return;
    const newWidth = window.innerWidth - event.clientX;
    if (newWidth >= 300 && newWidth < window.innerWidth * 0.95) {
      this.drawerWidth = newWidth;
    }
  };

  // Kết thúc resize, gỡ bỏ event listener
  onMouseUp = () => {
    this.isResizing = false;
    document.removeEventListener('mousemove', this.onMouseMove);
    document.removeEventListener('mouseup', this.onMouseUp);
  };

  // Load thông tin dự án bao gồm tên, tiến độ
  private loadProjectInfo(): void {
    this.projectService.get(this.projectId).subscribe(res => {
        this.projectName = res.name;
        this.projectProgress = res.progress > 0 && res.progress <= 1 
            ? Math.round(res.progress * 100) 
            : Math.round(res.progress || 0);
    });
  }

  // Load danh sách users để hiển thị trong dropdown chọn người được giao task
  private loadUsers(): void {
    this.projectService.getMembersLookup(this.projectId).subscribe(res => this.users = res.items);
  }

  // Load tasks với pagination, sorting, filtering, và đồng thời load tất cả tasks để tính stats
  private loadTasks(): void {
    // query 1 lấy tất cả tasks tính stats
    this.taskService.getList({ projectId: this.projectId, isApproved: true, maxResultCount: 1000 })
      .subscribe(res => {
        this.allTasksForStats = res.items;
        this.totalCount = res.totalCount;
        this.calculateStats();
      });
    // query 2 lấy tasks theo trang cho bảng hiển thị
    const streamCreator = (query: any) => {
      this.loading = true;
      return this.taskService.getList({
        ...query,
        projectId: this.projectId,
        filterText: this.filterText, 
        sorting: this.sorting,
        isApproved: true
      });
    };
    // ABP ListService tự xử lý pagination, sorting, filtering dựa trên query từ UI và trả về kết quả cho bảng
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.taskData = res;
      this.loading = false;
    });
  }

  // NOTE: Tính từ allTasksForStats (max 1000 tasks)
  // Nếu project > 1000 tasks thì stats sẽ không chính xác
  private calculateStats(): void {
    this.inProgressCount = this.allTasksForStats.filter(t => t.status === TaskStatus.InProgress).length;
    this.completedCount = this.allTasksForStats.filter(t => t.status === TaskStatus.Completed).length;
    this.totalWeight = this.allTasksForStats.reduce((sum, t) => sum + (t.weight || 1), 0);
  }

  // Tạo reactive form với validation giống CreateUpdateTaskDto BE
  private buildForm(): void {
    this.form = this.fb.group({
      projectId: [this.projectId],
      title: ['', [Validators.required, Validators.maxLength(256)]],
      description: [null],
      status: [TaskStatus.New, Validators.required],
      weight: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
      assignedUserIds: [[], [Validators.required]], 
      dueDate: [null, [Validators.required]], 
      isApproved: [false]
    });
  }

  // Mở modal tạo task mới, reset form về mặc định, và ẩn lỗi trùng lặp nếu có
  createTask(): void {
    this.duplicateErrorMessage = false;
    this.isEditMode = false;
    this.selectedTaskId = null;
    this.form.reset({ 
      status: TaskStatus.New, 
      weight: 1, 
      projectId: this.projectId, 
      isApproved: this.hasApprovePermission, 
      assignedUserIds: [] 
    });
    this.form.enable(); 
    this.isModalOpen = true;
  }

  // Mở modal edit, điền sẵn data task vào form
  // Xử lý đặc biệt DueDate: bỏ 'Z' cuối để tránh lệch timezone khi hiển thị
  editTask(task: TaskDto): void {
    this.duplicateErrorMessage = false;
    this.isEditMode = true;
    this.selectedTaskId = task.id;
    
    let localDueDate = null;
    if (task.dueDate) {
      const dateStr = task.dueDate.endsWith('Z') ? task.dueDate.slice(0, -1) : task.dueDate;
      localDueDate = new Date(dateStr);
    }

    this.form.patchValue({
      projectId: task.projectId,
      title: task.title,
      description: task.description,
      status: task.status,
      weight: task.weight,
      assignedUserIds: task.assignedUserIds,
      dueDate: localDueDate,
      isApproved: task.isApproved 
    });
    
    this.isModalOpen = true;
    this.isPendingModalOpen = false;
  }

  // Sắp xếp và cập nhật lại dữ liệu khi có thay đổi sắp xếp từ UI, nếu không có sắp xếp thì mặc định theo CreationTime DESC
  onSort(sort: { key: string; value: string | null }): void {
    this.sorting = sort.value ? `${sort.key} ${sort.value === 'descend' ? 'DESC' : 'ASC'}` : 'CreationTime DESC';
    this.list.get();
  }

  loadOverdueTasks(): void { this.taskService.getOverdueList(this.projectId).subscribe(res => this.overdueTasks = res.items); }

  loadPendingTasks(): void {
    this.taskService.getList({ projectId: this.projectId, isApproved: false, maxResultCount: 100 })
      .subscribe(res => { this.pendingTasks = res.items; this.pendingCount = res.totalCount; });
  }

  // Khi search thì reset về trang 1 và gọi lại API để lấy dữ liệu mới theo filter
  onSearch(): void { this.list.page = 0; this.list.get(); }

  // Khi đổi trang thì cập nhật pageIndex và gọi lại API để lấy dữ liệu trang mới
  onPageChange(pageIndex: number): void { this.pageIndex = pageIndex; this.list.page = pageIndex - 1; }

  // Khi đổi pageSize thì cập nhật pageSize và gọi lại API để lấy dữ liệu với pageSize mới
  onPageSizeChange(pageSize: number): void { this.pageSize = pageSize; this.list.maxResultCount = pageSize; this.list.get(); }

  // Xác nhận xóa task: kiểm tra quyền, nếu có quyền thì mở modal nhập lý do, nếu không có quyền thì hiện toast lỗi
  confirmDelete(id: string): void {
    const task = this.taskData.items.find(t => t.id === id) || this.pendingTasks.find(t => t.id === id);
    if (!task || !this.canDeleteTask(task)) {
        this.message.error(this.l('::NoPermissionToDeleteTask'));
        return;
    }
    this.selectedTaskId = id;
    this.deletionReason = '';
    this.isReasonModalOpen = true;
  }

  // Gọi API xóa task với lý do, hiện toast, refresh data, đóng modal
  deleteTaskWithReason(): void {
    if (!this.deletionReason.trim()) { this.message.warning(this.l('::ReasonRequired')); return; }
    this.taskService.delete(this.selectedTaskId!, this.deletionReason).subscribe(() => {
        this.message.success(this.l('::DeletedSuccess'));
        this.isReasonModalOpen = false;
        this.refreshData(); 
        this.isPendingModalOpen = false;    
    });
  }

  // NOTE: skipHandleError: true — tắt global error handler của ABP
  // Hàm lưu cả tạo mới và cập nhật task, với validation, xử lý lỗi trùng lặp, gọi API tương ứng, hiện toast, refresh data, đóng modal
  // để tự xử lý lỗi duplicate thay vì ABP hiện popup mặc định
  save(): void {
    this.duplicateErrorMessage = false;
    if (this.form.invalid) {
      Object.values(this.form.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty(); // Đánh dấu control là đã tương tác để hiển thị lỗi
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    // Chuẩn bị dữ liệu gửi lên API, bao gồm cả projectId và chuyển đổi dueDate về ISO string nếu có
    const requestData = { 
      ...this.form.value,
      projectId: this.projectId 
    };
    if (requestData.dueDate) {
      const d = new Date(requestData.dueDate);
      const localTime = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
      requestData.dueDate = localTime.toISOString().slice(0, 19); 
    }

    const requestOptions = { skipHandleError: true }; // Tắt global error handler để tự xử lý lỗi trùng lặp

    const request = this.isEditMode
      ? this.taskService.update(this.selectedTaskId!, requestData, requestOptions)
      : this.taskService.create(requestData, requestOptions);

    this.saving = true;
    request.subscribe({
      next: () => {
        this.isModalOpen = false;
        this.form.reset();
        this.refreshData(); 
        this.saving = false;
        this.message.success(this.l('::SaveSuccess'));
      },
      error: (err) => {
        this.saving = false;
        const errorMsg = err?.error?.error?.message || err?.error?.error?.code || '';
        
        if (errorMsg.includes('Task Already Exists') || errorMsg.includes('TaskDuplicatedMessage')) {
           this.duplicateErrorMessage = true;
        } else {
           this.message.error(errorMsg || 'Có lỗi xảy ra!');
           console.error('Lưu thất bại', err);
        }
      }
    });
  }

  // Mirror đúng logic BE DeleteAsync:
  // Boss (hasApprovePermission) → xóa được trừ task Completed
  // Member → chỉ xóa task chưa duyệt và do chính mình tạo
  canDeleteTask(task: TaskDto): boolean {
    if (this.hasApprovePermission) return task.status !== TaskStatus.Completed; 
    return !task.isApproved && task.creatorId === this.currentUser.id;
  }

  // Duyệt task: gọi API, hiện toast, refresh data, đóng modal
  approveTask(id: string): void { 
    this.taskService.approve(id).subscribe(() => { 
      this.message.success(this.l('::ApprovedSuccess')); 
      this.refreshData(); 
      this.isPendingModalOpen = false; 
      this.isModalOpen = false; 
    }); 
  }

  // Tương tự approve nhưng gọi API reject
  rejectTask(id: string): void { 
    this.taskService.reject(id).subscribe(() => { 
      this.message.success(this.l('::RejectedSuccess')); 
      this.refreshData(); 
      this.isPendingModalOpen = false; 
      this.isModalOpen = false; 
    }); 
  }

  // Refresh lại tất cả dữ liệu liên quan đến tasks và project sau khi có thay đổi (tạo/sửa/xóa/duyệt/reject)
  private refreshData(): void { 
    this.loadTasks(); 
    this.loadOverdueTasks(); 
    this.loadPendingTasks(); 
    this.loadProjectInfo(); 
  }

  // Đóng modal, reset form, và xóa lỗi trùng lặp khi hủy
  handleCancel(): void { this.isModalOpen = false; }

  // Kiểm tra quá hạn trả true nếu có dueDate và đã qua ngày hiện tại
  isOverdue(dueDate: string | null): boolean { return dueDate ? new Date(dueDate) < new Date() : false; }

  // Lấy màu theo status để hiển thị tag màu sắc tương ứng
  getStatusColor(status: TaskStatus | undefined | null): string {
    switch (status) { case TaskStatus.New: return 'blue'; case TaskStatus.InProgress: return 'orange'; case TaskStatus.Completed: return 'green'; default: return 'default'; }
  }

  // Lấy key localization theo status để hiển thị text tương ứng, nếu null/undefined trả về 'Unassigned'
  getStatusKey(status: TaskStatus | undefined | null): string {
    if (status === null || status === undefined) return 'Unassigned';
    return `Enum:TaskStatus:${(TaskStatus as any)[status as number]}`;
  }

  private l(key: string): string { return this.localizationService.instant(key); }
}
