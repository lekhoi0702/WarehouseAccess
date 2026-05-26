<script setup>
import { ref, computed } from 'vue';
import { useI18n } from '../../composables/useI18n';
import { useRecords } from '../../composables/useRecords';
import { useToast } from '../../composables/useToast';

const { t } = useI18n();
const { records } = useRecords();
const { showToast } = useToast();

const emit = defineEmits(['checkout', 'detail']);

// Local input states
const searchInput = ref('');
const typeInput = ref('');
const statusInput = ref('');
const startDateInput = ref('');
const endDateInput = ref('');

// Applied query states
const appliedSearch = ref('');
const appliedType = ref('');
const appliedStatus = ref('');
const appliedStartDate = ref('');
const appliedEndDate = ref('');

const VISITOR_TYPES = ["vendor", "brand", "audit", "staff"];

// Apply filter query
function handleHistoryQuery() {
  appliedSearch.value = searchInput.value;
  appliedType.value = typeInput.value;
  appliedStatus.value = statusInput.value;
  appliedStartDate.value = startDateInput.value;
  appliedEndDate.value = endDateInput.value;
}

// Computed filtered records
const filteredRecords = computed(() => {
  return records.value.filter(r => {
    // 1. Search Query
    const q = appliedSearch.value.trim().toLowerCase();
    const matchesSearch = !q || 
      (r.name && r.name.toLowerCase().includes(q)) ||
      (r.company && r.company.toLowerCase().includes(q)) ||
      (r.contact && r.contact.toLowerCase().includes(q));

    // 2. Type Filter
    const matchesType = !appliedType.value || r.type === appliedType.value;

    // 3. Status Filter
    const matchesStatus = !appliedStatus.value || 
      (appliedStatus.value === 'inside' ? !r.exitTime : !!r.exitTime);

    // 4. Date Range Filter
    if (appliedStartDate.value) {
      const startMs = new Date(appliedStartDate.value).setHours(0, 0, 0, 0);
      if (r.entryTime < startMs) return false;
    }
    if (appliedEndDate.value) {
      const endMs = new Date(appliedEndDate.value).setHours(23, 59, 59, 999);
      if (r.entryTime > endMs) return false;
    }

    return matchesSearch && matchesType && matchesStatus;
  });
});

// CSV Export Script
function exportCSV() {
  const headers = t.value.cols.slice(0, 9).join(',');
  const rows = filteredRecords.value.map(r => {
    const typeLabel = t.value.types[r.type];
    const entryStr = formatTime(r.entryTime);
    const exitStr = r.exitTime ? formatTime(r.exitTime) : t.value.stillInside;
    const durationStr = r.exitTime ? formatDuration(r.exitTime - r.entryTime) : '';

    return `"${r.id}","${r.name}","${r.company}","${typeLabel}","${r.contact}","${r.purpose}","${entryStr}","${exitStr}","${durationStr}"`;
  });

  const csvContent = "\ufeff" + headers + "\n" + rows.join("\n");
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.setAttribute("href", url);
  link.setAttribute("download", `warehouse_visitors_${Date.now()}.csv`);
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  showToast(t.value.toastCSV);
}

