import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';

// Ng-Zorro
import { NzCalendarModule } from 'ng-zorro-antd/calendar';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSelectModule } from 'ng-zorro-antd/select'; 
import { NzModalModule } from 'ng-zorro-antd/modal';

// Services
import { CalendarService } from '../proxy/controllers/calendar.service'; // Check lại đường dẫn service
import { TaskDto, TaskStatus } from '../proxy/tasks/models';
import { ProjectService } from '../proxy/projects/project.service';

interface CalendarEventGroup {
  isGroup: boolean;
  tasks: TaskDto[];
  topPx: number;
  heightPx: number;
}

@Component({
  selector: 'app-calendar',
  standalone: true,
  templateUrl: './calendar.html',
  styleUrls: ['../style/calendar.scss'],
  imports: [
    CommonModule, FormsModule, CoreModule, ThemeSharedModule,
    NzCalendarModule, NzBadgeModule, NzSelectModule, NzCardModule, 
    NzTagModule, NzToolTipModule, NzSpinModule, NzDividerModule,
    NzRadioModule, NzInputModule, NzIconModule, NzEmptyModule, NzModalModule
  ],
})
export class CalendarComponent implements OnInit {
  private calendarService = inject(CalendarService);
  private projectService = inject(ProjectService);

  loading = false;
  viewMode: 'month' | 'week' | 'list' = 'week'; 
  
  selectedDate = new Date(); 
  currentWeekStart = this.getStartOfWeek(new Date()); 
  
  searchTerm = '';
  allTasks: TaskDto[] = [];
  displayTasks: TaskDto[] = [];
  
  projects: any[] = [];
  selectedProjectIds: string[] = []; 
  
  PX_PER_HOUR = 45;
  // MẢNG ĐỘNG CHỨA CÁC GIỜ SẼ HIỂN THỊ (Đã loại bỏ khung giờ rác)
  activeHours: number[] = []; 

  weekDaysList: Date[] = [];
  tasksByDayMap = new Map<string, CalendarEventGroup[]>();
  tasksForMonthMap = new Map<string, TaskDto[]>();
  listGroupedTasks: [string, TaskDto[]][] = [];

  isTaskModalVisible = false;
  selectedGroupTasks: TaskDto[] = [];

  ngOnInit(): void {
    this.generateWeekDays();
    this.loadProjects();
    this.loadCalendarData();
  }

  loadProjects(): void {
    this.projectService.getList({ maxResultCount: 1000 }).subscribe(res => {
      this.projects = res.items;
      this.selectedProjectIds = this.projects.map(p => p.id); 
      this.applyFilter(); 
    });
  }

