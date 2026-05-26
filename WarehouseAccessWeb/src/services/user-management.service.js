import { apiClient } from './api/api-client'
import { APP_CONFIG } from '../constants/app-config'

export function getDepartments() {
  return apiClient.get('/WarehouseAccess/UserManagement/GetDepartments')
}

export function getUsers(params = {}) {
  const query = new URLSearchParams()
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.deptCode) query.set('deptCode', params.deptCode)
  if (params.page) query.set('page', String(params.page))
  if (params.pageSize) query.set('pageSize', String(params.pageSize))
  const qs = query.toString()
  const path = qs
    ? `/WarehouseAccess/UserManagement/GetUsers?${qs}`
    : '/WarehouseAccess/UserManagement/GetUsers'
  return apiClient.get(path)
}

export function createUser(payload) {
  return apiClient.post('/WarehouseAccess/UserManagement/CreateUser', payload)
}

export function updateUser(payload) {
  return apiClient.put('/WarehouseAccess/UserManagement/UpdateUser', payload)
}

export function deleteUser(userCode) {
  return apiClient.delete(`/WarehouseAccess/UserManagement/DeleteUser?userCode=${encodeURIComponent(userCode)}`)
}

export function importUsersExcel(file) {
  const formData = new FormData()
  formData.append('file', file)
  return apiClient.post('/WarehouseAccess/UserManagement/ImportUsers', formData)
}

export async function exportUsersTemplateExcel() {
  try {
    const response = await fetch(`${APP_CONFIG.apiBaseUrl}/WarehouseAccess/UserManagement/ExportUsersTemplate`, {
      method: 'GET'
    })
    if (!response.ok) {
      return { success: false, message: `HTTP ${response.status}` }
    }

    const blob = await response.blob()
    const contentDisposition = response.headers.get('content-disposition') || ''
    const match = contentDisposition.match(/filename="?([^"]+)"?/)
    const fileName = match?.[1] || 'users-template.xlsx'
    return { success: true, data: { blob, fileName } }
  } catch (error) {
    return { success: false, message: error?.message || 'Export template failed' }
  }
}
