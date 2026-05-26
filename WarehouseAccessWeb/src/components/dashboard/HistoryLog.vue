<script setup>
import { ref, onMounted } from 'vue';
import { useI18n } from '../../composables/useI18n';
import { useRecords } from '../../composables/useRecords';
import { useToast } from '../../composables/useToast';

const { t } = useI18n();
const { 
  historyItems, 
  historyLoading, 
  historyErrorMessage, 
  loadHistoryRecords,
  exportHistory
} = useRecords();
const { showToast } = useToast();

const keyword = ref('');
const fromDate = ref('');
const toDate = ref('');

const selectedPhoto = ref('');

onMounted(() => {
  loadHistoryRecords();
});

function handleSearch() {
  loadHistoryRecords(keyword.value, fromDate.value, toDate.value);
}

async function handleExportExcel() {
  try {
    const res = await exportHistory(keyword.value, fromDate.value, toDate.value);
    if (res.success && res.data?.blob) {
      const downloadUrl = URL.createObjectURL(res.data.blob);
      const a = document.createElement('a');
      a.href = downloadUrl;
      a.download = res.data.fileName || 'access-log.xlsx';
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(downloadUrl);
      showToast(t.value.toastCSV || "✓ Excel exported");
    } else {
      alert(res.message || "Export Excel failed");
    }
  } catch (e) {
    alert("Export failed");
  }
}

