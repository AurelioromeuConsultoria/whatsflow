import { api } from '@/lib/apiClient';

export const auditLogsApi = {
  getPaged: (params) => api.get('/auditLogs/paged', { params }),
  getMetrics: (params) => api.get('/auditLogs/metrics', { params }),
};
