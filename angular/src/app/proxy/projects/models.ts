import type { AuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateProjectDto {
  name?: string;
  description?: string | null;
  projectManagerId?: string;
  memberIds?: string[];
}

export interface GetProjectsInput extends PagedAndSortedResultRequestDto {
  filterText?: string | null;
}

export interface ProjectDto extends AuditedEntityDto<string> {
  name?: string;
  description?: string | null;
  projectManagerId?: string;
  projectManagerName?: string | null;
  taskCount?: number;
  completedTaskCount?: number;
  progress?: number;
  memberCount?: number;
  memberIds?: string[];
}
