<script setup>
import { ref, reactive, computed, nextTick, onMounted } from 'vue'
import { useCamera } from '../../composables/useCamera'
import { useAudio } from '../../composables/useAudio'
import { useToast } from '../../composables/useToast'
import { useI18n } from '../../composables/useI18n'
import { useRecords } from '../../composables/useRecords'

const emit = defineEmits(['close', 'success'])

const { t } = useI18n()
const { playBeep } = useAudio()
const { showToast } = useToast()
const {
  lookupCard,
  submitCheckIn,
  contactDeptItems,
  purposeItems
} = useRecords()

const {
  stream,
  ready: camReady,
  cameraError,
  facing,
  switching,
  hasMultipleCams,
  showCam,
  openCamera,
  closeCamera,
  flipCamera,
  capturePhoto
} = useCamera()

const step = ref(1)
const checkInCardNumber = ref('')
const cardInputRef = ref(null)
const lookupLoading = ref(false)
const lookupMessage = ref('')
const hasCardLookupResult = ref(false)
const submitLoading = ref(false)

const formState = reactive({
  cardNumber: '',
  userCode: '',
  fullName: '',
  deptCode: '',
  deptName: '',
  contactDept: '',
  purpose: '',
  photo: ''
})

const errors = reactive({
  userCode: '',
  fullName: ''
})

const videoRef = ref(null)
const canvasRef = ref(null)

onMounted(() => {
  // Focus card input on open
  nextTick(() => {
    cardInputRef.value?.focus()
  })
})

const scannerState = computed(() => {
  if (lookupLoading.value) return 'detecting'
  if (hasCardLookupResult.value) return 'found'
  return 'waiting'
})

async function handleCardLookup() {
  const cardNum = checkInCardNumber.value.trim()
  if (!cardNum) {
    showToast('Please enter a card number', 'warning')
    return
  }
  lookupLoading.value = true
  lookupMessage.value = ''
  hasCardLookupResult.value = false

  try {
    const res = await lookupCard(cardNum)
    if (res?.success && res.data) {
      playBeep(true)
      hasCardLookupResult.value = true
      formState.cardNumber = res.data.cardNumber || cardNum
      formState.userCode = res.data.userCode || cardNum
      formState.fullName = res.data.fullName || ''
      formState.deptCode = res.data.deptCode || ''
      formState.deptName = res.data.deptName || ''
      lookupMessage.value = 'Card verified. Employee loaded.'
      
      // Auto-prefill contact department and purpose if available
      if (contactDeptItems.value.length > 0 && !formState.contactDept) {
        formState.contactDept = contactDeptItems.value[0].contactDeptName
      }
      if (purposeItems.value.length > 0 && !formState.purpose) {
        formState.purpose = purposeItems.value[0].purposeName
      }
    } else {
      playBeep(false)
      lookupMessage.value = res?.message || 'Card number not found.'
      showToast(lookupMessage.value, 'error')
    }
  } catch (err) {
    playBeep(false)
    lookupMessage.value = 'System connection error.'
    showToast(lookupMessage.value, 'error')
  } finally {
    lookupLoading.value = false
  }
}

function validateStepOne() {
  if (!hasCardLookupResult.value) {
    lookupMessage.value = 'Please scan/check card first.'
    return false
  }
  errors.userCode = formState.userCode.trim() ? '' : 'User Code is required'
  errors.fullName = formState.fullName.trim() ? '' : 'Full Name is required'
  return !errors.userCode && !errors.fullName
}

function nextStep() {
  if (validateStepOne()) {
    step.value = 2
  }
}

function prevStep() {
  if (showCam.value) {
    closeCamera()
  }
  step.value = 1
}

function triggerCamera() {
  openCamera(null)
  nextTick(() => {
    openCamera(videoRef.value)
  })
}

