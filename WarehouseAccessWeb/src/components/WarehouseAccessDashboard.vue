<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
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

const props = defineProps({
  initialTab: { type: String, default: 'settings' },
  initialSettingsTab: { type: String, default: 'users' }
})

const activeTab = ref(props.initialTab)
const settingsActiveTab = ref(props.initialSettingsTab)
const SIDEBAR_COLLAPSE_STORAGE_KEY = 'wa_sidebar_collapsed'
const MOBILE_BREAKPOINT_PX = 1024
const sidebarCollapsed = ref(localStorage.getItem(SIDEBAR_COLLAPSE_STORAGE_KEY) === '1')
const isMobileView = ref(false)
const mobileSidebarOpen = ref(false)
const sidebarSections = [
  {
    code: 'operations',
    label: 'Operations',
    items: [
      { code: 'monitor', label: 'Live Monitor', iconText: 'LM' },
      { code: 'history', label: 'History', iconText: 'HS' }
    ]
  },
  {
    code: 'master-data',
    label: 'Master Data',
    items: [
      { code: 'settings:users', label: 'Users', iconText: 'US' },
      { code: 'settings:departments', label: 'Department', iconText: 'DP' },
      { code: 'settings:contactDepts', label: 'Contact Depts', iconText: 'CD' },
      { code: 'settings:purposes', label: 'Purpose', iconText: 'PR' }
    ]
  },
  {
    code: 'system',
    label: 'System',
    items: [{ code: 'settings', label: 'Settings Home', iconText: 'ST' }]
  }
]
const selectedLanguageCode = ref(localStorage.getItem('wa_language') || 'en')

const headerTranslations = {
  en: {
    title: 'WAREHOUSE ACCESS',
    subtitle: 'Finished Goods Warehouse Access System',
    settings: 'Settings',
    checkIn: '+ CheckIn'
  },
  vi: {
    title: 'WAREHOUSE ACCESS',
    subtitle: 'He thong Quan Ly Ra Vao Kho Thanh Pham',
    settings: 'Cai Dat',
    checkIn: '+ CheckIn'
  },
  zh: {
    title: 'WAREHOUSE ACCESS',
    subtitle: 'Finished Goods Warehouse Access System',
    settings: 'Settings',
    checkIn: '+ CheckIn'
  }
}
const supportedLanguageCodes = ['zh', 'en', 'vi']
const headerText = computed(() => headerTranslations[selectedLanguageCode.value] || headerTranslations.en)

function selectLanguage(languageCode) {
  selectedLanguageCode.value = supportedLanguageCodes.includes(languageCode) ? languageCode : 'en'
  localStorage.setItem('wa_language', selectedLanguageCode.value)
}

function openSettingsPanel() {
  activeTab.value = 'settings'
  settingsActiveTab.value = props.initialSettingsTab || 'users'
}

function updateViewportState() {
  isMobileView.value = window.innerWidth <= MOBILE_BREAKPOINT_PX
  if (isMobileView.value) {
    mobileSidebarOpen.value = false
  }
}

function toggleSidebarCollapsed() {
  sidebarCollapsed.value = !sidebarCollapsed.value
  localStorage.setItem(SIDEBAR_COLLAPSE_STORAGE_KEY, sidebarCollapsed.value ? '1' : '0')
}

function toggleMobileSidebar() {
  mobileSidebarOpen.value = !mobileSidebarOpen.value
}

function isSidebarPageActive(pageCode) {
  if (pageCode.startsWith('settings:')) {
    const settingCode = pageCode.split(':')[1]
    return activeTab.value === 'settings' && settingsActiveTab.value === settingCode
  }
  return activeTab.value === pageCode
}

function onSidebarPageClick(pageCode) {
  if (isMobileView.value) {
    mobileSidebarOpen.value = false
  }
  if (pageCode.startsWith('settings:')) {
    activeTab.value = 'settings'
    settingsActiveTab.value = pageCode.split(':')[1]
    return
  }
  activeTab.value = pageCode
}

function formatDateTime(value) {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return date.toLocaleString()
}

// CheckIn flow (new API)
const checkInModalOpen = ref(false)
const checkInStep = ref(1)
const checkInCardNumber = ref('')
const checkInCardInputElement = ref(null)
const hasCardLookupResult = ref(false)
const lookupByCardLoading = ref(false)
const checkInLookupMessage = ref('')
const submitCheckInLoading = ref(false)
const checkInFormState = reactive({
  cardNumber: '',
  userCode: '',
  fullName: '',
  deptCode: '',
  deptName: '',
  contactDept: '',
  purpose: '',
  photo: ''
})
const checkInFormErrors = reactive({ userCode: '', fullName: '' })
const checkInCameraOpen = ref(false)
const checkInVideoElement = ref(null)
const checkInCameraStream = ref(null)
const pageError = ref('')

// Live monitor + history
const monitorLoading = ref(false)
const monitorItems = ref([])
const monitorKeyword = ref('')
const monitorErrorMessage = ref('')
const selectedMonitorRecord = ref(null)
const checkOutSubmitting = ref(false)

const historyLoading = ref(false)
const historyItems = ref([])
const historyKeyword = ref('')
const historyFromDate = ref('')
const historyToDate = ref('')
const historyErrorMessage = ref('')
const selectedHistoryPhotoBase64 = ref('')

const checkInScannerState = computed(() => {
  if (lookupByCardLoading.value) return 'detecting'
  if (checkInLookupMessage.value && checkInLookupMessage.value.toLowerCase().includes('found')) return 'found'
  return 'waiting'
})

function resetCheckInForm() {
  checkInStep.value = 1
  checkInCardNumber.value = ''
  hasCardLookupResult.value = false
  checkInLookupMessage.value = ''
  checkInFormState.userCode = ''
  checkInFormState.cardNumber = ''
  checkInFormState.fullName = ''
  checkInFormState.deptCode = ''
  checkInFormState.deptName = ''
  checkInFormState.contactDept = ''
  checkInFormState.purpose = ''
  checkInFormState.photo = ''
  checkInFormErrors.userCode = ''
  checkInFormErrors.fullName = ''
}

