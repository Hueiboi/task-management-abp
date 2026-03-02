import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44319/',
  redirectUri: baseUrl,
  clientId: 'TaskManagement_App',
  responseType: 'code',
  scope: 'offline_access TaskManagement',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'TaskManagement',
  },
  localization: {
    defaultResourceName: 'TaskManagement', 
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44319',
      rootNamespace: 'TaskManagement',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
