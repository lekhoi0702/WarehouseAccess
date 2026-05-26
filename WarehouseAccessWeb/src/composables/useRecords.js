import { ref, computed } from 'vue'
import {
  confirmCheckOut,
  createCheckInAccessLog,
  exportAccessLogExcel,
  getAccessLogHistory,
  getLiveMonitor,
  lookupByCard
} from '../services/access-log.service'
import {
  createUser,
  deleteUser,
  exportUsersTemplateExcel,
  getDepartments,
  getUsers,
  importUsersExcel,
  updateUser
} from '../services/user-management.service'
import {
  createContactDept,
  createDepartment,
  createPurpose,
  deleteContactDept,
  deleteDepartment,
  deletePurpose,
  getContactDeptsCrud,
  getDepartmentsCrud,
  getPurposesCrud,
  updateContactDept,
  updateDepartment,
  updatePurpose
} from '../services/master-data.service'

const monitorItems = ref([])
const monitorLoading = ref(false)
const monitorErrorMessage = ref('')

const historyItems = ref([])
const historyLoading = ref(false)
const historyErrorMessage = ref('')

const userListItems = ref([])
const userDepartmentOptions = ref([])
const usersLoading = ref(false)
const usersErrorMessage = ref('')
const usersTotal = ref(0)
const usersPage = ref(1)
const usersPageSize = ref(10)

const departmentItems = ref([])
const contactDeptItems = ref([])
const purposeItems = ref([])
const masterDataErrorMessage = ref('')