function openCheckInModal() {
  resetCheckInForm()
  checkInModalOpen.value = true
  nextTick(() => checkInCardInputElement.value?.focus())
}

function closeCheckInModal() {
  if (checkInCameraStream.value) {
    checkInCameraStream.value.getTracks().forEach((track) => track.stop())
    checkInCameraStream.value = null
  }
  checkInCameraOpen.value = false
  checkInModalOpen.value = false
}

watch(checkInStep, (stepValue) => {
  if (stepValue === 1 && checkInModalOpen.value) {
    nextTick(() => checkInCardInputElement.value?.focus())
  }
})

async function lookupCardAndPrefill() {
  const normalizedCardNumber = checkInCardNumber.value.trim()
  if (!normalizedCardNumber) {
    hasCardLookupResult.value = false
    checkInLookupMessage.value = 'Card number is required.'
    return
  }

  lookupByCardLoading.value = true
  checkInLookupMessage.value = ''
  const response = await lookupByCard(normalizedCardNumber)
  lookupByCardLoading.value = false

  if (!response?.success || !response.data) {
    hasCardLookupResult.value = false
    checkInLookupMessage.value = response?.message || 'Card not found.'
    return
  }

  hasCardLookupResult.value = true
  checkInFormState.userCode = response.data.userCode || normalizedCardNumber
  checkInFormState.cardNumber = response.data.cardNumber || normalizedCardNumber
  checkInFormState.fullName = response.data.fullName || ''
  checkInFormState.deptCode = response.data.deptCode || ''
  checkInFormState.deptName = response.data.deptName || ''
  checkInLookupMessage.value = 'Card found. Employee information loaded.'
}

function validateCheckInStepOne() {
  if (!hasCardLookupResult.value) {
    checkInLookupMessage.value = 'Please scan/check card first.'
    return false
  }

  checkInFormErrors.userCode = checkInFormState.userCode.trim() ? '' : 'Required'
  checkInFormErrors.fullName = checkInFormState.fullName.trim() ? '' : 'Required'
  return !checkInFormErrors.userCode && !checkInFormErrors.fullName
}

function goToCheckInPhotoStep() {
  if (!validateCheckInStepOne()) return
  checkInStep.value = 2
}

async function openCheckInCamera() {
  checkInCameraOpen.value = true
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' } })
    checkInCameraStream.value = stream
    setTimeout(() => {
      if (checkInVideoElement.value) checkInVideoElement.value.srcObject = stream
    }, 50)
  } catch {
    checkInCameraOpen.value = false
  }
}

function closeCheckInCamera() {
  if (checkInCameraStream.value) {
    checkInCameraStream.value.getTracks().forEach((track) => track.stop())
    checkInCameraStream.value = null
  }
  checkInCameraOpen.value = false
}

function captureCheckInPhoto() {
  if (!checkInVideoElement.value) return
  const canvasElement = document.createElement('canvas')
  canvasElement.width = checkInVideoElement.value.videoWidth || 640
  canvasElement.height = checkInVideoElement.value.videoHeight || 480
  const context = canvasElement.getContext('2d')
  context.drawImage(checkInVideoElement.value, 0, 0, canvasElement.width, canvasElement.height)
  const dataUrl = canvasElement.toDataURL('image/jpeg', 0.82)
  const markerIndex = dataUrl.indexOf('base64,')
  checkInFormState.photo = markerIndex >= 0 ? dataUrl.substring(markerIndex + 7) : dataUrl
  closeCheckInCamera()
}

async function submitCheckIn() {
  if (!validateCheckInStepOne()) {
    checkInStep.value = 1
    return
  }

  submitCheckInLoading.value = true
  const payload = {
    cardNumber: checkInFormState.cardNumber ? checkInFormState.cardNumber.trim() : null,
    userCode: checkInFormState.userCode.trim(),
    fullName: checkInFormState.fullName.trim(),
    deptCode: checkInFormState.deptCode ? checkInFormState.deptCode.trim() : null,
    contactDept: checkInFormState.contactDept ? checkInFormState.contactDept.trim() : null,
    purpose: checkInFormState.purpose ? checkInFormState.purpose.trim() : null,
    photo: checkInFormState.photo || null
  }

  const response = await createCheckInAccessLog(payload)
  submitCheckInLoading.value = false

  if (!response?.success) {
    pageError.value = response?.message || 'CheckIn failed.'
    return
  }

  closeCheckInModal()
  resetCheckInForm()
  await loadLiveMonitor()
  await loadHistoryRecords()
}

// Settings > Users (new API)
const settingsTabs = [
  { code: 'users', label: 'Users' },
  { code: 'departments', label: 'Department' },
  { code: 'contactDepts', label: 'Contact Depts' },
  { code: 'purposes', label: 'Purpose' },
  { code: 'advanced', label: 'Advanced' }
]
const userDepartmentOptions = ref([])
const userListItems = ref([])
const usersLoading = ref(false)
const usersErrorMessage = ref('')
const usersKeyword = ref('')
const usersDeptFilter = ref('')
const usersPage = ref(1)
const usersPageSize = ref(10)
const usersTotal = ref(0)
const userImportResult = ref(null)
const importingUsers = ref(false)
const userFormMode = ref('create')
const userFormState = reactive({ userCode: '', cardNumber: '', fullName: '', deptCode: '' })
const userFormErrors = reactive({ userCode: '', fullName: '', deptCode: '' })

const masterDataErrorMessage = ref('')
const departmentItems = ref([])
const departmentFormMode = ref('create')
const departmentFormState = reactive({ deptCode: '', deptName: '' })

const contactDeptItems = ref([])
const contactDeptFormMode = ref('create')
const contactDeptFormState = reactive({ contactDeptId: null, contactDeptName: '' })

