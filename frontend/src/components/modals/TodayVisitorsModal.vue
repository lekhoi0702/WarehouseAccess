<script setup>
import { computed } from 'vue';

const props = defineProps({
  t: Object,
  records: Array
});

const emit = defineEmits(['close']);

const TYPE_COLORS = { vendor: "#F97316", brand: "#22C55E", audit: "#EF4444", staff: "#3B82F6" };

const todayRecords = computed(() => {
  const todayStart = new Date().setHours(0, 0, 0, 0);
  return props.records.filter(r => r.entryTime >= todayStart || !r.exitTime);
});

function formatTimeOnly(ts) {
  if (!ts) return "";
  const d = new Date(ts);
  return d.toLocaleTimeString("zh-TW", { hour12: false, hour: "2-digit", minute: "2-digit" });
}

const TYPE_BADGES = {
  vendor: 'bg-orange-50 text-orange-600',
  brand: 'bg-emerald-50 text-emerald-600',
  audit: 'bg-red-50 text-red-600',
  staff: 'bg-blue-50 text-blue-600'
};
</script>

<template>
  <div class="fixed inset-0 bg-[#0a142d]/65 backdrop-blur-md z-[8000] flex items-center justify-center p-4">
    <div class="bg-white border border-slate-200 rounded-3xl p-6 w-full max-w-md text-slate-800 shadow-2xl relative max-h-[85vh] flex flex-col">
      <!-- Header -->
      <div class="flex justify-between items-center mb-5 shrink-0">
        <div class="flex items-center gap-2">
          <span class="text-xl">📅</span>
          <h2 class="text-base font-extrabold text-slate-900 m-0">{{ t.statsToday }}明細</h2>
          <span class="bg-indigo-50 text-indigo-600 text-[10px] font-bold px-2 py-0.5 rounded-full">{{ todayRecords.length }} 人次</span>
        </div>
        <button class="text-slate-400 hover:text-slate-600 text-lg transition" @click="emit('close')">✕</button>
      </div>

      <!-- Visitor Flow List -->
      <div class="flex-1 overflow-y-auto space-y-2 pr-1 mb-4">
        <div v-if="todayRecords.length === 0" class="py-12 text-center text-sm text-slate-400 font-semibold">
          今日尚無訪客登記紀錄
        </div>
        
        <div 
          v-else 
          v-for="r in todayRecords" 
          :key="r.id" 
          class="flex items-center justify-between p-3 border border-slate-200/60 rounded-xl bg-slate-50/50 hover:bg-slate-50 transition"
        >
          <div class="flex items-center gap-3">
            <div class="w-9 h-9 rounded-full overflow-hidden border border-slate-200 bg-indigo-50/30 flex items-center justify-center shrink-0">
              <img v-if="r.type === 'staff' && r.empAvatar" :src="r.empAvatar" alt="staff" class="w-4/5 h-4/5 object-contain" />
              <img v-else-if="r.photo" :src="r.photo" alt="visitor" class="w-full h-full object-cover" />
              <span v-else class="text-sm">👤</span>
            </div>
            <div class="text-left">
              <div class="flex items-center gap-1.5">
                <h4 class="text-xs font-bold text-slate-800">{{ r.name }}</h4>
                <span 
                  class="px-1.5 py-0.2 text-[9px] font-extrabold rounded-md uppercase"
                  :class="TYPE_BADGES[r.type]"
                >
                  {{ t.types[r.type] }}
                </span>
              </div>
              <p class="text-[10px] font-semibold text-slate-400 mt-0.5 truncate max-w-[180px]">{{ r.company }} · {{ r.department || '—' }}</p>
            </div>
          </div>

          <div class="text-right text-[10px] font-semibold shrink-0">
            <div class="text-slate-600">進：<strong class="font-extrabold text-slate-800">{{ formatTimeOnly(r.entryTime) }}</strong></div>
            <div class="mt-0.5">
              <span v-if="r.exitTime" class="text-slate-400">
                出：{{ formatTimeOnly(r.exitTime) }}
              </span>
              <span v-else class="text-amber-500 font-extrabold flex items-center justify-end gap-0.5">
                在場 <span class="animate-pulse">⏱</span>
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Close Action -->
      <button 
        class="w-full bg-slate-100 hover:bg-slate-200/80 border border-slate-200 text-slate-600 text-xs font-bold py-2.5 rounded-xl transition duration-150 active:scale-[0.98] shrink-0"
        @click="emit('close')"
      >
        {{ t.close }}
      </button>
    </div>
  </div>
</template>
