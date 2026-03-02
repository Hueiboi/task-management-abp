// angular\src\app\route.provider.ts
import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/',
      name: 'TaskManagement::Menu:Home', 
      iconClass: 'fas fa-home',
      order: 1,
      layout: eLayoutType.application,
    },
    {
      path: '/projects', 
      name: 'TaskManagement::Menu:Projects', 
      iconClass: 'fas fa-project-diagram',    
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Projects',
    },
    {
      path: '/calendar',
      name: 'TaskManagement::Menu:Calendar', 
      iconClass: 'fas fa-calendar-alt', 
      order: 3, 
      layout: eLayoutType.application,
      requiredPolicy: 'TaskManagement.Calendar',
    },
  ]);
}