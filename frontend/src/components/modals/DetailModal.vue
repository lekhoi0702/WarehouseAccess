<script setup>
import { computed } from 'vue';

const props = defineProps({
  t: Object,
  record: Object
});

const emit = defineEmits(['close']);

const TYPE_COLORS = { vendor: "#F97316", brand: "#22C55E", audit: "#EF4444", staff: "#3B82F6" };
const color = computed(() => TYPE_COLORS[props.record.type] || "#94a3b8");

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

const fields = computed(() => [
  props.record.id,
  props.record.name,
  props.record.company,
  null, // Special template for Type badge
  props.record.department,
  props.record.contact,
  props.record.purpose,
  formatTime(props.record.entryTime),
  props.record.exitTime ? formatTime(props.record.exitTime) : null, // Special template for Exit status
  props.record.exitTime ? formatDuration(props.record.exitTime - props.record.entryTime) : "—",
  props.record.note || "—",
]);
</script>

<template>
  <div class="fixed inset-0 bg-[#0a142d]/65 backdrop-blur-md z-[8000] flex items-center justify-center p-4">
    <div class="bg-white border border-slate-200 rounded-3xl p-6 w-full max-w-[420px] text-slate-800 shadow-2xl relative max-h-[90vh] overflow-y-auto">
      <!-- Header -->
      <div class="flex justify-between items-center mb-5">
        <h2 class="text-base font-extrabold text-slate-900 m-0">{{ t.detailTitle }}</h2>
        <button class="text-slate-400 hover:text-slate-600 text-lg transition" @click="emit('close')">✕</button>
      </div>

      <!-- Entry / Exit Photos side-by-side comparison -->
      <div class="grid grid-cols-2 gap-3 mb-6">
        <!-- Entry Photo -->
        <div class="flex flex-col text-left">
          <label class="text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">{{ t.entryPhoto }}</label>
          <div class="aspect-[4/3] rounded-xl overflow-hidden border border-slate-200/80 bg-slate-50 flex items-center justify-center">
            <img v-if="record.photo" :src="record.photo" :alt="t.entryPhoto" class="w-full h-full object-cover" />
            <span v-else class="text-[10px] text-slate-400 font-semibold">{{ t.noEntryPhoto }}</span>
          </div>
        </div>

        <!-- Exit Photo -->
        <div class="flex flex-col text-left" v-if="record.exitTime || record.exitPhoto">
          <label class="text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">{{ t.exitPhoto }}</label>
          <div class="aspect-[4/3] rounded-xl overflow-hidden border border-slate-200/80 bg-slate-50 flex items-center justify-center">
            <img v-if="record.exitPhoto" :src="record.exitPhoto" :alt="t.exitPhoto" class="w-full h-full object-cover" />
            <span v-else class="text-[10px] text-slate-400 font-semibold">{{ t.noExitPhoto }}</span>
          </div>
        </div>
      </div>

      <!-- Field Data Grid List -->
      <div class="divide-y divide-slate-100 mb-6">
        <div 
          v-for="(k, i) in t.detailFields" 
          :key="k"
          class="flex justify-between items-center py-2.5 text-xs text-left"
        >
          <span class="text-slate-400 font-semibold">{{ k }}</span>
          <div class="text-slate-800 font-bold max-w-[65%] truncate text-right">
            <!-- Custom slot components overrides -->
            <span v-if="i === 3" :style="{ color: color }" class="font-extrabold uppercase text-[10px] tracking-wider">
              {{ t.types[record.type] }}
            </span>
            <span v-else-if="i === 8 && !record.exitTime" class="bg-amber-50 text-amber-600 px-2 py-0.5 rounded text-[10px] font-bold">
              {{ t.stillInside }}
            </span>
            <span v-else>
              {{ fields[i] }}
            </span>
          </div>
        </div>
      </div>

      <!-- Close Button -->
      <button 
        class="w-full bg-slate-100 hover:bg-slate-200/80 border border-slate-200 text-slate-600 text-xs font-bold py-2.5 rounded-xl transition duration-150 active:scale-[0.98]"
        @click="emit('close')"
      >
        {{ t.close }}
      </button>
    </div>
  </div>
</template>
