// Angular
import { Component, OnInit, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService, PagedResultDto, CoreModule, PermissionService, LocalizationService, ConfigStateService, CurrentUserDto } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
// UI Modules
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzSpinModule } from 'ng-zorro-antd/spin';
// Services
import { ProjectService } from '../proxy/projects/project.service';
import { ProjectDto } from '../proxy/projects/models';
import { TaskService } from '../proxy/tasks/task.service';

// Decorator
@Component({
  selector: 'app-project',
  standalone: true,
  templateUrl: './project.html',
  styleUrls: ['../style/global.scss'],
  providers: [ListService],
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, CoreModule, ThemeSharedModule,
    NzCardModule, NzProgressModule, NzButtonModule, NzIconModule, NzAvatarModule,
    NzInputModule, NzDrawerModule, NzFormModule, NzSelectModule, NzToolTipModule, NzSpinModule
  ],
})

  // Dependency Injection and Class Definition
export class ProjectComponent implements OnInit, OnDestroy {
  public readonly list = inject(ListService);
  private projectService = inject(ProjectService);
  private taskService = inject(TaskService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private message = inject(NzMessageService);
  public permission = inject(PermissionService); 
  private localizationService = inject(LocalizationService);
  private configState = inject(ConfigStateService);

  // State Variables
  projectData: PagedResultDto<ProjectDto> = { items: [], totalCount: 0 };
  users: any[] = []; 
  projectManagers: any[] = []; 
  loading = false;
  isModalOpen = false;
  isEditMode = false;
  saving = false;
  form!: FormGroup;
  filterText = '';
  sorting = 'CreationTime DESC';
  isCreationSortDesc = true;
  currentUser: CurrentUserDto;

  private pmSubscription?: Subscription; // Hàm đăng ký sự kiện thay đổi Project Manager

  // ngOnInit — component được tạo → bắt đầu lắng nghe
  ngOnInit(): void {
    this.currentUser = this.configState.getOne('currentUser');
    this.buildForm();
    this.loadUsers();
    this.loadProjectManagers(); 
    this.loadProjects();
    // tạo subscription, lưu vào this.pmSubscription
    this.subscribeToPmChanges();
  }

  // ngOnDestroy — component bị destroy → dọn dẹp
  // Lifecycle hook — chạy khi component bị destroy
  // Unsubscribe để tránh memory leak
  ngOnDestroy(): void {
    // hủy subscription đã tạo ở trên
    this.pmSubscription?.unsubscribe();
  }

  // Kiểm tra quyền chỉnh sửa dự án
  canEditProject(project: ProjectDto): boolean {
      const hasGlobalEditPermission = this.permission.getGrantedPolicy('TaskManagement.Projects.Update');
      const isProjectManager = project.projectManagerId === this.currentUser.id;
      return hasGlobalEditPermission || isProjectManager;
    }

  // Lắng nghe mỗi khi user chọn PM mới trong form
  // Business rule: PM phải luôn là member của project
  // → nếu chọn PM chưa có trong memberIds → tự động thêm vào luôn
  // Tránh trường hợp PM không thấy project của chính mình
  private subscribeToPmChanges(): void {
    this.pmSubscription = this.form.get('projectManagerId')?.valueChanges.subscribe(pmId => {
      if (pmId) {
        const currentMembers = this.form.get('memberIds')?.value || []; 
        if (!currentMembers.includes(pmId)) {
          this.form.get('memberIds')?.setValue([...currentMembers, pmId]); // Spread operator [...] để không mutate array cũ
        }
      }
    });
  }

  // Load dữ liệu, thông tin dự án
  loadProjects(): void {
    const streamCreator = (query: any) => {
      this.loading = true;
      return this.projectService.getList({ ...query, filterText: this.filterText, sorting: this.sorting });
    };
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.projectData = res;
      this.loading = false;
    });
  }

  // Load thông tin các PM
  loadProjectManagers(): void {
    this.projectService.getProjectManagersLookup().subscribe(res => this.projectManagers = res.items);
  }

  // Load thông tin user 
  loadUsers(): void {
    this.taskService.getUserLookup().subscribe(res => this.users = res.items);
  }

  // Toggle bật tắt sort theo thời gian
  toggleCreationSort(): void {
    this.isCreationSortDesc = !this.isCreationSortDesc;
    this.sorting = `CreationTime ${this.isCreationSortDesc ? 'DESC' : 'ASC'}`;
    this.list.get();
  }

  // Dựng form nhập thông tin của dự án
  buildForm(): void {
    this.form = this.fb.group({
      id: [null],
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: [''],
      projectManagerId: [null, Validators.required],
      memberIds: [[]] 
    });
  }

  // Điều hướng tới task bên trong project
  openTasks(projectId: string): void {
    this.router.navigate(['/projects/details'], { queryParams: { projectId } });
  }

  // Mở form tạo dự án
  createProject(): void {
    this.isEditMode = false;
    this.form.reset({ memberIds: [] });
    this.isModalOpen = true;
  }

  // Chỉnh sửa dự án (kiểm tra quyền -> mở form -> chỉnh sửa trên data cũ)
  editProject(event: Event, project: ProjectDto): void {
      event.stopPropagation();
      
      if (!this.canEditProject(project)) {
        this.message.error(this.l('::NoPermissionToEditProject'));
        return;
      }
  
      this.isEditMode = true;
      this.projectService.get(project.id).subscribe(res => {
        this.form.patchValue({ ...res, memberIds: res.memberIds || [] });
        this.isModalOpen = true;
      });
    }

  // Lưu project — tạo mới hoặc cập nhật tùy isEditMode
  // Không có skipHandleError vì project không cần xử lý lỗi đặc biệt
  // → để ABP global handler tự hiện popup nếu có lỗi
  save(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const formData = this.form.getRawValue();

    const request = this.isEditMode 
      ? this.projectService.update(formData.id, formData)
      : this.projectService.create(formData);

    request.subscribe(() => {
      this.message.success(this.l('::SaveSuccess'));
      this.isModalOpen = false;
      this.saving = false;
      this.list.get();
    });
  }

  // Xoá dự án
  deleteProject(event: Event, id: string): void {
    event.stopPropagation();
    this.projectService.delete(id).subscribe(() => {
      this.message.success(this.l('::DeletedSuccess'));
      this.list.get();
    });
  }

  // Huỷ các thao tác -> đóng form
  handleCancel(): void { this.isModalOpen = false; }

  private l(key: string): string { return this.localizationService.instant(key); }
}
