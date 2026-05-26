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
  null, // Special custom template for Type
  props.record.department,
  props.record.contact,
  props.record.purpose,
  formatTime(props.record.entryTime),
  props.record.exitTime ? formatTime(props.record.exitTime) : null, // Special template for ExitTime
  props.record.exitTime ? formatDuration(props.record.exitTime - props.record.entryTime) : "—",
  props.record.note || "—",
]);
</script>

<template>
  <div class="modal-backdrop">
    <div class="modal-box">
      <div class="modal-header">
        <h2>{{ t.detailTitle }}</h2>
        <button class="btn-close" @click="emit('close')">✕</button>
      </div>

      <!-- Entry and Exit Photos Grid -->
      <div class="photos-grid">
        <div class="photo-container">
          <label>{{ t.entryPhoto }}</label>
          <img v-if="record.photo" :src="record.photo" :alt="t.entryPhoto" class="photo-preview" />
          <div v-else class="photo-empty">{{ t.noEntryPhoto }}</div>
        </div>
        <div class="photo-container" v-if="record.exitTime || record.exitPhoto">
          <label>{{ t.exitPhoto }}</label>
          <img v-if="record.exitPhoto" :src="record.exitPhoto" :alt="t.exitPhoto" class="photo-preview" />
          <div v-else class="photo-empty">{{ t.noExitPhoto }}</div>
        </div>
      </div>

      <!-- Profile Fields List -->
      <div class="fields-list">
        <div class="field-item" v-for="(k, i) in t.detailFields" :key="k">
          <span class="field-label">{{ k }}</span>
          <span class="field-value">
            <!-- Custom overrides for complex UI structures -->
            <span v-if="i === 3" :style="{ color: color }" class="type-badge-text">
              {{ t.types[record.type] }}
            </span>
            <span v-else-if="i === 8 && !record.exitTime" class="on-site-badge">
              {{ t.stillInside }}
            </span>
            <span v-else>
              {{ fields[i] }}
            </span>
          </span>
        </div>
      </div>

      <button class="btn-close-action" @click="emit('close')">{{ t.close }}</button>
    </div>
  </div>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(10, 20, 45, 0.65);
  backdrop-filter: blur(8px);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
}

.modal-box {
  background: #ffffff;
  border: 1px solid #c8d8f0;
  border-radius: 24px;
  padding: 24px;
  width: 100%;
  max-width: 420px;
  color: #1a2d5a;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 50px rgba(10, 20, 60, 0.35);
  box-sizing: border-box;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.modal-header h2 {
  margin: 0;
  font-size: 17px;
  font-weight: 800;
  color: #111827;
}

.btn-close {
  background: none;
  border: none;
  color: #5a7298;
  font-size: 20px;
  cursor: pointer;
  padding: 0;
}

.photos-grid {
  display: flex;
  gap: 12px;
  margin-bottom: 18px;
}

.photo-container {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.photo-container label {
  font-size: 11px;
  color: #5a7298;
  margin-bottom: 4px;
  font-weight: 600;
}

.photo-preview {
  width: 100%;
  border-radius: 10px;
  object-fit: cover;
  height: 125px;
  border: 1px solid #dde6f5;
  box-shadow: 0 4px 10px rgba(0,0,0,0.05);
}

.photo-empty {
  background: #f9fafb;
  border: 1px solid #dde6f5;
  border-radius: 10px;
  height: 125px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8aaac8;
  font-size: 11px;
}

.fields-list {
  display: flex;
  flex-direction: column;
}

.field-item {
  display: flex;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid #f0f6ff;
  font-size: 13px;
  align-items: center;
}

.field-label {
  color: #5a7298;
}

.field-value {
  color: #1a2d5a;
  text-align: right;
  max-width: 60%;
  font-weight: 500;
}

.type-badge-text {
  font-weight: 700;
}

.on-site-badge {
  background: #f59e0b22;
  color: #d97706;
  font-size: 11px;
  padding: 2px 8px;
  border-radius: 6px;
  font-weight: 700;
}

.btn-close-action {
  width: 100%;
  margin-top: 20px;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #4b5563;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
  transition: all 0.2s;
}
.btn-close-action:hover {
  background: #e5e7eb;
}
</style>
