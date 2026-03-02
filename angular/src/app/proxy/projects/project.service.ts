import type { CreateUpdateProjectDto, GetProjectsInput, ProjectDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { UserLookupDto } from '../tasks/models';

@Injectable({
  providedIn: 'root',
})
export class ProjectService {
  private restService = inject(RestService);
  apiName = 'TaskManagement';
  

  create = (input: CreateUpdateProjectDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectDto>({
      method: 'POST',
      url: '/api/project',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/project/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectDto>({
      method: 'GET',
      url: `/api/project/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetProjectsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ProjectDto>>({
      method: 'GET',
      url: '/api/project',
      params: { filterText: input.filterText, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMembersLookup = (projectId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<UserLookupDto>>({
      method: 'GET',
      url: `/api/project/members/${projectId}`,
    },
    { apiName: this.apiName,...config });
  

  getProjectManagersLookup = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<UserLookupDto>>({
      method: 'GET',
      url: '/api/project/project-managers-lookup',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateProjectDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectDto>({
      method: 'PUT',
      url: `/api/project/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}