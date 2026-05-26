<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useI18n } from '../../composables/useI18n';
import { useRecords } from '../../composables/useRecords';

const { t } = useI18n();
const { activeOnSiteVisitors } = useRecords();

const emit = defineEmits(['checkout', 'detail']);

// Local search and filter states
const search = ref('');
const filterType = ref('');

const VISITOR_TYPES = ["vendor", "brand", "audit", "staff"];

// Dynamic clock to auto-refresh elapsed time counters
const now = ref(Date.now());
let timer = null;

onMounted(() => {
  timer = setInterval(() => {
    now.value = Date.now();
  }, 10000); // Update stay durations every 10s
});

onUnmounted(() => {
  if (timer) clearInterval(timer);
});

// Computed filtering logic
const filteredVisitors = computed(() => {
  return activeOnSiteVisitors.value.filter(r => {
    const q = search.value.trim().toLowerCase();
    const matchesSearch = !q || 
      (r.name && r.name.toLowerCase().includes(q)) ||
      (r.company && r.company.toLowerCase().includes(q)) ||
      (r.contact && r.contact.toLowerCase().includes(q));

    const matchesType = !filterType.value || r.type === filterType.value;
    return matchesSearch && matchesType;
  });
});

// Stay duration calculation using active clock
function getStayDuration(entryTime) {
  const ms = now.value - entryTime;
  if (ms < 0) return "0m";
  const m = Math.floor(ms / 60000);
  const h = Math.floor(m / 60);
  return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
}

function isLongStay(entryTime) {
  return (now.value - entryTime) > 3600000; // Greater than 1 hour
}

function formatSimpleTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  const pad = (n) => n.toString().padStart(2, '0');
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

const TYPE_CLASSES = {
  vendor: 'bg-orange-50 text-orange-600 border-orange-200/60',
  brand: 'bg-emerald-50 text-emerald-600 border-emerald-200/60',
  audit: 'bg-red-50 text-red-600 border-red-200/60',
  staff: 'bg-blue-50 text-blue-600 border-blue-200/60'
};
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
          v-model="search" 
          :placeholder="t.searchPlaceholder" 
          class="w-full pl-9 pr-4 py-2 text-sm text-slate-800 bg-slate-50 border border-slate-200 rounded-lg outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/5 transition-all"
        />
      </div>

      <!-- Type Select -->
      <div class="flex items-center gap-3">
        <select 
          v-model="filterType" 
          class="bg-slate-50 border border-slate-200 text-sm text-slate-700 px-3 py-2 rounded-lg outline-none focus:border-primary/50 cursor-pointer"
        >
          <option value="">{{ t.allTypes }}</option>
          <option v-for="type in VISITOR_TYPES" :key="type" :value="type">{{ t.types[type] }}</option>
        </select>
      </div>
    </div>

    <!-- Active List Grid -->
    <div>
      <div v-if="filteredVisitors.length === 0" class="flex flex-col items-center justify-center py-20 text-center bg-white/50 rounded-xl border border-dashed border-slate-300/80 glassmorphism">
        <div class="text-4xl mb-3 text-slate-400">📂</div>
        <p class="text-slate-500 font-medium text-sm">{{ t.noVisitors }}</p>
      </div>

      <!-- Cards Grid with Transition -->
      <transition-group 
        v-else 
        tag="div" 
        name="monitor-list"
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6"
        enter-active-class="transition duration-300 ease-out transform"
        enter-from-class="opacity-0 scale-95 translate-y-4"
        enter-to-class="opacity-100 scale-100 translate-y-0"
        leave-active-class="transition duration-200 ease-in transform absolute"
        leave-from-class="opacity-100 scale-100 translate-y-0"
        leave-to-class="opacity-0 scale-95 translate-y-4"
      >
        <div 
          v-for="r in filteredVisitors" 
          :key="r.id" 
          class="bg-white border border-slate-200/80 rounded-xl shadow-sm hover-lift flex flex-col relative overflow-hidden"
        >
          <!-- Card Header Badge -->
          <div class="absolute top-4 right-4">
            <span 
              class="px-2.5 py-0.5 text-[10px] font-bold rounded-full border" 
              :class="TYPE_CLASSES[r.type]"
            >
              {{ t.types[r.type] }}
            </span>
          </div>

          <!-- Card Body: Profile, identity, and time -->
          <div class="p-5 flex-1 flex flex-col justify-between">
            <div class="flex items-start gap-4">
              <!-- Avatar Circle -->
              <div 
                class="w-16 h-16 rounded-full border border-slate-200/60 flex flex-col items-center justify-center overflow-hidden bg-slate-50 cursor-pointer shadow-inner relative group shrink-0"
                @click="$emit('detail', r)"
              >
                <img v-if="r.type === 'staff' && r.empAvatar" :src="r.empAvatar" alt="staff" class="w-4/5 h-4/5 object-contain" />
                <img v-else-if="r.photo" :src="r.photo" alt="visitor" class="w-full h-full object-cover" />
                <div v-else class="text-slate-400">
                  <svg class="w-7 h-7" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z" />
                  </svg>
                </div>
                <!-- Hover Overlay -->
                <div class="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                  <span class="text-white text-[10px] font-bold">🔍 View</span>
                </div>
              </div>

              <!-- Identity Info -->
              <div class="flex-1 min-w-0 pr-10">
                <h3 
                  class="text-base font-bold text-slate-800 hover:text-primary cursor-pointer truncate transition"
                  @click="$emit('detail', r)"
                >
                  {{ r.name }}
                </h3>
                <p class="text-xs font-semibold text-slate-500 truncate mt-0.5">{{ r.company }}</p>
                <div class="mt-2 flex flex-wrap gap-1.5">
                  <span class="px-2 py-0.5 text-[10px] font-semibold bg-slate-100 text-slate-600 rounded-md">{{ r.purpose }}</span>
                </div>
              </div>
            </div>

            <!-- Time Indicator row -->
            <div class="mt-4 flex items-center justify-between border-t border-slate-100 pt-3">
              <div class="flex flex-col text-left">
                <span class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">Entry Time</span>
                <span class="text-xs font-bold text-slate-700 mt-0.5">🕒 {{ formatSimpleTime(r.entryTime) }}</span>
              </div>
              <div class="flex flex-col text-right">
                <span class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">Stay Time</span>
                <span 
                  class="text-xs font-black mt-0.5"
                  :class="isLongStay(r.entryTime) ? 'text-red-500' : 'text-emerald-500'"
                >
                  ⏳ {{ getStayDuration(r.entryTime) }}
                </span>
              </div>
            </div>
          </div>

          <!-- Card Bottom Actions Footer -->
          <div class="px-5 py-3.5 bg-slate-50/80 border-t border-slate-100 flex justify-between items-center">
            <span class="text-[11px] font-semibold text-slate-500 flex items-center gap-1">
              <span class="text-xs text-primary/70">👤</span> {{ r.contact }}
            </span>
            <button 
              class="bg-red-500 text-white text-xs font-bold px-3.5 py-1.5 rounded-lg shadow-sm shadow-red-500/10 hover:bg-red-600 active:scale-95 transition-all"
              @click="$emit('checkout', r)"
            >
              {{ t.checkout }}
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
