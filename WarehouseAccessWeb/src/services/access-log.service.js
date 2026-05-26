import { apiClient } from './api/api-client'
import { APP_CONFIG } from '../constants/app-config'

export function lookupByCard(cardNumber) {
  return apiClient.post('/WarehouseAccess/AccessLog/LookupByCard', { cardNumber })
}

export function createCheckInAccessLog(payload) {
  return apiClient.post('/WarehouseAccess/AccessLog/CreateCheckIn', payload)
}

export function getLiveMonitor(params = {}) {
  const query = new URLSearchParams()
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.take) query.set('take', String(params.take))
  const qs = query.toString()
  const path = qs
    ? `/WarehouseAccess/AccessLog/GetLiveMonitor?${qs}`
    : '/WarehouseAccess/AccessLog/GetLiveMonitor'
  return apiClient.get(path)
}

export function getAccessLogHistory(params = {}) {
  const query = new URLSearchParams()
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.fromDate) query.set('fromDate', params.fromDate)
  if (params.toDate) query.set('toDate', params.toDate)
  if (params.take) query.set('take', String(params.take))
  const qs = query.toString()
  const path = qs
    ? `/WarehouseAccess/AccessLog/GetHistory?${qs}`
    : '/WarehouseAccess/AccessLog/GetHistory'
  return apiClient.get(path)
}

export function confirmCheckOut(payload) {
  return apiClient.post('/WarehouseAccess/AccessLog/ConfirmCheckOut', payload)
}

export async function exportAccessLogExcel(params = {}) {
  try {
    const query = new URLSearchParams()
    if (params.keyword) query.set('keyword', params.keyword)
    if (params.fromDate) query.set('fromDate', params.fromDate)
    if (params.toDate) query.set('toDate', params.toDate)

    const qs = query.toString()
    const path = qs
      ? `/WarehouseAccess/AccessLog/ExportHistoryExcel?${qs}`
      : '/WarehouseAccess/AccessLog/ExportHistoryExcel'

    const response = await fetch(`${APP_CONFIG.apiBaseUrl}${path}`, { method: 'GET' })
    if (!response.ok) {
      return { success: false, message: `HTTP ${response.status}` }
    }

    const blob = await response.blob()
    const contentDisposition = response.headers.get('content-disposition') || ''
    const match = contentDisposition.match(/filename="?([^"]+)"?/)
    const fileName = match?.[1] || 'access-log.xlsx'
    return { success: true, data: { blob, fileName } }
  } catch (error) {
    return { success: false, message: error?.message || 'Export access log failed' }
  }
}
