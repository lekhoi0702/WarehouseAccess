<script setup>
import { ref, onMounted } from 'vue';
import { useI18n } from './composables/useI18n';
import { useRecords } from './composables/useRecords';

// Layout & Dashboard Components
import Header from './components/dashboard/Header.vue';
import StatsGrid from './components/dashboard/StatsGrid.vue';
import LiveMonitor from './components/dashboard/LiveMonitor.vue';
import HistoryLog from './components/dashboard/HistoryLog.vue';
import KioskTerminal from './components/kiosk/KioskTerminal.vue';
import ToastContainer from './components/common/ToastContainer.vue';

// Modal Sheets Components
import RegisterModal from './components/modals/RegisterModal.vue';
import CheckoutModal from './components/modals/CheckoutModal.vue';
import DetailModal from './components/modals/DetailModal.vue';
import TodayVisitorsModal from './components/modals/TodayVisitorsModal.vue';
import SettingsModal from './components/modals/SettingsModal.vue';

const { t } = useI18n();
const { 
  records, 
  departments, 
  purposes, 
  employees, 
  defaultContact, 
  refreshData,
  handleEntry,
  handleCheckout
} = useRecords();

// Active View & Modal Dialog states
const activeTab = ref('monitor'); // 'monitor' | 'history'
const showRegister = ref(false);
const showCheckout = ref(null); // record object or null
const showDetail = ref(null); // record object or null
const showTodayVisitors = ref(false);
const showSettings = ref(false);
const showKiosk = ref(false);

onMounted(() => {
  refreshData();
});

// Checkout action confirm handler
async function onConfirmCheckout(photo) {
  if (!showCheckout.value) return;
  const res = await handleCheckout(showCheckout.value.id, photo);
  if (res.success) {
    showCheckout.value = null;
  } else {
    alert("離場登記失敗，請檢查 API");
  }
}

// Entry registration submit handler
async function onSubmitEntry(form) {
  const res = await handleEntry(form);
  if (res.success) {
    showRegister.value = false;
  } else {
    alert("進場登記失敗，請檢查 API");
  }
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 flex flex-col antialiased">
    <!-- Header Banner -->
    <Header 
      @open-kiosk="showKiosk = true"
      @open-settings="showSettings = true"
      @open-register="showRegister = true"
    />

    <!-- Main Content -->
    <main class="flex-1 max-w-[1240px] w-full mx-auto px-6 py-6 box-border">
      <!-- Contact Info Bar -->
      <div class="inline-flex items-center gap-2 bg-emerald-50 border border-emerald-100 rounded-full px-4 py-1.5 mb-5 shadow-sm">
        <span class="w-2 h-2 rounded-full bg-emerald-500 relative flex shrink-0">
          <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
        </span>
        <span class="text-xs font-semibold text-emerald-800">
          接洽人員：<strong>{{ defaultContact.name }}</strong> · {{ defaultContact.dept }}
        </span>
      </div>

      <!-- Stats Metric Cards -->
      <StatsGrid 
        @select-monitor="activeTab = 'monitor'"
        @open-today-visitors="showTodayVisitors = true"
      />

      <!-- Navigation Tabs Row -->
      <nav class="flex gap-2 mb-5 items-center">
        <button 
          :class="[
            'px-5 py-2.5 rounded-xl font-bold text-sm transition-all duration-200 active:scale-95 shadow-sm',
            activeTab === 'monitor' 
              ? 'bg-[#e0eafc] text-primary border border-[#b8cce8]/40' 
              : 'bg-slate-200/70 hover:bg-slate-200 text-slate-600 hover:text-slate-800'
          ]" 
          @click="activeTab = 'monitor'"
        >
          👤 {{ t.monitor }}
        </button>
        
        <button 
          :class="[
            'px-5 py-2.5 rounded-xl font-bold text-sm transition-all duration-200 active:scale-95 shadow-sm',
            activeTab === 'history' 
              ? 'bg-[#e0eafc] text-primary border border-[#b8cce8]/40' 
              : 'bg-slate-200/70 hover:bg-slate-200 text-slate-600 hover:text-slate-800'
          ]" 
          @click="activeTab = 'history'"
        >
          📋 {{ t.history }}
        </button>
      </nav>

      <!-- Dashboard Panels -->
      <transition name="fade" mode="out-in">
        <LiveMonitor 
          v-if="activeTab === 'monitor'" 
          @checkout="r => showCheckout = r"
          @detail="r => showDetail = r"
        />
        <HistoryLog 
          v-else-if="activeTab === 'history'"
          @checkout="r => showCheckout = r"
          @detail="r => showDetail = r"
        />
      </transition>
    </main>

    <!-- Floating Global Toasts alerts -->
    <ToastContainer />

    <!-- Modals Layer using transitions -->
    <transition name="modal">
      <RegisterModal 
        v-if="showRegister" 
        :t="t" 
        :departments="departments" 
        :purposes="purposes" 
        :employees="employees"
        :defaultContact="defaultContact"
        @submit="onSubmitEntry" 
        @close="showRegister = false" 
      />
    </transition>

    <transition name="modal">
      <CheckoutModal 
        v-if="showCheckout" 
        :t="t" 
        :record="showCheckout" 
        @confirm="onConfirmCheckout" 
        @close="showCheckout = null" 
      />
    </transition>

    <transition name="modal">
      <DetailModal 
        v-if="showDetail" 
        :t="t" 
        :record="showDetail" 
        @close="showDetail = null" 
      />
    </transition>

    <transition name="modal">
      <TodayVisitorsModal 
        v-if="showTodayVisitors" 
        :t="t" 
        :records="records" 
        @close="showTodayVisitors = false" 
      />
    </transition>

    <transition name="modal">
      <SettingsModal 
        v-if="showSettings" 
        :t="t" 
        @close="showSettings = false" 
      />
    </transition>

    <transition name="modal">
      <KioskTerminal 
        v-slot="{}"
        v-if="showKiosk"
        @close="showKiosk = false"
      />
    </transition>
  </div>
</template>

<style>
/* Transition fading definitions */
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.15s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>
