<script setup>
import { ref, reactive, onMounted, onUnmounted, nextTick } from 'vue';

const props = defineProps({
  t: Object,
  departments: Array,
  purposes: Array,
  employees: Object,
  defaultContact: Object
});

const emit = defineEmits(['submit', 'close']);

const step = ref(1);
const form = reactive({
  name: "",
  company: "",
  type: "vendor",
  department: props.departments[0] || "",
  contact: props.defaultContact.name,
  purpose: props.purposes[0] || "",
  note: "",
  empId: "",
  empAvatar: ""
});

const photo = ref(null);
const errors = reactive({ name: "", company: "" });

// Employee Lookup States
const empError = ref("");
const empFound = ref(false);

function lookupEmployee() {
  const code = form.empId.toUpperCase().trim();
  const emp = props.employees[code];
  if (emp) {
    empFound.value = true;
    empError.value = "";
    form.name = emp.name;
    form.department = emp.dept;
    form.type = "staff";
    form.company = "內部員工";
    form.empAvatar = emp.avatar;
  } else {
    empFound.value = false;
    empError.value = props.t.lookupFail;
  }
}

function validate() {
  errors.name = !form.name.trim() ? props.t.required : "";
  errors.company = !form.company.trim() ? props.t.required : "";
  return !errors.name && !errors.company;
}

function nextStep() {
  if (validate()) step.value = 2;
}

function generateId() {
  return Date.now().toString(36).toUpperCase() + Math.random().toString(36).slice(2, 5).toUpperCase();
}

function handleSubmit() {
  emit('submit', {
    ...form,
    photo: photo.value || "",
    id: generateId(),
    entryTime: Date.now(),
    exitTime: null,
    exitPhoto: ""
  });
}

// ─── Camera Handler States ───────────────────────────────────────────────────
const videoRef = ref(null);
const canvasRef = ref(null);
const stream = ref(null);
const ready = ref(false);
const cameraError = ref("");
const facing = ref("user"); // "user" | "environment"
const switching = ref(false);
const hasMultipleCams = ref(false);
const showCam = ref(false);

async function startCamera(mode) {
  if (stream.value) {
    stream.value.getTracks().forEach(tr => tr.stop());
  }
  ready.value = false;
  switching.value = true;

  try {
    const s = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: mode }
    });
    stream.value = s;
    await nextTick();
    if (videoRef.value) {
      videoRef.value.srcObject = s;
      videoRef.value.onloadedmetadata = () => {
        ready.value = true;
        switching.value = false;
      };
    }
  } catch (e) {
    cameraError.value = props.t.camError;
    switching.value = false;
  }
}

function openCamera() {
  showCam.value = true;
  navigator.mediaDevices?.enumerateDevices().then(devices => {
    hasMultipleCams.value = devices.filter(d => d.kind === "videoinput").length > 1;
  });
  startCamera("user");
}

function closeCamera() {
  if (stream.value) {
    stream.value.getTracks().forEach(tr => tr.stop());
    stream.value = null;
  }
  showCam.value = false;
}

function flipCamera() {
  const next = facing.value === "user" ? "environment" : "user";
  facing.value = next;
  startCamera(next);
}

function shoot() {
  const v = videoRef.value;
  const c = canvasRef.value;
  c.width = v.videoWidth;
  c.height = v.videoHeight;
  const ctx = c.getContext("2d");

  if (facing.value === "user") {
    ctx.translate(c.width, 0);
    ctx.scale(-1, 1);
  }
  ctx.drawImage(v, 0, 0);
  photo.value = c.toDataURL("image/jpeg", 0.82);
  closeCamera();
}

onUnmounted(() => {
  if (stream.value) {
    stream.value.getTracks().forEach(tr => tr.stop());
  }
});

const VISITOR_TYPES = ["vendor", "brand", "audit", "staff"];
const TYPE_COLORS = { vendor: "#F97316", brand: "#22C55E", audit: "#EF4444", staff: "#3B82F6" };
</script>