  loadCalendarData(): void {
    this.loading = true;
    const startDate = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth() - 1, 1).toISOString();
    const endDate = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth() + 2, 0).toISOString();

    this.calendarService.getCalendarTasks(startDate, endDate).subscribe(res => {
      this.allTasks = res;
      this.applyFilter(); 
      this.loading = false;
    });
  }

  applyFilter(): void {
    if (this.projects.length === 0) return;

    this.displayTasks = this.allTasks.filter(t => {
      const matchProject = this.selectedProjectIds.includes(t.projectId);
      const matchSearch = !this.searchTerm || 
                          t.title.toLowerCase().includes(this.searchTerm.toLowerCase()) || 
                          (t.assignedUserName && t.assignedUserName.toLowerCase().includes(this.searchTerm.toLowerCase()));
      return matchProject && matchSearch;
    });

    this.buildDataMaps();
  }

  buildDataMaps() {
    this.tasksByDayMap.clear();
    this.tasksForMonthMap.clear();

    const listMap = new Map<string, TaskDto[]>();
    const taskHoursSet = new Set<number>();

    this.displayTasks.forEach(task => {
      const dateKey = this.formatDateKey(new Date(task.dueDate));
      
      if (!this.tasksForMonthMap.has(dateKey)) this.tasksForMonthMap.set(dateKey, []);
      this.tasksForMonthMap.get(dateKey)!.push(task);
      
      if (!listMap.has(dateKey)) listMap.set(dateKey, []);
      listMap.get(dateKey)!.push(task);
    });

    this.listGroupedTasks = Array.from(listMap.entries()).sort((a, b) => a[0].localeCompare(b[0]));

    // BƯỚC 1: XÂY DỰNG MẢNG GIỜ ĐỘNG (Lọc các giờ có task trong tuần)
    this.weekDaysList.forEach(day => {
      const dateKey = this.formatDateKey(day);
      const tasksOnThisDay = this.tasksForMonthMap.get(dateKey) || [];
      tasksOnThisDay.forEach(t => {
        taskHoursSet.add(new Date(t.dueDate).getHours());
      });
    });

    if (taskHoursSet.size === 0) {
      // Mặc định giờ hành chính nếu tuần rỗng
      this.activeHours = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17];
    } else {
      // Thêm đệm (buffer) 1h trước và 1h sau để card không bị sát viền
      const expandedHours = new Set<number>();
      taskHoursSet.forEach(h => {
        if (h > 0) expandedHours.add(h - 1);
        expandedHours.add(h);
        if (h < 23) expandedHours.add(h + 1);
      });
      // Sắp xếp lại từ bé đến lớn
      this.activeHours = Array.from(expandedHours).sort((a, b) => a - b);
    }

    // BƯỚC 2: TÍNH TOẠ ĐỘ DỰA VÀO VỊ TRÍ TRONG MẢNG ACTIVE_HOURS
    this.weekDaysList.forEach(day => {
      const dateKey = this.formatDateKey(day);
      const tasksOnThisDay = this.tasksForMonthMap.get(dateKey) || [];

      const timeGroupMap = new Map<string, TaskDto[]>();
      tasksOnThisDay.forEach(t => {
        const d = new Date(t.dueDate);
        const timeKey = `${d.getHours()}:${d.getMinutes()}`;
        if (!timeGroupMap.has(timeKey)) timeGroupMap.set(timeKey, []);
        timeGroupMap.get(timeKey)!.push(t);
      });

      const groups: CalendarEventGroup[] = [];
      timeGroupMap.forEach(tasks => {
        const { topPx, heightPx } = this.calculatePosition(new Date(tasks[0].dueDate));
        groups.push({
          isGroup: tasks.length > 1,
          tasks: tasks,
          topPx,
          heightPx
        });
      });

      this.tasksByDayMap.set(dateKey, groups);
    });
  }

  calculatePosition(date: Date) {
    const hour = date.getHours();
    const minutes = date.getMinutes();
    
    // TÌM VỊ TRÍ INDEX ĐỂ XÁC ĐỊNH TOẠ ĐỘ Y
    const hourIndex = this.activeHours.indexOf(hour);
    const safeIndex = hourIndex !== -1 ? hourIndex : 0; // Tránh lỗi

    const topPx = (safeIndex * this.PX_PER_HOUR) + ((minutes / 60) * this.PX_PER_HOUR);
    return { topPx, heightPx: this.PX_PER_HOUR }; 
  }

  getStartOfWeek(date: Date): Date {
    const d = new Date(date);
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day;
    d.setDate(d.getDate() + diff);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  generateWeekDays() {
    this.weekDaysList = Array.from({ length: 7 }).map((_, i) => {
      const d = new Date(this.currentWeekStart);
      d.setDate(d.getDate() + i);
      return d;
    });
  }

  formatDateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  navigateWeek(direction: 'prev' | 'next' | 'today'): void {
    if (direction === 'today') {
      this.currentWeekStart = this.getStartOfWeek(new Date());
    } else {
      const d = new Date(this.currentWeekStart);
      d.setDate(d.getDate() + (direction === 'next' ? 7 : -7));
      this.currentWeekStart = d;
    }
    this.generateWeekDays();
    this.buildDataMaps();
  }

  getTasksForMonth(date: Date): TaskDto[] {
    return this.tasksForMonthMap.get(this.formatDateKey(date)) || [];
  }

  getGroupsForWeekDay(date: Date): CalendarEventGroup[] {
    return this.tasksByDayMap.get(this.formatDateKey(date)) || [];
  }

  openGroupModal(group: CalendarEventGroup) {
    this.selectedGroupTasks = group.tasks;
    this.isTaskModalVisible = true;
  }

  getStatusType(status: TaskStatus): string {
    switch (status) {
      case TaskStatus.New: return 'processing';
      case TaskStatus.InProgress: return 'warning';
      case TaskStatus.Completed: return 'success';
      default: return 'default';
    }
  }
}