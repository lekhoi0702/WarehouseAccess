<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useI18n } from '../../composables/useI18n';
import { useRecords } from '../../composables/useRecords';

const { t } = useI18n();
const { 
  monitorItems, 
  monitorLoading, 
  monitorErrorMessage, 
  loadLiveMonitor 
} = useRecords();

defineEmits(['detail']);

const keyword = ref('');
const now = ref(Date.now());
let timer = null;

onMounted(() => {
  loadLiveMonitor();
  timer = setInterval(() => {
    now.value = Date.now();
  }, 10000); // Ticks every 10 seconds
});

onUnmounted(() => {
  if (timer) clearInterval(timer);
});

// Watch keyword to automatically reload results with debounce or on change
watch(keyword, (newVal) => {
  loadLiveMonitor(newVal);
});

// Stay Clocks
function getStayDuration(checkInTimeStr) {
  if (!checkInTimeStr) return "—";
  const entryTime = new Date(checkInTimeStr).getTime();
  const ms = now.value - entryTime;
  if (ms < 0) return "0m";
  const m = Math.floor(ms / 60000);
  const h = Math.floor(m / 60);
  return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
}

function isLongStay(checkInTimeStr) {
  if (!checkInTimeStr) return false;
  const entryTime = new Date(checkInTimeStr).getTime();
  return (now.value - entryTime) > 3600000; // Greater than 1 hr
}

function formatSimpleTime(checkInTimeStr) {
  if (!checkInTimeStr) return "—";
  const d = new Date(checkInTimeStr);
  const pad = (n) => n.toString().padStart(2, '0');
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}
</script>

