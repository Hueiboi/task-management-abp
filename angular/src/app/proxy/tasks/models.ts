import type { AuditedEntityDto, EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { TaskStatus } from './task-status.enum';
export * from './task-status.enum';

export interface TaskDto extends AuditedEntityDto<string> {
  title?: string;
  description?: string | null;
  status?: TaskStatus;
  projectId?: string;
  dueDate?: string | null;
  isApproved?: boolean;
  isRejected?: boolean;
  deletionReason?: string | null;
  weight?: number;
  assignedUserIds?: string[];
  assignedUserNames?: string[];
  assignedUserName?: string | null;
}

export interface UserLookupDto extends EntityDto<string> {
  userName?: string;
}

export interface CreateUpdateTaskDto {
  projectId?: string;
  title?: string;
  description?: string | null;
  status?: TaskStatus;
  dueDate?: string | null;
  weight?: number;
  assignedUserIds?: string[];
}

export interface GetTasksInput extends PagedAndSortedResultRequestDto {
  projectId?: string | null;
  filterText?: string | null;
  status?: TaskStatus | null;
  assignedUserId?: string | null;
  isApproved?: boolean | null;
}
