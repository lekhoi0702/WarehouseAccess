<script setup>
import { ref, computed, nextTick } from 'vue';
import { useCamera } from '../../composables/useCamera';

const props = defineProps({
  t: Object,
  record: Object
});

const emit = defineEmits(['confirm', 'close']);

const photo = ref(null);

const {
  ready: camReady,
  cameraError: camErrKey,
  facing: camFacing,
  switching: camSwitching,
  hasMultipleCams,
  showCam,
  openCamera,
  closeCamera,
  flipCamera,
  capturePhoto
} = useCamera();

const videoRef = ref(null);
const canvasRef = ref(null);

const camError = computed(() => {
  if (camErrKey.value === 'camera_error') {
    return props.t.camError || "Camera unavailable. Check permissions.";
  }
  return "";
});

function openCameraFlow() {
  openCamera(null); // Bind nextTick
  nextTick(() => {
    openCamera(videoRef.value);
  });
}

function shoot() {
  const cap = capturePhoto(videoRef.value, canvasRef.value);
  if (cap) {
    photo.value = cap;
  }
}
</script>

<template>
  <div class="fixed inset-0 bg-[#0a142d]/65 backdrop-blur-md z-[8000] flex items-center justify-center p-4">
    <!-- Camera Overlay Layer -->
    <div class="fixed inset-0 bg-[#080f28]/95 z-[8100] flex items-center justify-center p-4" v-if="showCam">
      <div class="w-full max-w-sm flex flex-col items-center">
        <div class="w-full flex justify-between items-center mb-3 text-slate-400 text-xs">
          <span>{{ t.camLabel }}</span>
          <span 
            class="px-2 py-0.5 rounded-full text-[10px] font-bold"
            :class="camFacing === 'user' ? 'bg-blue-500/20 text-blue-300' : 'bg-orange-500/20 text-orange-300'"
          >
            {{ camFacing === 'user' ? t.camFront : t.camBack }}
          </span>
        </div>

        <div class="w-full aspect-[4/3] rounded-2xl overflow-hidden border border-slate-700/80 bg-slate-900 shadow-xl relative flex items-center justify-center">
          <div v-if="camError" class="p-6 text-center text-red-300">
            <span class="text-3xl">📷</span>
            <p class="text-xs leading-relaxed font-semibold mt-2">{{ camError }}</p>
            <div class="flex gap-2 justify-center mt-4">
              <button class="bg-white/10 hover:bg-white/15 border border-white/10 text-white text-xs font-bold px-3.5 py-1.5 rounded-lg transition" @click="closeCamera">{{ t.skipPhoto }}</button>
              <button class="bg-slate-800 border border-slate-700 text-slate-300 text-xs font-bold px-3.5 py-1.5 rounded-lg transition" @click="closeCamera">{{ t.cancel }}</button>
            </div>
          </div>

          <div v-else class="w-full h-full relative">
            <video 
              ref="videoRef" 
              autoplay 
              playsinline 
              muted
              class="w-full h-full object-cover transition-opacity duration-300"
              :class="[
                camFacing === 'user' ? 'scale-x-[-1]' : '',
                camSwitching ? 'opacity-30' : 'opacity-100'
              ]"
            ></video>
            <!-- Overlay corners -->
            <div class="absolute w-4 h-4 border-2 border-indigo-500 top-4 left-4 border-r-0 border-b-0"></div>
            <div class="absolute w-4 h-4 border-2 border-indigo-500 top-4 right-4 border-l-0 border-b-0"></div>
            <div class="absolute w-4 h-4 border-2 border-indigo-500 bottom-4 left-4 border-r-0 border-t-0"></div>
            <div class="absolute w-4 h-4 border-2 border-indigo-500 bottom-4 right-4 border-l-0 border-t-0"></div>
            
            <div v-if="camSwitching" class="absolute inset-0 bg-black/40 flex items-center justify-center text-white text-xs">
              🔄 {{ t.camSwitching }}
            </div>
          </div>
          <canvas ref="canvasRef" style="display: none;"></canvas>
        </div>

        <div class="flex items-center justify-between w-full mt-6" v-if="!camError">
          <button class="bg-white/5 border border-white/10 text-white text-xs font-semibold px-4 py-2 rounded-xl active:scale-95 transition" @click="closeCamera">{{ t.skip }}</button>
          
          <button 
            class="w-14 h-14 rounded-full bg-gradient-to-r from-emerald-500 to-emerald-600 border-4 border-emerald-500/20 text-white text-lg flex items-center justify-center shadow-lg active:scale-90 transition disabled:opacity-40"
            :disabled="camSwitching" 
            @click="shoot"
          >
            📸
          </button>

          <button 
            v-if="hasMultipleCams" 
            class="bg-white/5 border border-white/10 text-white p-2 rounded-xl active:scale-95 transition"
            :disabled="camSwitching"
            @click="flipCamera(videoRef)"
          >
            🔄
          </button>
          <div class="w-10" v-else></div>
        </div>

        <div class="text-[10px] text-slate-500 font-semibold text-center mt-3" v-if="!camError">
          {{ camFacing === 'user' ? t.camHintFront : t.camHintBack }}
        </div>
      </div>
    </div>

    <!-- Main Dialog -->
    <div class="bg-white border border-slate-200 rounded-3xl p-7 w-full max-w-sm text-slate-800 shadow-2xl relative" v-else>
      <h2 class="text-lg font-extrabold text-slate-900 m-0">{{ t.checkoutTitle }}</h2>
      <p class="text-sm font-semibold text-slate-500 mt-1 mb-6">{{ record.name }}（{{ record.company }}）</p>

      <!-- Optional exit photo slot -->
      <div class="relative w-full aspect-[4/3] bg-slate-50 border border-slate-200 rounded-2xl overflow-hidden flex items-center justify-center shadow-inner mb-6">
        <div class="w-full h-full relative" v-if="photo">
          <img :src="photo" alt="exit snapshot" class="w-full h-full object-cover" />
          <button 
            class="absolute top-3 right-3 bg-black/60 hover:bg-black/80 text-white text-xs font-bold px-3 py-1 rounded-lg transition" 
            @click="photo = null"
          >
            {{ t.retake }}
          </button>
        </div>
        <div 
          class="w-full h-full flex flex-col items-center justify-center cursor-pointer hover:bg-indigo-50/20 group transition duration-200" 
          v-else 
          @click="openCameraFlow"
        >
          <span class="text-3xl group-hover:scale-110 transition duration-200">📷</span>
          <span class="text-xs font-bold text-slate-500 mt-2">{{ t.exitPhotoHint }}</span>
        </div>
      </div>

      <!-- Action row -->
      <div class="flex gap-3">
        <button 
          class="flex-1 bg-slate-100 hover:bg-slate-200/80 border border-slate-200 text-slate-600 text-xs font-bold py-3 rounded-xl active:scale-[0.98] transition"
          @click="emit('close')"
        >
          {{ t.cancel }}
        </button>
        
        <button 
          class="flex-1 bg-gradient-to-r from-red-500 to-red-600 text-white text-xs font-bold py-3 rounded-xl shadow-lg hover:shadow-red-500/20 active:scale-[0.98] transition"
          @click="emit('confirm', photo)"
        >
          {{ t.confirmCheckout }}
        </button>
      </div>
    </div>
  </div>
</template>