<template>
  <div class="modal-backdrop">
    <!-- Camera Overlaid Layer -->
    <div class="camera-capture-layer" v-if="showCam">
      <div class="cam-container-box">
        <div class="cam-header">
          <span>{{ t.camLabel }}</span>
          <span class="cam-badge" :style="{ backgroundColor: facing === 'user' ? '#3B82F622' : '#f9731622', color: facing === 'user' ? '#93c5fd' : '#fb923c' }">
            {{ facing === 'user' ? t.camFront : t.camBack }}
          </span>
        </div>

        <div v-if="cameraError" class="cam-error-box">
          <div class="error-emoji">📷</div>
          <p>{{ cameraError }}</p>
          <div class="error-actions">
            <button class="btn-primary" @click="closeCamera">{{ t.skipPhoto }}</button>
            <button class="btn-secondary" @click="closeCamera">{{ t.cancel }}</button>
          </div>
        </div>

        <div v-else class="cam-preview-box">
          <div class="video-frame">
            <video ref="videoRef" autoplay playsinline muted
              :style="{ transform: facing === 'user' ? 'scaleX(-1)' : 'none', opacity: switching ? 0.3 : 1 }"></video>
            <div class="cam-corners" v-for="c in ['top-left','top-right','bottom-left','bottom-right']" :key="c" :class="c"></div>
            <div v-if="switching" class="cam-switching-overlay">🔄 {{ t.camSwitching }}</div>
          </div>
          <canvas ref="canvasRef" style="display: none;"></canvas>

          <div class="cam-controls">
            <button class="btn-secondary" @click="closeCamera">{{ t.skip }}</button>
            <button class="btn-shoot" :disabled="!ready || switching" @click="shoot">📸</button>
            <button class="btn-secondary btn-flip" v-if="hasMultipleCams" :disabled="switching" @click="flipCamera">🔄</button>
            <div class="btn-spacer" v-else></div>
          </div>

          <div class="cam-hint-text">
            {{ facing === 'user' ? t.camHintFront : t.camHintBack }}
          </div>
        </div>
      </div>
    </div>

    <!-- Main Registration Box -->
    <div class="modal-box" v-else>
      <div class="modal-header">
        <div>
          <h2>{{ t.regTitle }}</h2>
          <p class="subtitle">{{ t.step }} {{ step }} {{ t.of }} 2</p>
        </div>
        <button class="btn-close" @click="emit('close')">✕</button>
      </div>

      <div class="progress-bar-wrapper">
        <div class="progress-bar-segment" v-for="i in [0, 1]" :key="i" :class="{ active: step > i }"></div>
      </div>

      <!-- Step 1: Text Data Fields -->
      <div class="form-content" v-if="step === 1">
        <!-- Employee Lookup Section -->
        <div class="emp-lookup-box">
          <label>🔍 {{ t.empId }}</label>
          <div class="input-row">
            <input type="text" v-model="form.empId" placeholder="E001..." @keydown.enter="lookupEmployee" />
            <button class="btn-lookup" @click="lookupEmployee">{{ t.lookupBtn }}</button>
          </div>
          <p class="error-text" v-if="empError">⚠ {{ empError }}</p>
          <p class="success-text" v-if="empFound">✓ {{ form.name }} · {{ form.department }}</p>
        </div>

        <div class="form-group">
          <input type="text" v-model="form.name" :placeholder="t.name + ' *'" :class="{ error: errors.name }" />
          <p class="error-field" v-if="errors.name">{{ errors.name }}</p>
        </div>

        <div class="form-group">
          <input type="text" v-model="form.company" :placeholder="t.company + ' *'" :class="{ error: errors.company }" />
          <p class="error-field" v-if="errors.company">{{ errors.company }}</p>
        </div>

        <div class="form-group">
          <div class="badges-row">
            <button type="button" class="badge-selector" v-for="v in VISITOR_TYPES" :key="v" @click="form.type = v"
              :class="{ active: form.type === v }" :style="{ borderColor: form.type === v ? TYPE_COLORS[v] : '', color: form.type === v ? TYPE_COLORS[v] : '', backgroundColor: form.type === v ? TYPE_COLORS[v] + '11' : '' }">
              {{ t.types[v] }}
            </button>
          </div>
        </div>

        <div class="form-group">
          <select v-model="form.department" class="select-field">
            <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
          </select>
        </div>

        <div class="form-group prefilled-row">
          <div>
            <label>{{ t.contact }}</label>
            <span class="prefilled-val">{{ form.contact }}</span>
          </div>
          <span class="prefilled-icon">⚙ {{ t.settings }}</span>
        </div>

        <div class="form-group">
          <div class="badges-row">
            <button type="button" class="badge-selector-sm" v-for="p in purposes" :key="p" @click="form.purpose = p"
              :class="{ active: form.purpose === p }">
              {{ p }}
            </button>
          </div>
        </div>

        <div class="form-group">
          <textarea v-model="form.note" :placeholder="t.note" rows="2" class="textarea-field"></textarea>
        </div>

        <button class="btn-action-primary" @click="nextStep">{{ t.nextPhoto }}</button>
      </div>

      <!-- Step 2: Photo Capture -->
      <div class="form-content" v-if="step === 2">
        <div class="photo-viewer-box">
          <div class="photo-img-wrapper" v-if="photo">
            <img :src="photo" alt="entry snapshot" />
            <span class="success-badge">{{ t.photoTaken }}</span>
            <button class="btn-retake" @click="photo = null">{{ t.retake }}</button>
          </div>
          <div class="photo-trigger-box" v-else @click="openCamera">
            <span class="trigger-emoji">📷</span>
            <span class="trigger-title">{{ t.photoHint }}</span>
            <span class="trigger-sub">{{ t.photoSub }}</span>
          </div>
        </div>

        <div class="review-details">
          <div><span class="label">{{ t.name }}：</span>{{ form.name }}</div>
          <div><span class="label">{{ t.company }}：</span>{{ form.company }}</div>
          <div><span class="label">{{ t.purpose }}：</span>{{ form.purpose }}</div>
          <div><span class="label">{{ t.contact }}：</span>{{ form.contact }}</div>
          <div><span class="label">{{ t.dept }}：</span>{{ form.department }}</div>
        </div>

        <div class="btn-row">
          <button class="btn-secondary" @click="step = 1">{{ t.back }}</button>
          <button class="btn-action-success" @click="handleSubmit">{{ t.confirmEntry }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Modal Glassmorphic Styling */
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
  padding: 28px;
  width: 100%;
  max-width: 440px;
  color: #1a2d5a;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 50px rgba(10, 20, 60, 0.35);
  box-sizing: border-box;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
}

