import { apiClient } from './api/api-client'

export function getDepartmentsCrud() {
  return apiClient.get('/WarehouseAccess/MasterData/GetDepartments')
}
export function createDepartment(payload) {
  return apiClient.post('/WarehouseAccess/MasterData/CreateDepartment', payload)
}
export function updateDepartment(payload) {
  return apiClient.put('/WarehouseAccess/MasterData/UpdateDepartment', payload)
}
export function deleteDepartment(deptCode) {
  return apiClient.delete(`/WarehouseAccess/MasterData/DeleteDepartment?deptCode=${encodeURIComponent(deptCode)}`)
}

export function getContactDeptsCrud() {
  return apiClient.get('/WarehouseAccess/MasterData/GetContactDepts')
}
export function createContactDept(payload) {
  return apiClient.post('/WarehouseAccess/MasterData/CreateContactDept', payload)
}
export function updateContactDept(payload) {
  return apiClient.put('/WarehouseAccess/MasterData/UpdateContactDept', payload)
}
export function deleteContactDept(contactDeptId) {
  return apiClient.delete(`/WarehouseAccess/MasterData/DeleteContactDept?contactDeptId=${encodeURIComponent(contactDeptId)}`)
}

export function getPurposesCrud() {
  return apiClient.get('/WarehouseAccess/MasterData/GetPurposes')
}
export function createPurpose(payload) {
  return apiClient.post('/WarehouseAccess/MasterData/CreatePurpose', payload)
}
export function updatePurpose(payload) {
  return apiClient.put('/WarehouseAccess/MasterData/UpdatePurpose', payload)
}
export function deletePurpose(purposeId) {
  return apiClient.delete(`/WarehouseAccess/MasterData/DeletePurpose?purposeId=${encodeURIComponent(purposeId)}`)
}