const purposeItems = ref([])
const purposeFormMode = ref('create')
const purposeFormState = reactive({ purposeId: null, purposeName: '' })

function resetUserForm() {
  userFormMode.value = 'create'
  userFormState.userCode = ''
  userFormState.cardNumber = ''
  userFormState.fullName = ''
  userFormState.deptCode = ''
  userFormErrors.userCode = ''
  userFormErrors.fullName = ''
  userFormErrors.deptCode = ''
}

function validateUserForm() {
  userFormErrors.userCode = userFormState.userCode.trim() ? '' : 'Required'
  userFormErrors.fullName = userFormState.fullName.trim() ? '' : 'Required'
  userFormErrors.deptCode = userFormState.deptCode ? '' : 'Required'
  return !userFormErrors.userCode && !userFormErrors.fullName && !userFormErrors.deptCode
}

async function loadUserDepartments() {
  const response = await getDepartments()
  userDepartmentOptions.value = response?.success ? response.data || [] : []
}

async function loadUsersList() {
  usersLoading.value = true
  usersErrorMessage.value = ''
  const response = await getUsers({
    keyword: usersKeyword.value.trim() || undefined,
    deptCode: usersDeptFilter.value || undefined,
    page: usersPage.value,
    pageSize: usersPageSize.value
  })
  usersLoading.value = false

  if (!response?.success) {
    usersErrorMessage.value = response?.message || 'Cannot load users.'
    userListItems.value = []
    usersTotal.value = 0
    return
  }

  userListItems.value = response.data?.items || []
  usersTotal.value = response.data?.total || 0
  usersPage.value = response.data?.page || 1
}

async function submitUserForm() {
  if (!validateUserForm()) return

  usersErrorMessage.value = ''
  const payload = {
    userCode: userFormState.userCode.trim(),
    cardNumber: userFormState.cardNumber ? userFormState.cardNumber.trim() : null,
    fullName: userFormState.fullName.trim(),
    deptCode: userFormState.deptCode
  }

  const response = userFormMode.value === 'create'
    ? await createUser(payload)
    : await updateUser(payload)

  if (!response?.success) {
    usersErrorMessage.value = response?.message || 'Save user failed.'
    return
  }

  resetUserForm()
  await loadUsersList()
}

function editUserItem(userItem) {
  userFormMode.value = 'edit'
  userFormState.userCode = userItem.userCode || ''
  userFormState.cardNumber = userItem.cardNumber || ''
  userFormState.fullName = userItem.fullName || ''
  userFormState.deptCode = userItem.deptCode || ''
}

async function removeUser(userItem) {
  if (!userItem?.userCode) return
  const response = await deleteUser(userItem.userCode)
  if (!response?.success) {
    usersErrorMessage.value = response?.message || 'Delete user failed.'
    return
  }
  await loadUsersList()
}

async function onImportUsersFileChange(event) {
  const file = event?.target?.files?.[0]
  if (!file) return

  importingUsers.value = true
  userImportResult.value = null
  usersErrorMessage.value = ''
  const response = await importUsersExcel(file)
  importingUsers.value = false

  if (!response?.success) {
    usersErrorMessage.value = response?.message || 'Import failed.'
    return
  }

  userImportResult.value = response.data
  await loadUsersList()
  event.target.value = ''
}

async function downloadUsersTemplate() {
  usersErrorMessage.value = ''
  const response = await exportUsersTemplateExcel()
  if (!response?.success || !response?.data?.blob) {
    usersErrorMessage.value = response?.message || 'Export template failed.'
    return
  }

  const downloadUrl = URL.createObjectURL(response.data.blob)
  const anchorElement = document.createElement('a')
  anchorElement.href = downloadUrl
  anchorElement.download = response.data.fileName || 'users-template.xlsx'
  document.body.appendChild(anchorElement)
  anchorElement.click()
  anchorElement.remove()
  URL.revokeObjectURL(downloadUrl)
}

async function applyUsersFilter() {
  usersPage.value = 1
  await loadUsersList()
}

function resetDepartmentForm() {
  departmentFormMode.value = 'create'
  departmentFormState.deptCode = ''
  departmentFormState.deptName = ''
}

async function loadDepartmentsCrud() {
  const response = await getDepartmentsCrud()
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Cannot load departments.'
    departmentItems.value = []
    return
  }
  departmentItems.value = response.data || []
}

async function submitDepartmentForm() {
  masterDataErrorMessage.value = ''
  if (!departmentFormState.deptCode.trim() || !departmentFormState.deptName.trim()) {
    masterDataErrorMessage.value = 'DeptCode and DeptName are required.'
    return
  }
  const payload = {
    deptCode: departmentFormState.deptCode.trim(),
    deptName: departmentFormState.deptName.trim()
  }
  const response = departmentFormMode.value === 'create'
    ? await createDepartment(payload)
    : await updateDepartment(payload)
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Save department failed.'
    return
  }
  resetDepartmentForm()
  await loadDepartmentsCrud()
}

function editDepartmentItem(item) {
  departmentFormMode.value = 'edit'
  departmentFormState.deptCode = item.deptCode || ''
  departmentFormState.deptName = item.deptName || ''
}

async function removeDepartmentItem(item) {
  const response = await deleteDepartment(item.deptCode)
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Delete department failed.'
    return
  }
  await loadDepartmentsCrud()
}

function resetContactDeptForm() {
  contactDeptFormMode.value = 'create'
  contactDeptFormState.contactDeptId = null
  contactDeptFormState.contactDeptName = ''
}

async function loadContactDeptsCrud() {
  const response = await getContactDeptsCrud()
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Cannot load contact departments.'
    contactDeptItems.value = []
    return
  }
  contactDeptItems.value = response.data || []
}

