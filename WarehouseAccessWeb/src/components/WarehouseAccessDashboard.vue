<script setup>
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from '../composables/useI18n'
import { useRecords } from '../composables/useRecords'

// Layout & Dashboard Components
import Header from './dashboard/Header.vue'
import StatsGrid from './dashboard/StatsGrid.vue'
import LiveMonitor from './dashboard/LiveMonitor.vue'
import HistoryLog from './dashboard/HistoryLog.vue'
import ToastContainer from './common/ToastContainer.vue'

// Settings Config Modules
import UsersConfig from './settings/UsersConfig.vue'
import DepartmentsConfig from './settings/DepartmentsConfig.vue'
import ContactDeptsConfig from './settings/ContactDeptsConfig.vue'
import PurposesConfig from './settings/PurposesConfig.vue'

// Modals
import CheckInModal from './modals/CheckInModal.vue'
import DetailModal from './modals/DetailModal.vue'

import jhvLogo from '../assets/jhv_logo.png'

const props = defineProps({
  initialTab: { type: String, default: 'monitor' },
  initialSettingsTab: { type: String, default: 'users' }
})

const router = useRouter()
const { t } = useI18n()
const {
  loadUserDepartments,
  loadDepartmentsCrud,
  loadContactDeptsCrud,
  loadPurposesCrud,
  loadLiveMonitor,
  loadHistoryRecords
} = useRecords()

const activeTab = ref(props.initialTab)
const settingsActiveTab = ref(props.initialSettingsTab)

const SIDEBAR_COLLAPSE_STORAGE_KEY = 'wa_sidebar_collapsed'
const MOBILE_BREAKPOINT_PX = 1024

const sidebarCollapsed = ref(localStorage.getItem(SIDEBAR_COLLAPSE_STORAGE_KEY) === '1')
const isMobileView = ref(false)
const mobileSidebarOpen = ref(false)

const showCheckInModal = ref(false)
const selectedRecord = ref(null)

const sidebarSections = [
  {
    code: 'operations',
    label: 'Operations',
    items: [
      { code: 'monitor', label: 'Live Monitor', icon: '👤', path: '/monitor' },
      { code: 'history', label: 'History', icon: '📋', path: '/history' }
    ]
  },
  {
    code: 'master-data',
    label: 'Master Data',
    items: [
      { code: 'settings', label: 'Master Data', icon: '⚙', path: '/settings/users' }
    ]
  }
]

const settingsTabs = [
  { code: 'users', label: 'Users' },
  { code: 'departments', label: 'Department' },
  { code: 'contactDepts', label: 'Contact Depts' },
  { code: 'purposes', label: 'Purpose' },
  { code: 'advanced', label: 'Advanced' }
]

// Keep refs synced with route updates
watch(() => props.initialTab, (newTab) => {
  activeTab.value = newTab
})

watch(() => props.initialSettingsTab, (newSettingsTab) => {
  settingsActiveTab.value = newSettingsTab
})

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

function handleSidebarToggle() {
  if (isMobileView.value) {
    mobileSidebarOpen.value = !mobileSidebarOpen.value
  } else {
    toggleSidebarCollapsed()
  }
}

function isSidebarPageActive(pageCode) {
  if (pageCode.startsWith('settings:')) {
    const settingCode = pageCode.split(':')[1]
    return activeTab.value === 'settings' && settingsActiveTab.value === settingCode
  }
  return activeTab.value === pageCode
}

function onSidebarPageClick(item) {
  if (isMobileView.value) {
    mobileSidebarOpen.value = false
  }
  router.push(item.path)
}

function openSettingsPanel() {
  router.push('/settings/users')
}

function openDetail(record) {
  selectedRecord.value = record
}

async function handleCheckInSuccess() {
  await Promise.all([
    loadLiveMonitor(),
    loadHistoryRecords()
  ])
}

async function handleCheckOutSuccess() {
  await Promise.all([
    loadLiveMonitor(),
    loadHistoryRecords()
  ])
}