export function useRecords() {
  
  async function loadLiveMonitor(keyword = '') {
    monitorLoading.value = true
    monitorErrorMessage.value = ''
    try {
      const response = await getLiveMonitor({
        keyword: keyword || undefined,
        take: 200
      })
      if (response?.success) {
        monitorItems.value = response.data || []
      } else {
        monitorErrorMessage.value = response?.message || 'Cannot load live monitor records.'
        monitorItems.value = []
      }
    } catch (e) {
      monitorErrorMessage.value = e.message || 'Error fetching live monitor'
    } finally {
      monitorLoading.value = false
    }
  }

  async function loadHistoryRecords(keyword = '', fromDate = '', toDate = '') {
    historyLoading.value = true
    historyErrorMessage.value = ''
    try {
      const response = await getAccessLogHistory({
        keyword: keyword || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        take: 500
      })
      if (response?.success) {
        historyItems.value = response.data || []
      } else {
        historyErrorMessage.value = response?.message || 'Cannot load history records.'
        historyItems.value = []
      }
    } catch (e) {
      historyErrorMessage.value = e.message || 'Error fetching history'
    } finally {
      historyLoading.value = false
    }
  }

  async function lookupCard(cardNumber) {
    return lookupByCard(cardNumber)
  }

  async function submitCheckIn(payload) {
    const res = await createCheckInAccessLog(payload)
    if (res?.success) {
      await loadLiveMonitor()
      await loadHistoryRecords()
    }
    return res
  }

  async function submitCheckOut(logId) {
    const res = await confirmCheckOut({ logId })
    if (res?.success) {
      await loadLiveMonitor()
      await loadHistoryRecords()
    }
    return res
  }

  async function exportHistory(keyword = '', fromDate = '', toDate = '') {
    return exportAccessLogExcel({ keyword, fromDate, toDate })
  }

  // --- Users CRUD ---
  async function loadUserDepartments() {
    const response = await getDepartments()
    userDepartmentOptions.value = response?.success ? response.data || [] : []
  }

  async function loadUsersList(keyword = '', deptCode = '') {
    usersLoading.value = true
    usersErrorMessage.value = ''
    try {
      const response = await getUsers({
        keyword: keyword || undefined,
        deptCode: deptCode || undefined,
        page: usersPage.value,
        pageSize: usersPageSize.value
      })
      if (response?.success) {
        userListItems.value = response.data?.items || []
        usersTotal.value = response.data?.total || 0
        usersPage.value = response.data?.page || 1
      } else {
        usersErrorMessage.value = response?.message || 'Cannot load users.'
        userListItems.value = []
        usersTotal.value = 0
      }
    } catch (e) {
      usersErrorMessage.value = e.message || 'Error loading users'
    } finally {
      usersLoading.value = false
    }
  }

  async function saveUser(payload, isEdit = false) {
    const res = isEdit ? await updateUser(payload) : await createUser(payload)
    if (res?.success) {
      await loadUsersList()
    }
    return res
  }

  async function removeUser(userCode) {
    const res = await deleteUser(userCode)
    if (res?.success) {
      await loadUsersList()
    }
    return res
  }

  async function importUsers(file) {
    const res = await importUsersExcel(file)
    if (res?.success) {
      await loadUsersList()
    }
    return res
  }

  async function downloadUsersTemplate() {
    const response = await exportUsersTemplateExcel()
    if (response?.success && response?.data?.blob) {
      const downloadUrl = URL.createObjectURL(response.data.blob)
      const anchorElement = document.createElement('a')
      anchorElement.href = downloadUrl
      anchorElement.download = response.data.fileName || 'users-template.xlsx'
      document.body.appendChild(anchorElement)
      anchorElement.click()
      anchorElement.remove()
      URL.revokeObjectURL(downloadUrl)
      return { success: true }
    }
    return response || { success: false, message: 'Export template failed' }
  }

  // --- Departments CRUD ---
  async function loadDepartmentsCrud() {
    masterDataErrorMessage.value = ''
    const response = await getDepartmentsCrud()
    if (response?.success) {
      departmentItems.value = response.data || []
    } else {
      masterDataErrorMessage.value = response?.message || 'Cannot load departments.'
      departmentItems.value = []
    }
  }

  async function saveDepartment(payload, isEdit = false) {
    const res = isEdit ? await updateDepartment(payload) : await createDepartment(payload)
    if (res?.success) {
      await loadDepartmentsCrud()
      await loadUserDepartments()
    }
    return res
  }

  async function removeDepartment(deptCode) {
    const res = await deleteDepartment(deptCode)
    if (res?.success) {
      await loadDepartmentsCrud()
      await loadUserDepartments()
    }
    return res
  }

  // --- Contact Depts CRUD ---
  async function loadContactDeptsCrud() {
    masterDataErrorMessage.value = ''
    const response = await getContactDeptsCrud()
    if (response?.success) {
      contactDeptItems.value = response.data || []
    } else {
      masterDataErrorMessage.value = response?.message || 'Cannot load contact departments.'
      contactDeptItems.value = []
    }
  }

  async function saveContactDept(payload, isEdit = false) {
    const res = isEdit ? await updateContactDept(payload) : await createContactDept(payload)
    if (res?.success) {
      await loadContactDeptsCrud()
    }
    return res
  }

  async function removeContactDept(contactDeptId) {
    const res = await deleteContactDept(contactDeptId)
    if (res?.success) {
      await loadContactDeptsCrud()
    }
    return res
  }

  // --- Purposes CRUD ---
  async function loadPurposesCrud() {
    masterDataErrorMessage.value = ''
    const response = await getPurposesCrud()
    if (response?.success) {
      purposeItems.value = response.data || []
    } else {
      masterDataErrorMessage.value = response?.message || 'Cannot load purposes.'
      purposeItems.value = []
    }
  }

  async function savePurpose(payload, isEdit = false) {
    const res = isEdit ? await updatePurpose(payload) : await createPurpose(payload)
    if (res?.success) {
      await loadPurposesCrud()
    }
    return res
  }

  async function removePurpose(purposeId) {
    const res = await deletePurpose(purposeId)
    if (res?.success) {
      await loadPurposesCrud()
    }
    return res
  }

  // --- Statistics computed properties ---
  const stats = computed(() => {
    const inside = monitorItems.value.length
    const todayStart = new Date().setHours(0, 0, 0, 0)
    // Estimate today's logs based on checkInTime matching today
    const today = historyItems.value.filter(r => {
      const time = new Date(r.checkInTime).getTime()
      return time >= todayStart || !r.checkOutTime
    }).length

    return {
      onSite: inside,
      today: today
    }
  })

  return {
    monitorItems,
    monitorLoading,
    monitorErrorMessage,
    historyItems,
    historyLoading,
    historyErrorMessage,
    userListItems,
    userDepartmentOptions,
    usersLoading,
    usersErrorMessage,
    usersTotal,
    usersPage,
    usersPageSize,
    departmentItems,
    contactDeptItems,
    purposeItems,
    masterDataErrorMessage,
    
    loadLiveMonitor,
    loadHistoryRecords,
    lookupCard,
    submitCheckIn,
    submitCheckOut,
    exportHistory,
    loadUserDepartments,
    loadUsersList,
    saveUser,
    removeUser,
    importUsers,
    downloadUsersTemplate,
    loadDepartmentsCrud,
    saveDepartment,
    removeDepartment,
    loadContactDeptsCrud,
    saveContactDept,
    removeContactDept,
    loadPurposesCrud,
    savePurpose,
    removePurpose,
    stats
  }
}