function formatDateTime(value) {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return date.toLocaleString()
}
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
          v-model="keyword" 
          placeholder="Search name, code, dept..." 
          class="w-full pl-9 pr-4 py-2 text-sm text-slate-800 bg-slate-50 border border-slate-200 rounded-lg outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/5 transition-all"
        />
      </div>

      <!-- Filters Group -->
      <div class="flex flex-wrap items-center gap-3">
        <!-- Date ranges (datetime-local) -->
        <div class="flex items-center gap-2 bg-slate-50 border border-slate-200 px-3 py-1.5 rounded-lg text-xs">
          <input type="datetime-local" v-model="fromDate" class="bg-transparent outline-none border-none text-slate-700 py-0.5" />
          <span class="text-slate-400">~</span>
          <input type="datetime-local" v-model="toDate" class="bg-transparent outline-none border-none text-slate-700 py-0.5" />
        </div>

        <button 
          class="bg-primary hover:bg-primary-dark text-white text-xs font-bold px-4 py-2.5 rounded-lg active:scale-95 transition"
          @click="handleSearch"
        >
          Search
        </button>

        <button 
          class="bg-emerald-50 hover:bg-emerald-100 text-emerald-600 border border-emerald-200 text-xs font-bold px-4 py-2.5 rounded-lg active:scale-95 transition flex items-center gap-1.5 shadow-sm shadow-emerald-500/5"
          @click="handleExportExcel"
        >
          Export Excel
        </button>
      </div>
    </div>

    <!-- Error state -->
    <p v-if="historyErrorMessage" class="text-xs text-red-500 font-semibold">{{ historyErrorMessage }}</p>

    <!-- Table -->
    <div class="bg-white/80 rounded-xl border border-slate-200/80 shadow-sm overflow-hidden glassmorphism">
      <div class="overflow-x-auto w-full">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50/70 border-b border-slate-100">
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">LogId</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">CheckIn Time</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">CheckOut Time</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">User Code</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Card Number</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Full Name</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Department</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Purpose</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Status</th>
              <th class="p-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Photo</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="historyLoading && historyItems.length === 0">
              <td colspan="10" class="p-10 text-center text-sm text-slate-400 font-semibold">
                <div class="w-6 h-6 border-2 border-slate-200 border-t-primary rounded-full animate-spin mx-auto mb-2"></div>
                Loading history records...
              </td>
            </tr>
            <tr v-else-if="historyItems.length === 0">
              <td colspan="10" class="p-10 text-center text-sm text-slate-400 font-semibold">No history records found.</td>
            </tr>
            <tr 
              v-else 
              v-for="r in historyItems" 
              :key="`history-${r.logId}`"
              class="hover:bg-slate-50/50 transition duration-150"
            >
              <td class="p-4"><span class="text-xs font-mono font-bold bg-slate-100 text-slate-600 px-2 py-0.5 rounded">{{ r.logId }}</span></td>
              <td class="p-4 text-xs text-slate-500 font-medium">{{ formatDateTime(r.checkInTime) }}</td>
              <td class="p-4 text-xs text-slate-500 font-medium">{{ formatDateTime(r.checkOutTime) }}</td>
              <td class="p-4 text-sm text-slate-700 font-bold">{{ r.userCode || '-' }}</td>
              <td class="p-4 text-sm text-slate-500 font-medium">{{ r.cardNumber || '-' }}</td>
              <td class="p-4"><strong class="text-sm text-slate-800">{{ r.fullName || '-' }}</strong></td>
              <td class="p-4 text-sm text-slate-500">{{ r.deptName || r.deptCode || '-' }}</td>
              <td class="p-4 text-xs"><span class="px-2 py-0.5 bg-slate-100 text-slate-600 rounded-md font-semibold" v-if="r.purpose">{{ r.purpose }}</span><span v-else>-</span></td>
              <td class="p-4">
                <span 
                  class="px-2.5 py-0.5 rounded-full text-[10px] font-bold border"
                  :class="[
                    r.checkOutTime 
                      ? 'bg-blue-50 text-blue-600 border-blue-200/60' 
                      : 'bg-emerald-50 text-emerald-600 border-emerald-200/60'
                  ]"
                >
                  {{ r.checkOutTime ? 'Checked Out' : 'Inside' }}
                </span>
              </td>
              <td class="p-4">
                <div 
                  v-if="r.photo"
                  class="w-10 h-10 rounded-lg overflow-hidden border border-slate-200 cursor-zoom-in relative group shrink-0"
                  @click="selectedPhoto = r.photo"
                >
                  <img :src="`data:image/jpeg;base64,${r.photo}`" class="w-full h-full object-cover" alt="photo" />
                  <div class="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                    <span class="text-white text-[9px] font-bold">🔎</span>
                  </div>
                </div>
                <span v-else class="text-slate-400 font-semibold text-xs">-</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <!-- Footer table -->
      <div class="px-5 py-4 bg-slate-50/70 border-t border-slate-100 text-xs text-slate-500 font-semibold flex justify-between">
        <span>
          Total: <strong class="text-slate-800 text-sm font-black">{{ historyItems.length }}</strong> records
        </span>
      </div>
    </div>

    <!-- Photo Zoom Modal Layer -->
    <transition name="modal">
      <div 
        class="fixed inset-0 bg-[#0a142d]/85 backdrop-blur-md z-[8500] flex items-center justify-center p-4" 
        v-if="selectedPhoto"
        @click="selectedPhoto = ''"
      >
        <div class="bg-white border border-slate-200 rounded-3xl p-6 w-full max-w-[800px] text-slate-800 shadow-2xl relative" @click.stop>
          <h2 class="text-sm font-extrabold text-slate-900 mb-4 text-left">Photo Preview</h2>
          <div class="w-full aspect-[4/3] max-h-[65vh] rounded-2xl border border-slate-200 bg-slate-50 overflow-hidden shadow-inner flex items-center justify-center">
            <img :src="`data:image/jpeg;base64,${selectedPhoto}`" class="max-w-full max-h-full object-contain" alt="zoom preview" />
          </div>
          <button 
            class="w-full bg-slate-100 hover:bg-slate-200 border border-slate-200 text-slate-600 text-xs font-bold py-2.5 rounded-xl transition mt-5 active:scale-[0.98]"
            @click="selectedPhoto = ''"
          >
            Close
          </button>
        </div>
      </div>
    </transition>
  </div>
</template>