onMounted(async () => {
  updateViewportState()
  window.addEventListener('resize', updateViewportState)

  // Prefetch settings data for check-in dropdowns
  await Promise.all([
    loadUserDepartments(),
    loadDepartmentsCrud(),
    loadContactDeptsCrud(),
    loadPurposesCrud()
  ])
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateViewportState)
})
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 flex flex-col antialiased">
    <!-- Top Header -->
    <Header 
      @open-settings="openSettingsPanel"
      @open-checkin="showCheckInModal = true"
    />

    <!-- Layout Wrapper -->
    <div class="flex-1 flex relative">
      <!-- Mobile Sidebar Burger Toggle -->
      <button 
        v-if="isMobileView" 
        @click="mobileSidebarOpen = !mobileSidebarOpen"
        class="fixed bottom-6 right-6 w-14 h-14 bg-[#0e4391] hover:bg-[#0a3575] text-white rounded-full shadow-2xl flex items-center justify-center z-40 transition-transform active:scale-90"
      >
        <span class="text-xl">☰</span>
      </button>

      <!-- Sidebar Overlay (Mobile) -->
      <div 
        v-if="isMobileView && mobileSidebarOpen" 
        @click="mobileSidebarOpen = false"
        class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-40"
      ></div>

      <!-- Navigation Sidebar -->
      <aside
        :class="[
          'bg-white border-r border-slate-200/80 flex flex-col transition-all duration-300 z-50 shrink-0',
          isMobileView 
            ? 'fixed top-0 bottom-0 left-0 w-64 shadow-2xl transform transition-transform duration-300' 
            : sidebarCollapsed ? 'w-20' : 'w-64',
          isMobileView && !mobileSidebarOpen ? '-translate-x-full' : 'translate-x-0'
        ]"
      >
        <!-- Sidebar Brand Banner -->
        <div 
          :class="[
            'border-b border-slate-100 flex items-center transition-all duration-300',
            sidebarCollapsed && !isMobileView ? 'p-4 justify-center' : 'p-6 justify-between'
          ]"
        >
          <div v-if="!sidebarCollapsed || isMobileView" class="flex items-center gap-3 overflow-hidden">
            <img :src="jhvLogo" alt="JHV Logo" class="w-8 h-8 object-contain shrink-0" />
            <div class="leading-none text-left">
              <span class="text-xs font-black text-slate-800 block">ADMIN SHELL</span>
              <span class="text-[10px] text-slate-400 font-semibold mt-0.5 block">Gate Console</span>
            </div>
          </div>
          
          <!-- Animated Hamburger Button inside Sidebar Header -->
          <button 
            @click="handleSidebarToggle"
            class="flex flex-col justify-center items-center w-8 h-8 rounded-lg hover:bg-slate-100 active:scale-95 transition-all duration-200 focus:outline-none relative shrink-0"
            title="Toggle Sidebar"
          >
            <span 
              :class="[
                'w-4 h-0.5 bg-slate-500 rounded-full transition-all duration-300 origin-center',
                (isMobileView ? mobileSidebarOpen : !sidebarCollapsed) ? 'rotate-45 translate-y-[4.5px]' : ''
              ]"
            ></span>
            <span 
              :class="[
                'w-4 h-0.5 bg-slate-500 rounded-full my-[3px] transition-all duration-200',
                (isMobileView ? mobileSidebarOpen : !sidebarCollapsed) ? 'opacity-0 scale-x-0' : 'opacity-100'
              ]"
            ></span>
            <span 
              :class="[
                'w-4 h-0.5 bg-slate-500 rounded-full transition-all duration-300 origin-center',
                (isMobileView ? mobileSidebarOpen : !sidebarCollapsed) ? '-rotate-45 -translate-y-[4.5px]' : ''
              ]"
            ></span>
          </button>
        </div>

        <!-- Navigation Lists -->
        <nav class="flex-1 p-4 space-y-6 overflow-y-auto">
          <div v-for="section in sidebarSections" :key="section.code" class="space-y-1 text-left">
            <span 
              v-if="!sidebarCollapsed || isMobileView" 
              class="px-3 text-[10px] font-bold text-slate-400 uppercase tracking-widest block mb-2"
            >
              {{ section.label }}
            </span>
            <button
              v-for="item in section.items"
              :key="item.code"
              @click="onSidebarPageClick(item)"
              :class="[
                'w-full px-3 py-2.5 rounded-xl text-sm font-semibold flex items-center gap-3 transition-all duration-150',
                isSidebarPageActive(item.code)
                  ? 'bg-blue-50/70 text-[#0e4391]'
                  : 'text-slate-600 hover:text-slate-800 hover:bg-slate-50'
              ]"
              :title="sidebarCollapsed && !isMobileView ? item.label : ''"
            >
              <span class="text-base shrink-0">{{ item.icon }}</span>
              <span v-if="!sidebarCollapsed || isMobileView" class="truncate">{{ item.label }}</span>
            </button>
          </div>
        </nav>
      </aside>

      <!-- Main Panel View Container -->
      <main class="flex-1 min-w-0 bg-slate-50 px-6 py-8 flex flex-col">
        <!-- Metric Summary Stats (Only visible in Operations pages) -->
        <StatsGrid 
          v-if="activeTab === 'monitor' || activeTab === 'history'"
          @select-monitor="router.push('/monitor')"
          @select-history="router.push('/history')"
        />

        <!-- Active View Panel -->
        <div class="flex-1">
          <transition name="fade" mode="out-in">
            <!-- Operations Monitor -->
            <LiveMonitor 
              v-if="activeTab === 'monitor'" 
              @detail="openDetail"
            />
            
            <!-- Operations History -->
            <HistoryLog 
              v-else-if="activeTab === 'history'"
              @detail="openDetail"
            />

            <!-- Configuration Panels Layout -->
            <div v-else-if="activeTab === 'settings'" class="bg-white border border-slate-200/80 rounded-2xl p-6 shadow-sm flex flex-col h-full text-left">
              <div class="border-b border-slate-100 pb-4 mb-6">
                <h2 class="text-lg font-bold text-slate-800">System Configuration</h2>
                <p class="text-xs text-slate-400 mt-1">Manage users, departments, visit purposes and gate terminals.</p>
              </div>

              <!-- Secondary Settings Tab Row -->
              <nav class="flex flex-wrap gap-2 mb-6 border-b border-slate-100 pb-3">
                <button
                  v-for="tab in settingsTabs"
                  :key="tab.code"
                  @click="router.push(`/settings/${tab.code === 'users' ? 'users' : tab.code === 'departments' ? 'departments' : tab.code === 'contactDepts' ? 'contact-depts' : tab.code === 'purposes' ? 'purposes' : 'users'}`)"
                  :class="[
                    'px-4 py-2 text-xs font-bold rounded-lg border transition duration-150',
                    settingsActiveTab === tab.code
                      ? 'bg-slate-800 text-white border-slate-800'
                      : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'
                  ]"
                >
                  {{ tab.label }}
                </button>
              </nav>

              <!-- Settings View Panel Component Switcher -->
              <div class="flex-1">
                <UsersConfig v-if="settingsActiveTab === 'users'" />
                <DepartmentsConfig v-else-if="settingsActiveTab === 'departments'" />
                <ContactDeptsConfig v-else-if="settingsActiveTab === 'contactDepts'" />
                <PurposesConfig v-else-if="settingsActiveTab === 'purposes'" />
                <div v-else-if="settingsActiveTab === 'advanced'" class="py-10 text-center text-slate-400 font-semibold text-sm">
                  Advanced system configs will be available in the next deployment.
                </div>
              </div>
            </div>
          </transition>
        </div>
      </main>
    </div>

    <!-- Modals Layer -->
    <transition name="modal">
      <CheckInModal 
        v-slot="{}"
        v-if="showCheckInModal" 
        @close="showCheckInModal = false"
        @success="handleCheckInSuccess"
      />
    </transition>

    <transition name="modal">
      <DetailModal 
        v-slot="{}"
        v-if="selectedRecord"
        :record="selectedRecord"
        @close="selectedRecord = null"
        @checkoutSuccess="handleCheckOutSuccess"
      />
    </transition>

    <!-- Floating Global Toasts Alerts -->
    <ToastContainer />
  </div>
</template>

<style scoped>
/* Fade transitions */
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>