.modal-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #111827;
}

.modal-header .subtitle {
  margin: 4px 0 0 0;
  font-size: 11px;
  color: #5a7298;
}

.btn-close {
  background: none;
  border: none;
  color: #5a7298;
  font-size: 20px;
  cursor: pointer;
  padding: 0;
}

.progress-bar-wrapper {
  display: flex;
  gap: 8px;
  margin-bottom: 24px;
}

.progress-bar-segment {
  flex: 1;
  height: 4px;
  border-radius: 2px;
  background: #dde6f5;
  transition: all 0.25s;
}

.progress-bar-segment.active {
  background: #3b82f6;
}

/* Forms */
.form-group {
  margin-bottom: 14px;
}

.form-group input[type="text"] {
  width: 100%;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 10px;
  padding: 11px 14px;
  color: #111827;
  font-size: 14px;
  outline: none;
  box-sizing: border-box;
  transition: all 0.2s;
}

.form-group input[type="text"]:focus {
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.form-group input.error {
  border-color: #ef4444;
}

.error-field {
  color: #ef4444;
  font-size: 11px;
  margin: 4px 0 0 0;
}

.select-field {
  width: 100%;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 10px;
  padding: 11px 14px;
  color: #111827;
  font-size: 14px;
  outline: none;
  cursor: pointer;
  box-sizing: border-box;
}

.select-field:focus {
  border-color: #6366f1;
}

.textarea-field {
  width: 100%;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 10px;
  padding: 11px 14px;
  color: #111827;
  font-size: 14px;
  outline: none;
  resize: vertical;
  box-sizing: border-box;
}

.textarea-field:focus {
  border-color: #6366f1;
}

/* Badges */
.badges-row {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.badge-selector {
  padding: 6px 14px;
  border-radius: 20px;
  border: 1.5px solid #dde6f5;
  background: transparent;
  color: #5a7298;
  cursor: pointer;
  font-size: 12px;
  font-weight: 500;
  transition: all 0.2s;
}

.badge-selector.active {
  font-weight: 700;
}

.badge-selector-sm {
  padding: 5px 12px;
  border-radius: 14px;
  border: 1px solid #c8d8f0;
  background: transparent;
  color: #5a7298;
  cursor: pointer;
  font-size: 11px;
  transition: all 0.2s;
}

.badge-selector-sm.active {
  border-color: #6366f1;
  background: #f0f3ff;
  color: #3b82f6;
  font-weight: 600;
}

/* Prefilled Info Block */
.prefilled-row {
  background: #f0f6ff66;
  border: 1px solid #dde6f5;
  border-radius: 10px;
  padding: 10px 14px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.prefilled-row label {
  display: block;
  font-size: 10px;
  color: #5a7298;
  margin-bottom: 2px;
}

.prefilled-val {
  font-size: 14px;
  font-weight: 700;
  color: #1d4ed8;
}

.prefilled-icon {
  font-size: 11px;
  color: #8aaac8;
}

/* Emp ID Lookup */
.emp-lookup-box {
  margin-bottom: 16px;
  background: #f0f6ff44;
  border: 1px solid #dde6f5;
  border-radius: 12px;
  padding: 12px 14px;
}

.emp-lookup-box label {
  display: block;
  font-size: 12px;
  color: #5a7298;
  margin-bottom: 8px;
  font-weight: 600;
}

.input-row {
  display: flex;
  gap: 8px;
}

.emp-lookup-box input {
  flex: 1;
  background: #fff;
  border: 1px solid #c8d8f0;
  border-radius: 8px;
  padding: 9px 12px;
  font-size: 14px;
  color: #111827;
  outline: none;
}

.btn-lookup {
  background: #6366f1;
  border: none;
  color: #fff;
  border-radius: 8px;
  padding: 9px 16px;
  cursor: pointer;
  font-size: 12px;
  font-weight: 600;
}

.error-text {
  color: #ef4444;
  font-size: 11px;
  margin: 6px 0 0 0;
}

.success-text {
  color: #10b981;
  font-size: 11px;
  margin: 6px 0 0 0;
  font-weight: 600;
}

/* Buttons Actions */
.btn-action-primary {
  width: 100%;
  background: linear-gradient(135deg, #2563eb, #3b82f6);
  border: none;
  color: #fff;
  border-radius: 12px;
  padding: 13px 0;
  cursor: pointer;
  font-weight: 700;
  font-size: 14px;
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.25);
  margin-top: 10px;
}

.btn-row {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

.btn-secondary {
  flex: 1;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #4b5563;
  border-radius: 12px;
  padding: 12px 0;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
}

.btn-action-success {
  flex: 2;
  background: linear-gradient(135deg, #059669, #10b981);
  border: none;
  color: #fff;
  border-radius: 12px;
  padding: 12px 0;
  cursor: pointer;
  font-weight: 700;
  font-size: 14px;
}

/* Review Details */
.review-details {
  background: #f9fafb;
  border-radius: 12px;
  padding: 12px 16px;
  font-size: 13px;
  line-height: 1.8;
  color: #4b5563;
  border: 1px solid #dde6f5;
  margin-bottom: 20px;
}

.review-details .label {
  color: #6b7280;
}

/* Photo Box */
.photo-viewer-box {
  margin-bottom: 20px;
}

.photo-trigger-box {
  border: 2px dashed #b8cce8;
  border-radius: 12px;
  height: 150px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #5a7298;
  transition: all 0.2s;
}

.photo-trigger-box:hover {
  border-color: #6366f1;
  background: #f5f8ff;
  color: #3b82f6;
}

.trigger-emoji {
  font-size: 40px;
}

.trigger-title {
  font-size: 13px;
  font-weight: 600;
  margin-top: 8px;
}

.trigger-sub {
  font-size: 11px;
  color: #8aaac8;
  margin-top: 4px;
}

.photo-img-wrapper {
  position: relative;
  border: 2px solid #10b98122;
  border-radius: 12px;
  overflow: hidden;
  max-height: 220px;
}

.photo-img-wrapper img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.success-badge {
  position: absolute;
  top: 8px;
  left: 8px;
  background: #10b981;
  color: #fff;
  font-size: 10px;
  padding: 3px 8px;
  border-radius: 20px;
  font-weight: 700;
}

.btn-retake {
  position: absolute;
  top: 8px;
  right: 8px;
  background: rgba(0,0,0,0.6);
  border: none;
  color: #fff;
  border-radius: 6px;
  padding: 3px 10px;
  cursor: pointer;
  font-size: 11px;
  transition: all 0.2s;
}
.btn-retake:hover {
  background: rgba(0,0,0,0.8);
}

/* ─── Hardware Camera Layer ────────────────────────────────────────────────── */
.camera-capture-layer {
  position: fixed;
  inset: 0;
  background: rgba(8, 15, 40, 0.95);
  z-index: 1100;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.cam-container-box {
  width: 330px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.cam-header {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  color: #8aaac8;
  font-size: 12px;
}

.cam-badge {
  font-size: 10px;
  padding: 3px 8px;
  border-radius: 20px;
  font-weight: 700;
}

.cam-error-box {
  background: #fff;
  border-radius: 16px;
  padding: 24px;
  text-align: center;
  box-shadow: 0 8px 30px rgba(0,0,0,0.3);
}

.error-emoji {
  font-size: 36px;
  margin-bottom: 10px;
}

.cam-error-box p {
  font-size: 13px;
  line-height: 1.6;
  color: #374151;
  margin: 0 0 20px 0;
}

.error-actions {
  display: flex;
  gap: 10px;
  justify-content: center;
}

.cam-preview-box {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.video-frame {
  position: relative;
  width: 320px;
  height: 240px;
  border-radius: 16px;
  overflow: hidden;
  border: 2px solid #5a7298;
  background: #111827;
  box-shadow: 0 10px 24px rgba(0,0,0,0.4);
}

.video-frame video {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: opacity 0.25s;
}

.cam-corners {
  position: absolute;
  width: 20px;
  height: 20px;
  border: 2.5px solid #6366f1;
}

.cam-corners.top-left { top: 12px; left: 12px; border-bottom: none; border-right: none; }
.cam-corners.top-right { top: 12px; right: 12px; border-bottom: none; border-left: none; }
.cam-corners.bottom-left { bottom: 12px; left: 12px; border-top: none; border-right: none; }
.cam-corners.bottom-right { bottom: 12px; right: 12px; border-top: none; border-left: none; }

.cam-switching-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 13px;
  background: rgba(8,15,40,0.5);
  backdrop-filter: blur(2px);
}

.cam-controls {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 24px;
  width: 100%;
  justify-content: space-between;
}

.btn-shoot {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: linear-gradient(135deg, #10b981, #059669);
  border: 3px solid #10b98177;
  color: #fff;
  font-size: 26px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 0 20px rgba(16, 185, 129, 0.4);
  transition: all 0.2s;
  padding: 0;
}

.btn-shoot:disabled {
  background: #374151;
  border-color: #4b5563;
  cursor: not-allowed;
  box-shadow: none;
}

.btn-flip {
  font-size: 18px;
}

.btn-spacer {
  width: 48px;
}

.cam-hint-text {
  margin-top: 14px;
  font-size: 11px;
  color: #8aaac8;
}
</style>