// Helpers
function formatTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  return d.toLocaleString("zh-TW", { hour12: false, month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

function formatDuration(ms) {
  if (!ms || ms < 0) return "—";
  const m = Math.floor(ms / 60000), h = Math.floor(m / 60);
  return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
}

const TYPE_COLORS = { vendor: "#F97316", brand: "#22C55E", audit: "#EF4444", staff: "#3B82F6" };
</script>

<template>
  <div class="space-y-6">
    <!-- Filter bar -->
    <div class="flex flex-col xl:flex-row xl:items-center justify-between gap-4 bg-white/70 p-4 rounded-xl border border-slate-200/80 shadow-sm glassmorphism">
      <!-- Search Input -->
      <div class="relative flex-1 max-w-sm">
        <span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400 text-sm">🔍</span>
        <input 
          type="text" 
          v-model="searchInput" 
          :placeholder="t.searchPlaceholder" 
          class="w-full pl-9 pr-4 py-2 text-sm text-slate-800 bg-slate-50 border border-slate-200 rounded-lg outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/5 transition-all"
        />
      </div>

      <!-- Filter Controls Group -->
      <div class="flex flex-wrap items-center gap-3">
        <!-- Type Filter -->
        <select 
          v-model="typeInput" 
          class="bg-slate-50 border border-slate-200 text-sm text-slate-700 px-3 py-2 rounded-lg outline-none focus:border-primary/50 cursor-pointer"
        >
          <option value="">{{ t.allTypes }}</option>
          <option v-for="type in VISITOR_TYPES" :key="type" :value="type">{{ t.types[type] }}</option>
        </select>

        <!-- Status Filter -->
        <select 
          v-model="statusInput" 
          class="bg-slate-50 border border-slate-200 text-sm text-slate-700 px-3 py-2 rounded-lg outline-none focus:border-primary/50 cursor-pointer"
        >
          <option value="">{{ t.allStatus }}</option>
          <option value="inside">{{ t.inside }}</option>
          <option value="exited">{{ t.exited }}</option>
        </select>

        <!-- Date Range Filter -->
        <div class="flex items-center gap-2 bg-slate-50 border border-slate-200 px-3 py-1 rounded-lg text-sm">
          <input type="date" v-model="startDateInput" class="bg-transparent outline-none border-none text-slate-700 py-1 text-xs" />
          <span class="text-slate-400">~</span>
          <input type="date" v-model="endDateInput" class="bg-transparent outline-none border-none text-slate-700 py-1 text-xs" />
          <button 
            class="bg-primary text-white text-xs font-bold px-3 py-1.5 rounded-md hover:bg-primary-dark transition active:scale-95 ml-1" 
            @click="handleHistoryQuery"
          >
            查詢
          </button>
        </div>

        <!-- Export CSV Button -->
        <button 
          class="bg-emerald-50 text-emerald-600 border border-emerald-200 text-xs font-bold px-4 py-2 rounded-lg hover:bg-emerald-100 transition flex items-center gap-1.5 active:scale-95 shadow-sm shadow-emerald-600/5"
          @click="exportCSV"
        >
          {{ t.exportCSV }}
        </button>
      </div>
    </div>

    <!-- Table content -->
    <div class="bg-white/80 rounded-xl border border-slate-200/80 shadow-sm overflow-hidden glassmorphism">
      <div class="overflow-x-auto w-full">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50/70 border-b border-slate-100">
              <th v-for="c in t.cols" :key="c" class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">{{ c }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="filteredRecords.length === 0">
              <td colspan="10" class="p-10 text-center text-sm text-slate-400 font-semibold">{{ t.noRecords }}</td>
            </tr>
            <tr 
              v-else 
              v-for="r in filteredRecords" 
              :key="r.id"
              class="hover:bg-slate-50/50 transition duration-150"
            >
              <td class="p-4"><span class="text-xs font-mono font-bold bg-slate-100 text-slate-600 px-2 py-0.5 rounded">{{ r.id }}</span></td>
              <td class="p-4"><strong class="text-sm text-slate-800">{{ r.name }}</strong></td>
              <td class="p-4 text-sm text-slate-500">{{ r.company }}</td>
              <td class="p-4">
                <span 
                  class="px-2 py-0.5 text-[10px] font-bold rounded-md"
                  :style="{ backgroundColor: TYPE_COLORS[r.type] + '1a', color: TYPE_COLORS[r.type] }"
                >
                  {{ t.types[r.type] }}
                </span>
              </td>
              <td class="p-4 text-sm text-slate-600 font-medium">{{ r.contact }}</td>
              <td class="p-4 text-xs"><span class="px-2 py-0.5 bg-slate-100 text-slate-600 rounded-md font-semibold">{{ r.purpose }}</span></td>
              <td class="p-4 text-xs text-slate-500 font-medium">{{ formatTime(r.entryTime) }}</td>
              <td class="p-4 text-xs text-slate-500 font-medium">
                <span v-if="!r.exitTime" class="px-2 py-0.5 bg-amber-50 text-amber-600 rounded-md font-bold text-[10px]">{{ t.stillInside }}</span>
                <span v-else>{{ formatTime(r.exitTime) }}</span>
              </td>
              <td class="p-4 text-xs text-slate-500 font-bold">{{ r.exitTime ? formatDuration(r.exitTime - r.entryTime) : '—' }}</td>
              <td class="p-4">
                <div class="flex gap-2">
                  <button 
                    v-if="!r.exitTime" 
                    class="bg-red-500 text-white text-[10px] font-bold px-2 py-1 rounded shadow-sm hover:bg-red-600 transition active:scale-95" 
                    @click="$emit('checkout', r)"
                  >
                    {{ t.checkout }}
                  </button>
                  <button 
                    class="bg-primary text-white text-[10px] font-bold px-2 py-1 rounded shadow-sm hover:bg-primary-light transition active:scale-95" 
                    @click="$emit('detail', r)"
                  >
                    {{ t.details }}
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <!-- Footer table -->
      <div class="px-5 py-4 bg-slate-50/70 border-t border-slate-100 text-xs text-slate-500 font-semibold flex items-center justify-between">
        <span>
          {{ t.total }} <strong class="text-slate-800 text-sm font-black">{{ filteredRecords.length }}</strong> {{ t.records }}
        </span>
      </div>
    </div>
  </div>
</template>
