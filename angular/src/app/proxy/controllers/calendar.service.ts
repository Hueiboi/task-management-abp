import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { TaskDto } from '../tasks/models';

@Injectable({
  providedIn: 'root',
})
export class CalendarService {
  private restService = inject(RestService);
  apiName = 'TaskManagement';
  

  getCalendarTasks = (startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto[]>({
      method: 'GET',
      url: '/api/app/calendar/tasks',
      params: { startDate, endDate },
    },
    { apiName: this.apiName,...config });
}