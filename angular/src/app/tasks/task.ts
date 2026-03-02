import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { LocalizationService, ListService, PagedResultDto, CoreModule, PermissionService, ConfigStateService, CurrentUserDto } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';

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

import { TaskService } from '../proxy/tasks/task.service';
import { TaskDto, TaskStatus, UserLookupDto } from '../proxy/tasks/models';
import { ProjectService } from '../proxy/projects/project.service';
import { Router, ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-task',
  standalone: true,
  templateUrl: './task.html',
  styleUrls: ['../style/global.scss'],
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzDrawerModule, NzTableModule, NzTagModule, NzButtonModule, NzIconModule,
    NzModalModule, NzFormModule, NzInputModule, NzSelectModule, NzProgressModule,
    NzToolTipModule, NzAvatarModule, NzDatePickerModule, NzSpinModule, NzDividerModule,
    NzCheckboxModule, NzSliderModule
  ],
})
export class TaskComponent implements OnInit {
  public readonly list = inject(ListService);
  private taskService = inject(TaskService);
  private projectService = inject(ProjectService);
  private fb = inject(FormBuilder);
  private message = inject(NzMessageService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private permissionService = inject(PermissionService);
  private configState = inject(ConfigStateService);
  private localizationService = inject(LocalizationService);

  weightMarks: any = { 1: '1', 5: '5', 10: '10' };

  taskData: PagedResultDto<TaskDto> = { items: [], totalCount: 0 };
  allTasksForStats: TaskDto[] = [];
  overdueTasks: TaskDto[] = [];
  pendingTasks: TaskDto[] = [];
  users: UserLookupDto[] = [];
  taskStatus = TaskStatus;
  currentUser: CurrentUserDto;
  projectId: string;
  projectName: string = '';
  projectProgress: number = 0;

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

  filterText = '';
  sorting = 'CreationTime DESC';
  pageIndex = 1;
  pageSize = 10;
  
  totalCount = 0;
  inProgressCount = 0;
  completedCount = 0;
  pendingCount = 0;
  totalWeight = 0;

  showOnlyUncompletedOverdue = true;
  form!: FormGroup;

  ngOnInit(): void {
    this.projectId = this.route.snapshot.queryParams['projectId'];
    if (!this.projectId) { this.goBack(); return; }

    this.currentUser = this.configState.getOne('currentUser');
    this.hasCreatePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Create');
    this.hasApprovePermission = this.permissionService.getGrantedPolicy('TaskManagement.Tasks.Approve');

    this.buildForm();
    this.loadProjectInfo();
    this.loadUsers();
    this.loadTasks();
    this.loadOverdueTasks();
    this.loadPendingTasks();
  }

  get filteredOverdueTasks() {
    if (this.showOnlyUncompletedOverdue) {
      return this.overdueTasks.filter(t => t.status !== TaskStatus.Completed);
    }
    return this.overdueTasks;
  }

  goBack(): void { this.router.navigate(['/projects']); }

  startResize(event: MouseEvent): void {
    this.isResizing = true;
    event.preventDefault();
    document.addEventListener('mousemove', this.onMouseMove);
    document.addEventListener('mouseup', this.onMouseUp);
  }

  onMouseMove = (event: MouseEvent) => {
    if (!this.isResizing) return;
    const newWidth = window.innerWidth - event.clientX;
    if (newWidth >= 300 && newWidth < window.innerWidth * 0.95) {
      this.drawerWidth = newWidth;
    }
  };

  onMouseUp = () => {
    this.isResizing = false;
    document.removeEventListener('mousemove', this.onMouseMove);
    document.removeEventListener('mouseup', this.onMouseUp);
  };

  private loadProjectInfo(): void {
    this.projectService.get(this.projectId).subscribe(res => {
        this.projectName = res.name;
        this.projectProgress = res.progress > 0 && res.progress <= 1 
            ? Math.round(res.progress * 100) 
            : Math.round(res.progress || 0);
    });
  }

  private loadUsers(): void {
    this.projectService.getMembersLookup(this.projectId).subscribe(res => this.users = res.items);
  }

  private loadTasks(): void {
    this.taskService.getList({ projectId: this.projectId, isApproved: true, maxResultCount: 1000 })
      .subscribe(res => {
        this.allTasksForStats = res.items;
        this.totalCount = res.totalCount;
        this.calculateStats();
      });

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
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.taskData = res;
      this.loading = false;
    });
  }

  private calculateStats(): void {
    this.inProgressCount = this.allTasksForStats.filter(t => t.status === TaskStatus.InProgress).length;
    this.completedCount = this.allTasksForStats.filter(t => t.status === TaskStatus.Completed).length;
    this.totalWeight = this.allTasksForStats.reduce((sum, t) => sum + (t.weight || 1), 0);
  }

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

  onSort(sort: { key: string; value: string | null }): void {
    this.sorting = sort.value ? `${sort.key} ${sort.value === 'descend' ? 'DESC' : 'ASC'}` : 'CreationTime DESC';
    this.list.get();
  }

  loadOverdueTasks(): void { this.taskService.getOverdueList(this.projectId).subscribe(res => this.overdueTasks = res.items); }

  loadPendingTasks(): void {
    this.taskService.getList({ projectId: this.projectId, isApproved: false, maxResultCount: 100 })
      .subscribe(res => { this.pendingTasks = res.items; this.pendingCount = res.totalCount; });
  }

  onSearch(): void { this.list.page = 0; this.list.get(); }

  onPageChange(pageIndex: number): void { this.pageIndex = pageIndex; this.list.page = pageIndex - 1; }

  onPageSizeChange(pageSize: number): void { this.pageSize = pageSize; this.list.maxResultCount = pageSize; this.list.get(); }

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

  deleteTaskWithReason(): void {
    if (!this.deletionReason.trim()) { this.message.warning(this.l('::ReasonRequired')); return; }
    this.taskService.delete(this.selectedTaskId!, this.deletionReason).subscribe(() => {
        this.message.success(this.l('::DeletedSuccess'));
        this.isReasonModalOpen = false;
        this.refreshData(); 
        this.isPendingModalOpen = false;    
    });
  }

  save(): void {
    this.duplicateErrorMessage = false;
    if (this.form.invalid) {
      Object.values(this.form.controls).forEach(control => {
        if (control.invalid) {
          control.markAsDirty();
          control.updateValueAndValidity({ onlySelf: true });
        }
      });
      return;
    }

    const requestData = { 
      ...this.form.value,
      projectId: this.projectId 
    };

    if (requestData.dueDate) {
      const d = new Date(requestData.dueDate);
      const localTime = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
      requestData.dueDate = localTime.toISOString().slice(0, 19); 
    }

    const requestOptions = { skipHandleError: true };

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

  canDeleteTask(task: TaskDto): boolean {
    if (this.hasApprovePermission) return task.status !== TaskStatus.Completed; 
    return !task.isApproved && task.creatorId === this.currentUser.id;
  }

  approveTask(id: string): void { 
    this.taskService.approve(id).subscribe(() => { 
      this.message.success(this.l('::ApprovedSuccess')); 
      this.refreshData(); 
      this.isPendingModalOpen = false; 
      this.isModalOpen = false; 
    }); 
  }

  rejectTask(id: string): void { 
    this.taskService.reject(id).subscribe(() => { 
      this.message.success(this.l('::RejectedSuccess')); 
      this.refreshData(); 
      this.isPendingModalOpen = false; 
      this.isModalOpen = false; 
    }); 
  }

  private refreshData(): void { 
    this.loadTasks(); 
    this.loadOverdueTasks(); 
    this.loadPendingTasks(); 
    this.loadProjectInfo(); 
  }

  handleCancel(): void { this.isModalOpen = false; }

  isOverdue(dueDate: string | null): boolean { return dueDate ? new Date(dueDate) < new Date() : false; }

  getStatusColor(status: TaskStatus | undefined | null): string {
    switch (status) { case TaskStatus.New: return 'blue'; case TaskStatus.InProgress: return 'orange'; case TaskStatus.Completed: return 'green'; default: return 'default'; }
  }

  getStatusKey(status: TaskStatus | undefined | null): string {
    if (status === null || status === undefined) return 'Unassigned';
    return `Enum:TaskStatus:${(TaskStatus as any)[status as number]}`;
  }

  private l(key: string): string { return this.localizationService.instant(key); }
}