<template>
  <div class="space-y-6">
    <!-- Filter bar -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white/70 p-4 rounded-xl border border-slate-200/80 shadow-sm glassmorphism">
      <!-- Search Input -->
      <div class="relative flex-1 max-w-md">
        <span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 text-sm">🔍</span>
        <input 
          type="text" 
          v-model="keyword" 
          placeholder="Search name, code, dept, purpose..." 
          class="w-full pl-9 pr-4 py-2 text-sm text-slate-800 bg-slate-50 border border-slate-200 rounded-lg outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/5 transition-all"
        />
      </div>

      <!-- Refresh Action -->
      <button 
        class="bg-white hover:bg-slate-50 border border-slate-200 text-slate-700 text-xs font-bold px-4 py-2 rounded-lg transition active:scale-95 flex items-center gap-1.5"
        @click="loadLiveMonitor(keyword)"
        :disabled="monitorLoading"
      >
        <span v-if="monitorLoading" class="w-3.5 h-3.5 border-2 border-slate-400 border-t-transparent rounded-full animate-spin"></span>
        <span>Refresh</span>
      </button>
    </div>

    <!-- Error View -->
    <p v-if="monitorErrorMessage" class="text-xs text-red-500 font-semibold">{{ monitorErrorMessage }}</p>

    <!-- Active List Grid -->
    <div>
      <div v-if="monitorLoading && monitorItems.length === 0" class="py-20 text-center text-slate-400 font-semibold text-sm">
        <div class="w-8 h-8 border-4 border-slate-200 border-t-primary rounded-full animate-spin mx-auto mb-3"></div>
        Loading live monitor data...
      </div>

      <div v-else-if="monitorItems.length === 0" class="flex flex-col items-center justify-center py-20 text-center bg-white/50 rounded-xl border border-dashed border-slate-300/80 glassmorphism">
        <div class="text-4xl mb-3 text-slate-400">📂</div>
        <p class="text-slate-500 font-medium text-sm">{{ t.noVisitors }}</p>
      </div>

      <!-- Cards Grid -->
      <transition-group 
        v-else
        tag="div" 
        name="monitor-list"
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6"
        enter-active-class="transition duration-300 ease-out transform"
        enter-from-class="opacity-0 scale-95 translate-y-4"
        enter-to-class="opacity-100 scale-100 translate-y-0"
        leave-active-class="transition duration-200 ease-in transform absolute"
        leave-from-class="opacity-100 scale-100 translate-y-0"
        leave-to-class="opacity-0 scale-95 translate-y-4"
      >
        <div 
          v-for="r in monitorItems" 
          :key="r.logId" 
          class="bg-white border border-slate-200/80 rounded-xl shadow-sm hover-lift flex flex-col relative overflow-hidden"
        >
          <!-- Card Header Border Accent -->
          <div class="h-1.5 w-full bg-gradient-to-r from-primary to-primary-light"></div>

          <!-- Card Body: Avatar, details -->
          <div class="p-5 flex-1 flex flex-col justify-between">
            <div class="flex items-start gap-4">
              <!-- Avatar -->
              <div 
                class="w-16 h-20 rounded-xl border border-slate-200/60 flex items-center justify-center overflow-hidden bg-slate-50 cursor-pointer shadow-inner relative group shrink-0"
                @click="$emit('detail', r)"
              >
                <img 
                  v-if="r.photo" 
                  :src="`data:image/jpeg;base64,${r.photo}`" 
                  alt="avatar" 
                  class="w-full h-full object-cover" 
                />
                <div v-else class="text-primary/70 font-extrabold text-2xl uppercase">
                  {{ (r.fullName || '?').slice(0, 1) }}
                </div>
                <!-- View Hover Overlay -->
                <div class="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                  <span class="text-white text-[10px] font-bold">🔍 View</span>
                </div>
              </div>

              <!-- Identity Details -->
              <div class="flex-1 min-w-0 pr-2 text-left">
                <h3 
                  class="text-sm font-extrabold text-slate-800 hover:text-primary cursor-pointer truncate transition"
                  @click="$emit('detail', r)"
                >
                  {{ r.fullName || 'Unknown' }}
                </h3>
                <p class="text-[10px] font-bold text-slate-400 mt-1 truncate">
                  ID: {{ r.userCode || '-' }}
                </p>
                <p class="text-[10px] font-bold text-slate-400 mt-0.5 truncate">
                  CARD: {{ r.cardNumber || '-' }}
                </p>
                <div class="mt-2 flex flex-wrap gap-1.5" v-if="r.purpose">
                  <span class="px-1.5 py-0.5 text-[9px] font-bold bg-slate-100 text-slate-500 rounded">{{ r.purpose }}</span>
                </div>
              </div>
            </div>

            <!-- Time Indicator row -->
            <div class="mt-4 flex items-center justify-between border-t border-slate-100 pt-3">
              <div class="flex flex-col text-left">
                <span class="text-[9px] text-slate-400 font-bold uppercase tracking-wider">Entry Time</span>
                <span class="text-[11px] font-bold text-slate-700 mt-0.5">🕒 {{ formatSimpleTime(r.checkInTime) }}</span>
              </div>
              <div class="flex flex-col text-right">
                <span class="text-[9px] text-slate-400 font-bold uppercase tracking-wider">Stay Time</span>
                <span 
                  class="text-[11px] font-black mt-0.5"
                  :class="isLongStay(r.checkInTime) ? 'text-red-500' : 'text-emerald-500'"
                >
                  ⏳ {{ getStayDuration(r.checkInTime) }}
                </span>
              </div>
            </div>
          </div>

          <!-- Card Bottom Action -->
          <div class="px-5 py-3 bg-slate-50/80 border-t border-slate-100 flex justify-end">
            <button 
              class="w-full bg-primary/5 hover:bg-primary text-primary hover:text-white text-xs font-bold py-2 rounded-lg active:scale-95 transition"
              @click="$emit('detail', r)"
            >
              View & CheckOut
            </button>
          </div>
        </div>
      </transition-group>
    </div>
  </div>
</template>

<style scoped>
.monitor-list-move {
  transition: transform 0.3s ease;
}
</style>
