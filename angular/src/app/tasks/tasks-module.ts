import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

import { ProjectComponent } from '../projects/project'; 
import { TaskComponent } from './task'; 

const routes: Routes = [
  { 
    path: '', 
    component: ProjectComponent // Route gốc của module: /tasks
  },
  { 
    path: 'details', 
    component: TaskComponent // Route chi tiết: /tasks/details
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    ProjectComponent,
    TaskComponent,
  ],
})
export class TasksModule {}