async function submitContactDeptForm() {
  masterDataErrorMessage.value = ''
  if (!contactDeptFormState.contactDeptName.trim()) {
    masterDataErrorMessage.value = 'ContactDeptName is required.'
    return
  }
  const payload = {
    contactDeptId: contactDeptFormState.contactDeptId,
    contactDeptName: contactDeptFormState.contactDeptName.trim()
  }
  const response = contactDeptFormMode.value === 'create'
    ? await createContactDept(payload)
    : await updateContactDept(payload)
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Save contact department failed.'
    return
  }
  resetContactDeptForm()
  await loadContactDeptsCrud()
}

function editContactDeptItem(item) {
  contactDeptFormMode.value = 'edit'
  contactDeptFormState.contactDeptId = item.contactDeptId
  contactDeptFormState.contactDeptName = item.contactDeptName || ''
}

async function removeContactDeptItem(item) {
  const response = await deleteContactDept(item.contactDeptId)
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Delete contact department failed.'
    return
  }
  await loadContactDeptsCrud()
}

function resetPurposeForm() {
  purposeFormMode.value = 'create'
  purposeFormState.purposeId = null
  purposeFormState.purposeName = ''
}

async function loadPurposesCrud() {
  const response = await getPurposesCrud()
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Cannot load purposes.'
    purposeItems.value = []
    return
  }
  purposeItems.value = response.data || []
}

async function submitPurposeForm() {
  masterDataErrorMessage.value = ''
  if (!purposeFormState.purposeName.trim()) {
    masterDataErrorMessage.value = 'PurposeName is required.'
    return
  }
  const payload = {
    purposeId: purposeFormState.purposeId,
    purposeName: purposeFormState.purposeName.trim()
  }
  const response = purposeFormMode.value === 'create'
    ? await createPurpose(payload)
    : await updatePurpose(payload)
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Save purpose failed.'
    return
  }
  resetPurposeForm()
  await loadPurposesCrud()
}

function editPurposeItem(item) {
  purposeFormMode.value = 'edit'
  purposeFormState.purposeId = item.purposeId
  purposeFormState.purposeName = item.purposeName || ''
}

async function removePurposeItem(item) {
  const response = await deletePurpose(item.purposeId)
  if (!response?.success) {
    masterDataErrorMessage.value = response?.message || 'Delete purpose failed.'
    return
  }
  await loadPurposesCrud()
}

async function loadLiveMonitor() {
  monitorLoading.value = true
  monitorErrorMessage.value = ''
  const response = await getLiveMonitor({
    keyword: monitorKeyword.value.trim() || undefined,
    take: 200
  })
  monitorLoading.value = false

  if (!response?.success) {
    monitorErrorMessage.value = response?.message || 'Cannot load live monitor records.'
    monitorItems.value = []
    return
  }

  monitorItems.value = response.data || []
}

async function loadHistoryRecords() {
  historyLoading.value = true
  historyErrorMessage.value = ''
  const response = await getAccessLogHistory({
    keyword: historyKeyword.value.trim() || undefined,
    fromDate: historyFromDate.value || undefined,
    toDate: historyToDate.value || undefined,
    take: 500
  })
  historyLoading.value = false

  if (!response?.success) {
    historyErrorMessage.value = response?.message || 'Cannot load history records.'
    historyItems.value = []
    return
  }

  historyItems.value = response.data || []
}

async function exportHistoryExcel() {
  historyErrorMessage.value = ''
  const response = await exportAccessLogExcel({
    keyword: historyKeyword.value.trim() || undefined,
    fromDate: historyFromDate.value || undefined,
    toDate: historyToDate.value || undefined
  })

  if (!response?.success || !response?.data?.blob) {
    historyErrorMessage.value = response?.message || 'Export Excel failed.'
    return
  }

  const downloadUrl = URL.createObjectURL(response.data.blob)
  const anchorElement = document.createElement('a')
  anchorElement.href = downloadUrl
  anchorElement.download = response.data.fileName || 'access-log.xlsx'
  document.body.appendChild(anchorElement)
  anchorElement.click()
  anchorElement.remove()
  URL.revokeObjectURL(downloadUrl)
}

function openMonitorDetail(recordItem) {
  selectedMonitorRecord.value = recordItem
}

function closeMonitorDetail() {
  selectedMonitorRecord.value = null
}

function openHistoryPhotoPreview(photoBase64) {
  selectedHistoryPhotoBase64.value = photoBase64 || ''
}

function closeHistoryPhotoPreview() {
  selectedHistoryPhotoBase64.value = ''
}

async function submitCheckOut(recordItem) {
  if (!recordItem?.logId || checkOutSubmitting.value) return

  checkOutSubmitting.value = true
  const response = await confirmCheckOut({ logId: recordItem.logId })
  checkOutSubmitting.value = false

  if (!response?.success) {
    monitorErrorMessage.value = response?.message || 'Confirm check out failed.'
    return
  }

  closeMonitorDetail()
  await loadLiveMonitor()
  await loadHistoryRecords()
}

watch(activeTab, async (tabValue) => {
  if (tabValue === 'monitor') {
    await loadLiveMonitor()
  }
  if (tabValue === 'history') {
    await loadHistoryRecords()
  }
})

onMounted(async () => {
  updateViewportState()
  window.addEventListener('resize', updateViewportState)
  await loadUserDepartments()
  await loadUsersList()
  await loadDepartmentsCrud()
  await loadContactDeptsCrud()
  await loadPurposesCrud()
  if (activeTab.value === 'monitor') {
    await loadLiveMonitor()
  }
  if (activeTab.value === 'history') {
    await loadHistoryRecords()
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportState)
})
</script>