function triggerCapture() {
  const photoBase64Url = capturePhoto(videoRef.value, canvasRef.value)
  if (photoBase64Url) {
    playBeep(true)
    // Extract base64 part
    const marker = 'base64,'
    const index = photoBase64Url.indexOf(marker)
    formState.photo = index >= 0 ? photoBase64Url.substring(index + marker.length) : photoBase64Url
    showToast('Photo captured successfully', 'success')
  } else {
    showToast('Capture failed. Please try again.', 'error')
  }
}

async function handleCheckInSubmit() {
  if (!validateStepOne()) {
    step.value = 1
    return
  }
  submitLoading.value = true
  try {
    const payload = {
      cardNumber: formState.cardNumber ? formState.cardNumber.trim() : null,
      userCode: formState.userCode.trim(),
      fullName: formState.fullName.trim(),
      deptCode: formState.deptCode ? formState.deptCode.trim() : null,
      contactDept: formState.contactDept ? formState.contactDept.trim() : null,
      purpose: formState.purpose ? formState.purpose.trim() : null,
      photo: formState.photo || null
    }

    const res = await submitCheckIn(payload)
    if (res?.success) {
      playBeep(true)
      showToast(`${formState.fullName} checked in successfully!`, 'success')
      emit('success')
      emit('close')
    } else {
      playBeep(false)
      showToast(res?.message || 'CheckIn submission failed.', 'error')
    }
  } catch (err) {
    playBeep(false)
    showToast('System error submitting check-in.', 'error')
  } finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-md z-[80] flex items-center justify-center p-4">
    <!-- Main Modal Card -->
    <div class="bg-white rounded-3xl w-full max-w-lg shadow-2xl overflow-hidden border border-slate-100 flex flex-col max-h-[90vh]">
      <!-- Header Banner -->
      <div class="bg-gradient-to-r from-[#0a3575] to-[#0e4391] px-6 py-5 text-white flex justify-between items-center relative">
        <div>
          <h2 class="text-xl font-bold tracking-tight">Check-In Terminal</h2>
          <p class="text-xs text-blue-200/90 mt-1">Finished Goods Warehouse Access</p>
        </div>
        <div class="flex items-center gap-3">
          <div class="flex bg-black/20 rounded-lg p-0.5 text-xs font-semibold">
            <span :class="['px-2.5 py-1 rounded-md transition-all', step === 1 ? 'bg-white text-[#0e4391] shadow' : 'text-blue-100']">1. Card</span>
            <span :class="['px-2.5 py-1 rounded-md transition-all', step === 2 ? 'bg-white text-[#0e4391] shadow' : 'text-blue-100']">2. Face</span>
          </div>
          <button @click="emit('close')" class="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 text-white flex items-center justify-center transition">
            ✕
          </button>
        </div>
      </div>

      <!-- Scrollable Form Container -->
      <div class="p-6 flex-1 overflow-y-auto space-y-5">
        
        <!-- STEP 1: CARD LOOKUP -->
        <div v-if="step === 1" class="space-y-5">
          <!-- Card Sensor Simulator Graphic -->
          <div class="bg-slate-50 border border-slate-100 rounded-2xl p-5 flex flex-col items-center justify-center text-center relative overflow-hidden group">
            <div 
              class="w-16 h-16 rounded-full flex items-center justify-center text-2xl transition-all duration-500 relative z-10"
              :class="[
                scannerState === 'waiting' ? 'bg-blue-50 text-blue-600 animate-pulse' : '',
                scannerState === 'detecting' ? 'bg-amber-50 text-amber-600' : '',
                scannerState === 'found' ? 'bg-emerald-50 text-emerald-600 scale-110 shadow-lg shadow-emerald-500/10' : ''
              ]"
            >
              <span v-if="scannerState === 'waiting'">💳</span>
              <span v-else-if="scannerState === 'detecting'" class="animate-spin">🔄</span>
              <span v-else>✓</span>

              <!-- Radar rings -->
              <span v-if="scannerState === 'waiting'" class="absolute inset-0 rounded-full border border-blue-400/30 animate-ping opacity-75"></span>
            </div>
            
            <div class="mt-3 space-y-1 relative z-10">
              <h4 class="font-bold text-slate-800 text-sm">
                <span v-if="scannerState === 'waiting'">Place card on scanner</span>
                <span v-else-if="scannerState === 'detecting'">Reading card data...</span>
                <span v-else class="text-emerald-600">Card verification success</span>
              </h4>
              <p class="text-xs text-slate-400 max-w-[280px]">
                Tap employee IC card or manually input card number to fetch information.
              </p>
            </div>
            
            <!-- Laser scanning line -->
            <div v-if="scannerState === 'detecting'" class="absolute left-0 right-0 h-0.5 bg-gradient-to-r from-transparent via-amber-500 to-transparent animate-sweep top-0"></div>
          </div>

          <!-- Card Input Field -->
          <div class="space-y-2">
            <label class="text-xs font-bold text-slate-500 uppercase tracking-wide">Card Identification</label>
            <div class="flex gap-2">
              <input
                ref="cardInputRef"
                v-model="checkInCardNumber"
                type="text"
                placeholder="Enter or scan card number..."
                class="flex-1 px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:ring-2 focus:ring-blue-500/20 focus:border-[#0e4391] outline-none transition"
                :disabled="lookupLoading"
                @keydown.enter.prevent="handleCardLookup"
              />
              <button
                @click="handleCardLookup"
                :disabled="lookupLoading"
                class="bg-slate-800 hover:bg-slate-900 text-white text-xs font-semibold px-5 rounded-xl transition duration-200 active:scale-95 disabled:opacity-50"
              >
                {{ lookupLoading ? 'Checking...' : 'Verify' }}
              </button>
            </div>
            <p v-if="lookupMessage" class="text-xs font-medium" :class="hasCardLookupResult ? 'text-emerald-600' : 'text-rose-500'">
              {{ lookupMessage }}
            </p>
          </div>

          <!-- Employee Verification Info Block -->
          <div v-if="hasCardLookupResult" class="bg-blue-50/50 border border-blue-100 rounded-2xl p-4 space-y-3">
            <h3 class="text-xs font-bold text-[#0a3575] uppercase tracking-wider flex items-center gap-1.5">
              <span>👤</span> Verified Employee Profile
            </h3>
            
            <div class="grid grid-cols-2 gap-3 text-xs">
              <div class="bg-white/80 p-2.5 rounded-xl border border-slate-100">
                <span class="text-slate-400 block mb-0.5">Full Name</span>
                <span class="font-bold text-slate-700">{{ formState.fullName }}</span>
              </div>
              <div class="bg-white/80 p-2.5 rounded-xl border border-slate-100">
                <span class="text-slate-400 block mb-0.5">User Code</span>
                <span class="font-bold text-slate-700">{{ formState.userCode }}</span>
              </div>
              <div class="bg-white/80 p-2.5 rounded-xl border border-slate-100">
                <span class="text-slate-400 block mb-0.5">Department Code</span>
                <span class="font-bold text-slate-700">{{ formState.deptCode || '-' }}</span>
              </div>
              <div class="bg-white/80 p-2.5 rounded-xl border border-slate-100">
                <span class="text-slate-400 block mb-0.5">Department Name</span>
                <span class="font-bold text-slate-700">{{ formState.deptName || '-' }}</span>
              </div>
            </div>
          </div>

          <!-- Contact Dept & Purpose fields (Visit details) -->
          <div v-if="hasCardLookupResult" class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <!-- Contact Dept Selection -->
            <div class="space-y-2">
              <label class="text-xs font-bold text-slate-500 uppercase tracking-wide">Contact Department</label>
              <select
                v-model="formState.contactDept"
                class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-sm cursor-pointer focus:ring-2 focus:ring-blue-500/20 focus:border-[#0e4391] outline-none transition"
              >
                <option value="">Select Department...</option>
                <option 
                  v-for="item in contactDeptItems" 
                  :key="item.contactDeptId" 
                  :value="item.contactDeptName"
                >
                  {{ item.contactDeptName }}
                </option>
              </select>
            </div>

            <!-- Purpose Selection -->
            <div class="space-y-2">
              <label class="text-xs font-bold text-slate-500 uppercase tracking-wide">Access Purpose</label>
              <select
                v-model="formState.purpose"
                class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-sm cursor-pointer focus:ring-2 focus:ring-blue-500/20 focus:border-[#0e4391] outline-none transition"
              >
                <option value="">Select Purpose...</option>
                <option 
                  v-for="item in purposeItems" 
                  :key="item.purposeId" 
                  :value="item.purposeName"
                >
                  {{ item.purposeName }}
                </option>
              </select>
            </div>
          </div>
        </div>

        <!-- STEP 2: FACE SCAN CAMERA -->
        <div v-if="step === 2" class="space-y-5">
          
          <!-- Camera Feed Viewport / Captured Preview -->
          <div class="relative w-full aspect-[4/3] bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden flex items-center justify-center shadow-inner">
            
            <!-- Showing Captured Image -->
            <div v-if="formState.photo" class="w-full h-full relative">
              <img 
                :src="`data:image/jpeg;base64,${formState.photo}`" 
                class="w-full h-full object-cover" 
                alt="captured face snapshot"
              />
              <div class="absolute inset-0 bg-slate-950/20"></div>
              
              <!-- Badges -->
              <span class="absolute top-3 left-3 bg-[#00df89] text-slate-900 text-xs font-bold px-3 py-1 rounded-full shadow flex items-center gap-1">
                <span>✓</span> Snapshot Ready
              </span>
              
              <button
                @click="formState.photo = ''"
                class="absolute top-3 right-3 bg-black/60 hover:bg-black/80 text-white text-xs font-semibold px-3 py-1.5 rounded-xl transition"
              >
                Retake Photo
              </button>
            </div>

            <!-- Showing Camera Stream -->
            <div v-else-if="showCam" class="w-full h-full relative">
              <video
                ref="videoRef"
                autoplay
                playsinline
                muted
                class="w-full h-full object-cover"
                :class="[
                  facing === 'user' ? 'scale-x-[-1]' : '',
                  switching ? 'opacity-30' : 'opacity-100'
                ]"
              ></video>

              <!-- Neon Face Outline Overlay -->
              <div class="absolute inset-0 border border-emerald-500/20 flex items-center justify-center pointer-events-none">
                <!-- Ellipse shape indicator -->
                <div class="w-[50%] h-[68%] rounded-[50%/55%] border-2 border-dashed border-[#00df89]/75 shadow-[0_0_20px_rgba(0,223,137,0.2)] animate-pulse relative">
                  <!-- Scanning overlay sweep line -->
                  <div class="absolute left-0 right-0 h-0.5 bg-gradient-to-r from-transparent via-[#00df89] to-transparent animate-sweep top-0"></div>
                </div>
              </div>

              <!-- Camera controls on screen overlay -->
              <div class="absolute bottom-3 left-3 right-3 flex items-center justify-between text-white bg-black/40 backdrop-blur-md rounded-xl p-2">
                <span class="text-xs font-medium pl-1 text-slate-300">
                  {{ facing === 'user' ? 'Front Camera' : 'Rear Camera' }}
                </span>
                <button
                  v-if="hasMultipleCams"
                  @click="flipCamera(videoRef)"
                  class="bg-white/10 hover:bg-white/20 text-white text-xs font-semibold px-2.5 py-1 rounded-lg transition"
                >
                  Switch Cam
                </button>
              </div>

              <!-- Loader on switching -->
              <div v-if="switching" class="absolute inset-0 bg-slate-950/60 flex flex-col items-center justify-center text-white space-y-2">
                <span class="animate-spin text-xl">🔄</span>
                <span class="text-xs">Initializing camera stream...</span>
              </div>
            </div>

            <!-- Camera Idle / Closed State -->
            <div v-else class="text-center p-6 space-y-4">
              <div class="w-16 h-16 rounded-full bg-slate-800 text-slate-400 flex items-center justify-center text-2xl mx-auto shadow-inner">
                📷
              </div>
              <div class="space-y-1">
                <h4 class="font-bold text-slate-200 text-sm">WebRTC Face Scanning</h4>
                <p class="text-xs text-slate-500 max-w-[280px]">
                  Please enable camera capture to record a verification snapshot.
                </p>
              </div>
              <button
                @click="triggerCamera"
                class="bg-[#0e4391] hover:bg-[#0a3575] text-white text-xs font-semibold px-5 py-2.5 rounded-xl shadow-lg transition active:scale-95"
              >
                Launch Live Camera
              </button>
            </div>
            
            <canvas ref="canvasRef" style="display: none;"></canvas>
          </div>

          <!-- Camera Capture Action row -->
          <div v-if="showCam && !formState.photo" class="flex justify-center">
            <button
              @click="triggerCapture"
              class="w-14 h-14 rounded-full bg-[#00df89] hover:bg-[#00c576] border-4 border-emerald-500/20 text-slate-900 text-2xl flex items-center justify-center shadow-lg active:scale-90 transition duration-150"
              title="Capture Photo"
            >
              📸
            </button>
          </div>

          <!-- Error Message -->
          <div v-if="cameraError" class="bg-rose-50 border border-rose-100 rounded-xl p-3 text-xs text-rose-600 flex items-center gap-2">
            <span>⚠</span>
            <span>Camera Access Failed. Please grant permission or connect a device.</span>
          </div>

          <!-- Summary card info -->
          <div class="bg-slate-50 rounded-2xl p-4 text-xs text-slate-600 space-y-2 border border-slate-100">
            <div class="flex justify-between border-b border-slate-200/50 pb-1.5">
              <span class="text-slate-400">Employee</span>
              <strong class="text-slate-800">{{ formState.fullName }}</strong>
            </div>
            <div class="flex justify-between border-b border-slate-200/50 pb-1.5">
              <span class="text-slate-400">Contact Department</span>
              <strong class="text-slate-800">{{ formState.contactDept || '-' }}</strong>
            </div>
            <div class="flex justify-between pb-0.5">
              <span class="text-slate-400">Visit Purpose</span>
              <strong class="text-slate-800">{{ formState.purpose || '-' }}</strong>
            </div>
          </div>
        </div>

      </div>

      <!-- Action Footer buttons -->
      <div class="p-6 bg-slate-50 border-t border-slate-100 flex gap-3">
        <!-- Back button (for step 2) / Cancel button (for step 1) -->
        <button
          v-if="step === 1"
          @click="emit('close')"
          class="flex-1 bg-white hover:bg-slate-50 border border-slate-200 text-slate-700 text-sm font-semibold py-3 rounded-xl transition duration-150 active:scale-[0.98]"
        >
          Cancel
        </button>
        <button
          v-else
          @click="prevStep"
          class="flex-1 bg-white hover:bg-slate-50 border border-slate-200 text-slate-700 text-sm font-semibold py-3 rounded-xl transition duration-150 active:scale-[0.98]"
        >
          Back
        </button>

        <!-- Next button (for step 1) / Submit button (for step 2) -->
        <button
          v-if="step === 1"
          @click="nextStep"
          :disabled="!hasCardLookupResult"
          class="flex-1 bg-[#0e4391] hover:bg-[#0a3575] text-white text-sm font-semibold py-3 rounded-xl shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition duration-150 active:scale-[0.98]"
        >
          Next
        </button>
        <button
          v-else
          @click="handleCheckInSubmit"
          :disabled="submitLoading"
          class="flex-1 bg-[#00df89] hover:bg-[#00c576] text-slate-900 text-sm font-bold py-3 rounded-xl shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition duration-150 active:scale-[0.98] flex items-center justify-center gap-2"
        >
          <span v-if="submitLoading" class="animate-spin text-sm">🔄</span>
          <span>{{ submitLoading ? 'Submitting...' : '✓ Confirm CheckIn' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>
