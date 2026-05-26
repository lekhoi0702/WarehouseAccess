<script setup>
import { ref, onUnmounted, nextTick } from 'vue';

const props = defineProps({
  t: Object,
  record: Object
});

const emit = defineEmits(['confirm', 'close']);

const photo = ref(null);
const showCam = ref(false);

// Camera States
const videoRef = ref(null);
const canvasRef = ref(null);
const stream = ref(null);
const ready = ref(false);
const cameraError = ref("");
const facing = ref("user");
const switching = ref(false);

async function startCamera(mode) {
  if (stream.value) {
    stream.value.getTracks().forEach(tr => tr.stop());
  }
  ready.value = false;
  switching.value = true;

  try {
    const s = await navigator.mediaDevices.getUserMedia({ video: { facingMode: mode } });
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
  startCamera("user");
}

function closeCamera() {
  if (stream.value) {
    stream.value.getTracks().forEach(tr => tr.stop());
    stream.value = null;
  }
  showCam.value = false;
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
</script>

<template>
  <div class="modal-backdrop">
    <!-- Camera Overlay Layer -->
    <div class="camera-capture-layer" v-if="showCam">
      <div class="cam-container-box">
        <div class="cam-header">
          <span>{{ t.camLabel }}</span>
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
          </div>
          <canvas ref="canvasRef" style="display: none;"></canvas>

          <div class="cam-controls">
            <button class="btn-secondary" @click="closeCamera">{{ t.skip }}</button>
            <button class="btn-shoot" :disabled="!ready || switching" @click="shoot">📸</button>
            <div class="btn-spacer"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Main Checkout Box -->
    <div class="checkout-modal-box" v-else>
      <h2>{{ t.checkoutTitle }}</h2>
      <p class="visitor-name">{{ record.name }}（{{ record.company }}）</p>

      <div class="photo-viewer-box">
        <div class="photo-img-wrapper" v-if="photo">
          <img :src="photo" alt="exit snapshot" />
          <button class="btn-retake" @click="photo = null">{{ t.retake }}</button>
        </div>
        <div class="photo-trigger-box" v-else @click="openCamera">
          <span class="trigger-emoji">📷</span>
          <span class="trigger-title">{{ t.exitPhotoHint }}</span>
        </div>
      </div>

      <div class="btn-row">
        <button class="btn-checkout-action" @click="emit('confirm', photo)">{{ t.confirmCheckout }}</button>
        <button class="btn-cancel-action" @click="emit('close')">{{ t.cancel }}</button>
      </div>
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
}

.checkout-modal-box {
  background: #ffffff;
  border: 1px solid #c8d8f0;
  border-radius: 20px;
  padding: 28px;
  width: 360px;
  color: #1a2d5a;
  box-shadow: 0 15px 40px rgba(10, 20, 60, 0.3);
  box-sizing: border-box;
}

.checkout-modal-box h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #111827;
}

.visitor-name {
  margin: 4px 0 20px 0;
  font-size: 13px;
  color: #4b5563;
  font-weight: 500;
}

.photo-viewer-box {
  margin-bottom: 20px;
}

.photo-trigger-box {
  border: 2px dashed #b8cce8;
  border-radius: 12px;
  height: 110px;
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
  font-size: 28px;
}

.trigger-title {
  font-size: 12px;
  font-weight: 600;
  margin-top: 6px;
}

.photo-img-wrapper {
  position: relative;
  border: 1px solid #dde6f5;
  border-radius: 12px;
  overflow: hidden;
  max-height: 160px;
}

.photo-img-wrapper img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.btn-retake {
  position: absolute;
  top: 6px;
  right: 6px;
  background: rgba(0, 0, 0, 0.6);
  border: none;
  color: #fff;
  border-radius: 6px;
  padding: 3px 8px;
  cursor: pointer;
  font-size: 11px;
}

.btn-row {
  display: flex;
  gap: 10px;
}

.btn-checkout-action {
  flex: 1;
  background: linear-gradient(135deg, #dc2626, #ef4444);
  border: none;
  color: #fff;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 700;
  font-size: 13px;
  box-shadow: 0 4px 10px rgba(220, 38, 38, 0.2);
}

.btn-cancel-action {
  flex: 1;
  background: #f3f4f6;
  border: 1px solid #d1d5db;
  color: #4b5563;
  border-radius: 10px;
  padding: 11px 0;
  cursor: pointer;
  font-weight: 600;
  font-size: 13px;
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

.btn-spacer {
  width: 48px;
}
</style>