<template>
  <div class="access-page">
    <header class="app-header">
      <div class="app-header-inner">
        <div class="logo-wrapper">
          <img class="header-logo-img" src="/jhv-Photoroom.png" alt="JHV" />
          <div class="logo-divider"></div>
          <div class="logo-text">
            <h1>{{ headerText.title }}</h1>
            <p>{{ headerText.subtitle }}</p>
          </div>
        </div>
        <div class="header-controls">
          <div class="lang-pills">
            <button :class="['lang-pill', { active: selectedLanguageCode === 'zh' }]" @click="selectLanguage('zh')">中文</button>
            <button :class="['lang-pill', { active: selectedLanguageCode === 'en' }]" @click="selectLanguage('en')">EN</button>
            <button :class="['lang-pill', { active: selectedLanguageCode === 'vi' }]" @click="selectLanguage('vi')">VI</button>
          </div>
          <button class="btn-settings-header" @click="openSettingsPanel">
            <span class="settings-icon">⚙</span> {{ headerText.settings }}
          </button>
          <button class="btn-primary-header" @click="openCheckInModal">{{ headerText.checkIn }}</button>
        </div>
      </div>
    </header>

    <div :class="['dashboard-layout', { 'sidebar-collapsed-layout': sidebarCollapsed && !isMobileView }]">
      <div v-if="isMobileView && mobileSidebarOpen" class="sidebar-overlay" @click="mobileSidebarOpen = false"></div>
      <aside
        :class="[
          'dashboard-sidebar',
          { collapsed: sidebarCollapsed && !isMobileView },
          { 'mobile-open': isMobileView && mobileSidebarOpen },
          { 'mobile-closed': isMobileView && !mobileSidebarOpen }
        ]"
      >
        <div class="sidebar-brand">
          <div class="sidebar-brand-icon">WA</div>
          <div>
            <h3 class="sidebar-title">Admin Dashboard</h3>
            <p v-show="!sidebarCollapsed || isMobileView" class="sidebar-subtitle">Operations Navigation</p>
          </div>
        </div>
        <div class="sidebar-actions">
          <button v-if="!isMobileView" class="sidebar-toggle-btn" @click="toggleSidebarCollapsed">
            {{ sidebarCollapsed ? 'Expand' : 'Collapse' }}
          </button>
          <button v-if="isMobileView" class="sidebar-toggle-btn" @click="mobileSidebarOpen = false">Close</button>
        </div>

        <section v-for="section in sidebarSections" :key="section.code" class="sidebar-section">
          <p v-show="!sidebarCollapsed || isMobileView" class="sidebar-section-title">{{ section.label }}</p>
          <button
            v-for="pageItem in section.items"
            :key="pageItem.code"
            :class="['sidebar-link', { active: isSidebarPageActive(pageItem.code) }]"
            :title="sidebarCollapsed && !isMobileView ? pageItem.label : ''"
            @click="onSidebarPageClick(pageItem.code)"
          >
            <span class="sidebar-link-icon">{{ pageItem.iconText }}</span>
            <span v-show="!sidebarCollapsed || isMobileView">{{ pageItem.label }}</span>
          </button>
        </section>
      </aside>

      <section class="panel-section">
        <div class="panel-top-row">
          <button v-if="isMobileView" class="sidebar-mobile-open-btn" @click="toggleMobileSidebar">Menu</button>
        </div>
        <p v-if="pageError" class="error-text">{{ pageError }}</p>

      <div v-if="activeTab === 'monitor'" class="monitor-panel">
        <div class="toolbar-row">
          <input v-model="monitorKeyword" class="search-input" placeholder="Search by name/user/dept/purpose" />
          <button class="button-secondary" @click="loadLiveMonitor">Refresh</button>
        </div>
        <p v-if="monitorErrorMessage" class="error-text">{{ monitorErrorMessage }}</p>
        <p v-if="monitorLoading" class="muted-text">Loading live monitor data...</p>
        <div v-if="!monitorLoading && monitorItems.length === 0" class="muted-text">
          No users are currently checked in.
        </div>
        <div v-if="!monitorLoading && monitorItems.length > 0" class="monitor-cards-grid">
          <article v-for="recordItem in monitorItems" :key="recordItem.logId" class="monitor-style-card">
            <div class="monitor-style-card-border-top"></div>
            <div class="monitor-style-card-image-box">
              <img
                v-if="recordItem.photo"
                :src="`data:image/jpeg;base64,${recordItem.photo}`"
                class="monitor-style-card-image"
                alt="employee"
              />
              <div v-else class="monitor-style-card-image-placeholder">{{ (recordItem.fullName || '?').slice(0,1) }}</div>
            </div>
            <span class="monitor-style-card-name">{{ recordItem.fullName || 'Unknown' }}</span>
            <p class="monitor-style-card-job">{{ recordItem.userCode || '-' }} | {{ recordItem.cardNumber || '-' }}</p>
            <p class="monitor-style-card-meta">{{ formatDateTime(recordItem.checkInTime) }}</p>
            <button class="monitor-style-card-button" @click="openMonitorDetail(recordItem)">View & CheckOut</button>
          </article>
        </div>
      </div>

      <div v-if="activeTab === 'history'" class="history-panel">
        <div class="toolbar-row">
          <input v-model="historyKeyword" class="search-input" placeholder="Search by name/user/dept/purpose" />
          <input v-model="historyFromDate" class="search-input" type="datetime-local" />
          <input v-model="historyToDate" class="search-input" type="datetime-local" />
          <button class="button-secondary" @click="loadHistoryRecords">Search</button>
          <button class="button-primary" @click="exportHistoryExcel">Export Excel</button>
        </div>
        <p v-if="historyErrorMessage" class="error-text">{{ historyErrorMessage }}</p>
        <p v-if="historyLoading" class="muted-text">Loading history records...</p>
        <div class="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>LogId</th>
                <th>CheckIn Time</th>
                <th>CheckOut Time</th>
                <th>User Code</th>
                <th>Card Number</th>
                <th>Full Name</th>
                <th>Department</th>
                <th>Purpose</th>
                <th>Status</th>
                <th>Photo</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="recordItem in historyItems" :key="`history-${recordItem.logId}`">
                <td>{{ recordItem.logId }}</td>
                <td>{{ formatDateTime(recordItem.checkInTime) }}</td>
                <td>{{ formatDateTime(recordItem.checkOutTime) }}</td>
                <td>{{ recordItem.userCode || '-' }}</td>
                <td>{{ recordItem.cardNumber || '-' }}</td>
                <td>{{ recordItem.fullName || '-' }}</td>
                <td>{{ recordItem.deptName || recordItem.deptCode || '-' }}</td>
                <td>{{ recordItem.purpose || '-' }}</td>
                <td>
                  <span :class="['status-chip', recordItem.checkOutTime ? 'status-chip-out' : 'status-chip-in']">
                    {{ recordItem.checkOutTime ? 'Checked Out' : 'Inside' }}
                  </span>
                </td>
                <td>
                  <img
                    v-if="recordItem.photo"
                    :src="`data:image/jpeg;base64,${recordItem.photo}`"
                    class="history-photo-thumb"
                    alt="photo"
                    @click="openHistoryPhotoPreview(recordItem.photo)"
                  />
                  <span v-else class="muted-text">-</span>
                </td>
              </tr>
              <tr v-if="!historyLoading && historyItems.length === 0">
                <td colspan="10" class="muted-text">No history records found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="activeTab === 'settings'" class="settings-panel">
        <div class="settings-tab-row">
          <button
            v-for="settingsTab in settingsTabs"
            :key="settingsTab.code"
            :class="['tab-button', { active: settingsActiveTab === settingsTab.code }]"
            @click="settingsActiveTab = settingsTab.code"
          >
            {{ settingsTab.label }}
          </button>
        </div>

        <div v-if="settingsActiveTab === 'users'" class="users-settings-grid">
          <div class="users-form-card">
            <h3>{{ userFormMode === 'create' ? 'Add User' : 'Edit User' }}</h3>
            <div class="field-row">
              <label>User Code</label>
              <input v-model="userFormState.userCode" class="search-input" :disabled="userFormMode === 'edit'" />
              <p v-if="userFormErrors.userCode" class="error-text">{{ userFormErrors.userCode }}</p>
            </div>
            <div class="field-row">
              <label>Card Number</label>
              <input v-model="userFormState.cardNumber" class="search-input" />
            </div>
            <div class="field-row">
              <label>Full Name</label>
              <input v-model="userFormState.fullName" class="search-input" />
              <p v-if="userFormErrors.fullName" class="error-text">{{ userFormErrors.fullName }}</p>
            </div>
            <div class="field-row">
              <label>Department</label>
              <select v-model="userFormState.deptCode" class="select-input">
                <option value="">Select department...</option>
                <option v-for="department in userDepartmentOptions" :key="department.deptCode" :value="department.deptCode">
                  {{ department.deptName }}
                </option>
              </select>
              <p v-if="userFormErrors.deptCode" class="error-text">{{ userFormErrors.deptCode }}</p>
            </div>
            <div class="inline-row">
              <button class="button-primary" @click="submitUserForm">{{ userFormMode === 'create' ? 'Add User' : 'Save Changes' }}</button>
              <button class="button-secondary" @click="resetUserForm">Reset</button>
            </div>
          </div>

          <div class="users-import-card">
            <h3>Import Users (.xlsx)</h3>
            <p class="muted-text">Required headers: UserCode, FullName, DeptCode (or DeptName). Optional: CardNumber.</p>
            <div class="inline-row">
              <button class="button-secondary" @click="downloadUsersTemplate">Export Template</button>
            </div>
            <input type="file" accept=".xlsx" @change="onImportUsersFileChange" :disabled="importingUsers" />
            <p v-if="importingUsers" class="muted-text">Importing users...</p>
            <div v-if="userImportResult" class="import-result-box">
              <p><strong>Total:</strong> {{ userImportResult.totalRows }}</p>
              <p><strong>Inserted:</strong> {{ userImportResult.insertedCount }}</p>
              <p><strong>Skipped:</strong> {{ userImportResult.skippedCount }}</p>
              <div v-if="userImportResult.errors?.length">
                <strong>Errors:</strong>
                <ul class="import-error-list">
                  <li v-for="errorItem in userImportResult.errors" :key="`${errorItem.rowNumber}-${errorItem.userCode}`">
                    Row {{ errorItem.rowNumber }} - {{ errorItem.userCode || '(empty)' }}: {{ errorItem.message }}
                  </li>
                </ul>
              </div>
            </div>
          </div>

          <div class="users-list-card">
            <div class="toolbar-row">
              <input v-model="usersKeyword" class="search-input" placeholder="Search user code or full name" />
              <select v-model="usersDeptFilter" class="select-input">
                <option value="">All departments</option>
                <option v-for="department in userDepartmentOptions" :key="`filter-${department.deptCode}`" :value="department.deptCode">
                  {{ department.deptName }}
                </option>
              </select>
              <button class="button-secondary" @click="applyUsersFilter">Search</button>
              <button class="button-secondary" @click="loadUsersList">Refresh</button>
            </div>
            <p v-if="usersErrorMessage" class="error-text">{{ usersErrorMessage }}</p>
            <p v-if="usersLoading" class="muted-text">Loading users...</p>
            <div class="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>User Code</th>
                    <th>Card Number</th>
                    <th>Full Name</th>
                    <th>Department</th>
                    <th>Updated At</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="userItem in userListItems" :key="userItem.userCode">
                    <td>{{ userItem.userCode }}</td>
                    <td>{{ userItem.cardNumber || '-' }}</td>
                    <td>{{ userItem.fullName || '-' }}</td>
                    <td>{{ userItem.deptName || userItem.deptCode || '-' }}</td>
                    <td>{{ userItem.updatedAt ? formatDateTime(userItem.updatedAt) : '-' }}</td>
                    <td class="actions-col">
                      <button class="button-secondary-small" @click="editUserItem(userItem)">Edit</button>
                      <button class="button-danger-small" @click="removeUser(userItem)">Delete</button>
                    </td>
                  </tr>
                  <tr v-if="!usersLoading && userListItems.length === 0">
                    <td colspan="6" class="muted-text">No users found.</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="users-paging-row">
              <span>Total: {{ usersTotal }}</span>
              <button class="button-secondary-small" :disabled="usersPage <= 1" @click="usersPage -= 1; loadUsersList()">Prev</button>
              <span>Page {{ usersPage }}</span>
              <button
                class="button-secondary-small"
                :disabled="userListItems.length < usersPageSize"
                @click="usersPage += 1; loadUsersList()"
              >
                Next
              </button>
            </div>
          </div>
        </div>

        <div v-if="settingsActiveTab === 'departments'" class="users-settings-grid">
          <div class="users-form-card">
            <h3>{{ departmentFormMode === 'create' ? 'Add Department' : 'Edit Department' }}</h3>
            <div class="field-row"><label>Dept Code</label><input v-model="departmentFormState.deptCode" class="search-input" :disabled="departmentFormMode === 'edit'" /></div>
            <div class="field-row"><label>Dept Name</label><input v-model="departmentFormState.deptName" class="search-input" /></div>
            <div class="inline-row">
              <button class="button-primary" @click="submitDepartmentForm">{{ departmentFormMode === 'create' ? 'Add' : 'Save' }}</button>
              <button class="button-secondary" @click="resetDepartmentForm">Reset</button>
            </div>
          </div>
          <div class="users-list-card">
            <p v-if="masterDataErrorMessage" class="error-text">{{ masterDataErrorMessage }}</p>
            <div class="table-wrapper">
              <table>
                <thead><tr><th>Dept Code</th><th>Dept Name</th><th>Status</th><th>Actions</th></tr></thead>
                <tbody>
                  <tr v-for="item in departmentItems" :key="item.deptCode">
                    <td>{{ item.deptCode }}</td><td>{{ item.deptName }}</td><td>{{ item.recordStatus || '-' }}</td>
                    <td class="actions-col">
                      <button class="button-secondary-small" @click="editDepartmentItem(item)">Edit</button>
                      <button class="button-danger-small" @click="removeDepartmentItem(item)">Delete</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div v-if="settingsActiveTab === 'contactDepts'" class="users-settings-grid">
          <div class="users-form-card">
            <h3>{{ contactDeptFormMode === 'create' ? 'Add Contact Dept' : 'Edit Contact Dept' }}</h3>
            <div class="field-row"><label>Contact Dept Name</label><input v-model="contactDeptFormState.contactDeptName" class="search-input" /></div>
            <div class="inline-row">
              <button class="button-primary" @click="submitContactDeptForm">{{ contactDeptFormMode === 'create' ? 'Add' : 'Save' }}</button>
              <button class="button-secondary" @click="resetContactDeptForm">Reset</button>
            </div>
          </div>
          <div class="users-list-card">
            <p v-if="masterDataErrorMessage" class="error-text">{{ masterDataErrorMessage }}</p>
            <div class="table-wrapper">
              <table>
                <thead><tr><th>Id</th><th>Name</th><th>Status</th><th>Actions</th></tr></thead>
                <tbody>
                  <tr v-for="item in contactDeptItems" :key="item.contactDeptId">
                    <td>{{ item.contactDeptId }}</td><td>{{ item.contactDeptName }}</td><td>{{ item.recordStatus || '-' }}</td>
                    <td class="actions-col">
                      <button class="button-secondary-small" @click="editContactDeptItem(item)">Edit</button>
                      <button class="button-danger-small" @click="removeContactDeptItem(item)">Delete</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div v-if="settingsActiveTab === 'purposes'" class="users-settings-grid">
          <div class="users-form-card">
            <h3>{{ purposeFormMode === 'create' ? 'Add Purpose' : 'Edit Purpose' }}</h3>
            <div class="field-row"><label>Purpose Name</label><input v-model="purposeFormState.purposeName" class="search-input" /></div>
            <div class="inline-row">
              <button class="button-primary" @click="submitPurposeForm">{{ purposeFormMode === 'create' ? 'Add' : 'Save' }}</button>
              <button class="button-secondary" @click="resetPurposeForm">Reset</button>
            </div>
          </div>
          <div class="users-list-card">
            <p v-if="masterDataErrorMessage" class="error-text">{{ masterDataErrorMessage }}</p>
            <div class="table-wrapper">
              <table>
                <thead><tr><th>Id</th><th>Name</th><th>Status</th><th>Actions</th></tr></thead>
                <tbody>
                  <tr v-for="item in purposeItems" :key="item.purposeId">
                    <td>{{ item.purposeId }}</td><td>{{ item.purposeName }}</td><td>{{ item.recordStatus || '-' }}</td>
                    <td class="actions-col">
                      <button class="button-secondary-small" @click="editPurposeItem(item)">Edit</button>
                      <button class="button-danger-small" @click="removePurposeItem(item)">Delete</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div v-if="settingsActiveTab === 'advanced'" class="muted-text">
          Advanced settings will be added in next phase.
        </div>
      </div>
      </section>
    </div>

    <div v-if="checkInModalOpen" class="modal-backdrop">
      <div class="modal-box checkin-modal-box">
        <div class="checkin-modal-header">
          <div>
            <h2>CheckIn Workflow</h2>
            <p class="muted-text">Scan card, verify employee, then proceed to face scan.</p>
          </div>
          <div class="checkin-step-chips">
            <span :class="['checkin-step-chip', { active: checkInStep === 1 }]">1. Card</span>
            <span :class="['checkin-step-chip', { active: checkInStep === 2 }]">2. Face</span>
          </div>
        </div>
        <div v-if="checkInStep === 1">
          <div class="scanner-panel">
            <div :class="['scanner-device', checkInScannerState]">
              <div class="scanner-glow"></div>
              <div class="scanner-line"></div>
              <div class="scanner-icon">💳</div>
            </div>
            <div class="scanner-status">
              <strong v-if="checkInScannerState === 'waiting'">Waiting for card scan...</strong>
              <strong v-else-if="checkInScannerState === 'detecting'">Detecting card...</strong>
              <strong v-else>Card detected successfully</strong>
              <span>Use scanner device to read card number, verify employee info, then continue.</span>
            </div>
          </div>
          <div class="field-row">
            <label>Card Number</label>
            <div class="inline-row">
              <input
                ref="checkInCardInputElement"
                v-model="checkInCardNumber"
                class="search-input"
                placeholder="Enter card number"
                :disabled="lookupByCardLoading"
                @keydown.enter.prevent="lookupCardAndPrefill"
              />
              <button class="button-secondary" :disabled="lookupByCardLoading" @click="lookupCardAndPrefill">
                {{ lookupByCardLoading ? 'Detecting...' : 'Check' }}
              </button>
            </div>
            <p v-if="checkInLookupMessage" class="muted-text">{{ checkInLookupMessage }}</p>
          </div>
          <template v-if="hasCardLookupResult">
            <div class="employee-info-card">
              <h3 class="section-title">Employee Information</h3>
              <div class="employee-info-grid">
                <div class="employee-info-item"><label>Card Number</label><span>{{ checkInFormState.cardNumber || '-' }}</span></div>
                <div class="employee-info-item"><label>User Code</label><span>{{ checkInFormState.userCode || '-' }}</span></div>
                <div class="employee-info-item"><label>Full Name</label><span>{{ checkInFormState.fullName || '-' }}</span></div>
                <div class="employee-info-item"><label>Dept Code</label><span>{{ checkInFormState.deptCode || '-' }}</span></div>
                <div class="employee-info-item"><label>Dept Name</label><span>{{ checkInFormState.deptName || '-' }}</span></div>
              </div>
              <p v-if="checkInFormErrors.userCode || checkInFormErrors.fullName" class="error-text">
                {{ checkInFormErrors.userCode || checkInFormErrors.fullName }}
              </p>
            </div>
          </template>
          <div class="inline-row checkin-action-row">
            <button class="button-secondary" @click="closeCheckInModal">Cancel</button>
            <button class="button-primary" :disabled="!hasCardLookupResult" @click="goToCheckInPhotoStep">Next</button>
          </div>
        </div>
        <div v-else>
          <h3 class="section-title">Face Scan</h3>
          <div class="photo-capture-box">
            <img v-if="checkInFormState.photo" :src="`data:image/jpeg;base64,${checkInFormState.photo}`" class="capture-preview" alt="checkin" />
            <div v-else class="muted-text">No face image captured yet</div>
          </div>
          <div class="inline-row checkin-action-row">
            <button class="button-secondary" @click="openCheckInCamera">Open Camera</button>
            <button class="button-secondary" @click="checkInFormState.photo = ''">Skip</button>
          </div>
          <div class="inline-row checkin-action-row">
            <button class="button-secondary" @click="checkInStep = 1">Back</button>
            <button class="button-primary" :disabled="submitCheckInLoading" @click="submitCheckIn">Submit CheckIn</button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="checkInCameraOpen" class="modal-backdrop camera-layer">
      <div class="camera-box">
        <video ref="checkInVideoElement" autoplay playsinline class="camera-video"></video>
        <div class="inline-row"><button class="button-secondary" @click="closeCheckInCamera">Cancel</button><button class="button-primary" @click="captureCheckInPhoto">Capture</button></div>
      </div>
    </div>

    <div v-if="selectedMonitorRecord" class="modal-backdrop">
      <div class="modal-box detail-modal-box">
        <h2>CheckIn Detail</h2>
        <dl class="detail-grid">
          <dt>LogId</dt><dd>{{ selectedMonitorRecord.logId }}</dd>
          <dt>User Code</dt><dd>{{ selectedMonitorRecord.userCode || '-' }}</dd>
          <dt>Card Number</dt><dd>{{ selectedMonitorRecord.cardNumber || '-' }}</dd>
          <dt>Full Name</dt><dd>{{ selectedMonitorRecord.fullName || '-' }}</dd>
          <dt>Department</dt><dd>{{ selectedMonitorRecord.deptName || selectedMonitorRecord.deptCode || '-' }}</dd>
          <dt>Purpose</dt><dd>{{ selectedMonitorRecord.purpose || '-' }}</dd>
          <dt>Contact Dept</dt><dd>{{ selectedMonitorRecord.contactDept || '-' }}</dd>
          <dt>CheckIn Time</dt><dd>{{ formatDateTime(selectedMonitorRecord.checkInTime) }}</dd>
        </dl>
        <div class="photo-capture-box">
          <img
            v-if="selectedMonitorRecord.photo"
            :src="`data:image/jpeg;base64,${selectedMonitorRecord.photo}`"
            class="capture-preview"
            alt="checkin"
          />
          <div v-else class="muted-text">No check-in photo</div>
        </div>
        <div class="inline-row checkin-action-row">
          <button class="button-secondary" @click="closeMonitorDetail">Close</button>
          <button class="button-primary" :disabled="checkOutSubmitting" @click="submitCheckOut(selectedMonitorRecord)">
            {{ checkOutSubmitting ? 'Processing...' : 'Confirm CheckOut' }}
          </button>
        </div>
      </div>
    </div>

    <div v-if="selectedHistoryPhotoBase64" class="modal-backdrop" @click="closeHistoryPhotoPreview">
      <div class="modal-box photo-preview-modal-box" @click.stop>
        <h2>Photo Preview</h2>
        <img :src="`data:image/jpeg;base64,${selectedHistoryPhotoBase64}`" class="history-photo-preview-large" alt="history photo preview" />
        <div class="inline-row checkin-action-row">
          <button class="button-secondary" @click="closeHistoryPhotoPreview">Close</button>
        </div>
      </div>
    </div>
  </div>
</template>
