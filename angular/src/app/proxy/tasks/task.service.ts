import type { CreateUpdateTaskDto, GetTasksInput, TaskDto, UserLookupDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private restService = inject(RestService);
  apiName = 'TaskManagement';
  

  approve = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'POST',
      url: `/api/task/${id}/approve`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateTaskDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'POST',
      url: '/api/task',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/task/${id}`,
      params: { reason },
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'GET',
      url: `/api/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetTasksInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TaskDto>>({
      method: 'GET',
      url: '/api/task',
      params: { projectId: input.projectId, filterText: input.filterText, status: input.status, assignedUserId: input.assignedUserId, isApproved: input.isApproved, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getOverdueList = (projectId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TaskDto>>({
      method: 'GET',
      url: `/api/task/overdue/${projectId}`,
    },
    { apiName: this.apiName,...config });
  

  getUserLookup = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<UserLookupDto>>({
      method: 'GET',
      url: '/api/task/user-lookup',
    },
    { apiName: this.apiName,...config });
  

  reject = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/task/${id}/reject`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTaskDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'PUT',
      url: `/api/task/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}