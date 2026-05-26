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
</script>

<template>
  <div class="modal-backdrop">
    <div class="modal-box">
      <!-- Header -->
      <div class="modal-header">
        <div class="header-title">
          <span class="header-emoji">📅</span>
          <h2>{{ t.statsToday }}明細</h2>
          <span class="count-tag">{{ todayRecords.length }} 人次</span>
        </div>
        <button class="btn-close" @click="emit('close')">✕</button>
      </div>

      <!-- Visitor Flow List -->
      <div class="flow-list">
        <div v-if="todayRecords.length === 0" class="list-empty">
          今日尚無訪客登記紀錄
        </div>
        
        <div v-else v-for="r in todayRecords" :key="r.id" class="flow-item">
          <div class="item-left">
            <div class="avatar-circle">
              <img v-if="r.type === 'staff' && r.empAvatar" :src="r.empAvatar" alt="staff" class="avatar-staff" />
              <img v-else-if="r.photo" :src="r.photo" alt="visitor" class="avatar-visitor" />
              <span v-else class="avatar-placeholder">👤</span>
            </div>
            <div class="visitor-info">
              <div class="name-row">
                <h4>{{ r.name }}</h4>
                <span class="badge" :style="{ backgroundColor: TYPE_COLORS[r.type] + '1a', color: TYPE_COLORS[r.type] }">
                  {{ t.types[r.type] }}
                </span>
              </div>
              <p class="company-dept">{{ r.company }} · {{ r.department }}</p>
            </div>
          </div>

          <div class="item-right">
            <div class="time-label">進：<strong>{{ formatTimeOnly(r.entryTime) }}</strong></div>
            <div class="status-label">
              <span v-if="r.exitTime" class="time-exit">
                出：{{ formatTimeOnly(r.exitTime) }}
              </span>
              <span v-else class="status-active">
                在場 ⏱
              </span>
            </div>
          </div>
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
  max-width: 480px;
  color: #1a2d5a;
  max-height: 85vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 50px rgba(10, 20, 60, 0.35);
  box-sizing: border-box;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 18px;
  flex-shrink: 0;
}

.header-title {
  display: flex;
  align-items: center;
  gap: 8px;
}

.header-emoji {
  font-size: 20px;
}

.header-title h2 {
  margin: 0;
  font-size: 17px;
  font-weight: 850;
  color: #111827;
}

.count-tag {
  font-size: 11px;
  background: rgba(99, 102, 241, 0.1);
  color: #4f46e5;
  padding: 2px 10px;
  border-radius: 12px;
  font-weight: 700;
}

.btn-close {
  background: none;
  border: none;
  color: #5a7298;
  font-size: 20px;
  cursor: pointer;
  padding: 0;
}

.flow-list {
  overflow-y: auto;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-right: 4px;
  margin-bottom: 10px;
}

.list-empty {
  text-align: center;
  color: #8aaac8;
  padding: 48px 0;
  font-size: 13px;
}

.flow-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  border: 1px solid #dde6f5;
  border-radius: 14px;
  background: #f8fafd;
  box-sizing: border-box;
}

.item-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar-circle {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  overflow: hidden;
  background: #e8f0fb;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #dde6f5;
}

.avatar-staff {
  width: 80%;
  height: 80%;
  object-fit: contain;
}

.avatar-visitor {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-placeholder {
  font-size: 14px;
}

.visitor-info h4 {
  margin: 0;
  font-size: 13px;
  font-weight: 700;
  color: #111827;
  display: flex;
  align-items: center;
  gap: 6px;
}

.badge {
  font-size: 9px;
  padding: 1px 6px;
  border-radius: 6px;
  font-weight: 600;
}

.company-dept {
  margin: 3px 0 0 0;
  font-size: 11px;
  color: #6b7280;
}

.item-right {
  text-align: right;
  font-size: 11px;
}

.time-label {
  color: #374151;
}

.status-active {
  color: #d97706;
  font-weight: 700;
  display: inline-block;
  margin-top: 2px;
}

.time-exit {
  color: #9ca3af;
  display: inline-block;
  margin-top: 2px;
}

.btn-close-action {
  width: 100%;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #4b5563;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
  flex-shrink: 0;
  transition: all 0.2s;
}
.btn-close-action:hover {
  background: #e5e7eb;
}
</style>
