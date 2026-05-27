<script setup>
import { useRouter } from 'vue-router'
import { useCheckInFlow } from '../composables/useCheckInFlow'
import headerLogo from '../assets/logo-jiahsin-co-chu.png'
import { useSweetAlert } from '../composables/useSweetAlert'
import AppDropdown from '../components/common/AppDropdown.vue'

const router = useRouter()
const { showError, showSuccess } = useSweetAlert()

const fireMobileAlert = async (message, type = 'info') => {
  if (type === 'success') {
    await showSuccess(message)
    return
  }
  await showError(message)
}

const {
  cameraError,
  facing,
  switching,
  hasMultipleCams,
  showCam,
  flipCamera,
  step,
  checkInCardNumber,
  cardInputRef,
  lookupLoading,
  lookupMessage,
  hasCardLookupResult,
  submitLoading,
  fieldsLockedByGuestData,
  formState,
  videoRef,
  canvasRef,
  scannerState,
  autoCaptureSupported,
  autoCaptureStatus,
  contactDeptItems,
  purposeItems,
  userTypeItems,
  handleCardLookup,
  nextStep,
  prevStep,
  resetCheckInFlow,
  triggerCamera
} = useCheckInFlow({
  notify: fireMobileAlert,
  onSuccess: async () => {
    await showSuccess('')
    window.location.href = '/check-in-mobile'
  }
})
</script>

<template>
  <div class="checkin-mobile-page min-h-screen p-4 md:p-8 xl:p-10">
    <div class="checkin-bg-layer"></div>

    <div class="checkin-shell relative z-10 mx-auto w-full max-w-3xl bg-white rounded-3xl shadow-xl border border-slate-200 overflow-visible">
      <div class="rounded-3xl overflow-hidden">
      <div class="bg-gradient-to-r from-[#0a3575] to-[#0e4391] px-6 py-5 md:px-8 md:py-6 text-white flex items-center justify-between">
        <div class="flex items-center gap-3 min-w-0">
          <img class="h-9 md:h-10 w-auto object-contain shrink-0" :src="headerLogo" alt="JIA HSIN" />
          <span class="h-8 md:h-9 w-px bg-white/70 shrink-0"></span>
          <div class="min-w-0 flex-1">
            <p class="checkin-mobile-header-title font-extrabold leading-tight tracking-wide">WAREHOUSE ACCESS</p>
            <p class="text-[10px] md:text-xs text-white/80 font-medium mt-0.5 truncate">Finished Goods Warehouse Access System</p>
          </div>
        </div>

      </div>

      <div class="p-6 md:p-8 space-y-5 md:space-y-6">
        <div v-if="step === 1" class="space-y-4">
          <label class="text-xs md:text-sm font-bold text-slate-500 uppercase">Card Number</label>
          <div class="flex gap-2">
            <input
              ref="cardInputRef"
              v-model="checkInCardNumber"
              :disabled="lookupLoading"
              @keydown.enter.prevent="handleCardLookup"
              class="flex-1 border border-slate-300 rounded-xl px-4 py-3 md:py-4 md:text-2xl focus:outline-none focus:ring-2 focus:ring-blue-300"
              placeholder="Scan or enter card number"
            />
            <button @click="handleCardLookup" :disabled="lookupLoading" class="px-4 py-3 md:px-6 md:py-4 rounded-xl bg-slate-900 text-white font-semibold md:text-lg disabled:opacity-50">
              {{ lookupLoading ? 'Checking...' : 'Check' }}
            </button>
            <button
              @click="resetCheckInFlow"
              :disabled="lookupLoading"
              class="px-3 py-2 md:px-4 md:py-3 rounded-lg border border-slate-300 bg-white text-slate-700 text-xs md:text-sm font-semibold disabled:opacity-50"
            >
              Reload
            </button>
          </div>
          <p v-if="lookupMessage" class="text-xs md:text-sm" :class="hasCardLookupResult ? 'text-emerald-600' : 'text-rose-600'">{{ lookupMessage }}</p>

          <div v-if="hasCardLookupResult" class="grid grid-cols-1 md:grid-cols-2 gap-3 lg:gap-4">
            <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 md:p-4 text-sm md:text-base"><b>Company:</b> {{ formState.company || '-' }}</div>
            <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 md:p-4 text-sm md:text-base"><b>Contact Person:</b> {{ formState.contactPerson || '-' }}</div>
            <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 md:p-4 text-sm md:text-base"><b>Code:</b> {{ formState.userCode }}</div>
            <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 md:p-4 text-sm md:text-base"><b>User:</b> {{ formState.fullName }}</div>
            <template v-if="fieldsLockedByGuestData">
              <div class="md:col-span-2">
                <label class="text-xs md:text-sm font-bold text-slate-500 uppercase mb-2 block">User Type</label>
                <AppDropdown
                  v-model="formState.userTypeId"
                  :items="userTypeItems"
                  value-key="userTypeId"
                  label-key="userTypeName"
                  placeholder="Select user type"
                  search-placeholder="Search user type..."
                  :searchable="true"
                />
              </div>
              <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 md:p-4 text-sm md:text-base">
                <b>Contact Dept:</b> {{ formState.contactDept || '-' }}
              </div>
              <div class="bg-slate-50 border border-slate-200 rounded-xl p-3 md:p-4 text-sm md:text-base">
                <b>Purpose:</b> {{ formState.purpose || '-' }}
              </div>
            </template>
            <template v-else>
              <AppDropdown
                v-model="formState.contactDept"
                :items="contactDeptItems"
                value-key="contactDeptName"
                label-key="contactDeptName"
                placeholder="Select contact department"
                search-placeholder="Search contact department..."
                :searchable="true"
                :clearable="true"
              />
              <AppDropdown
                v-model="formState.purpose"
                :items="purposeItems"
                value-key="purposeName"
                label-key="purposeName"
                placeholder="Select purpose"
                search-placeholder="Search purpose..."
                :searchable="true"
                :clearable="true"
              />
            </template>
          </div>
        </div>
          
        <div v-else class="space-y-4">
          <div class="relative w-full checkin-camera-frame rounded-2xl bg-slate-900 overflow-hidden border border-slate-800">
            <img v-if="formState.photo" :src="`data:image/jpeg;base64,${formState.photo}`" class="w-full h-full object-cover" alt="captured" />
            <div v-else-if="showCam" class="w-full h-full relative">
              <video ref="videoRef" autoplay playsinline muted class="w-full h-full object-cover" :class="[facing === 'user' ? 'scale-x-[-1]' : '', switching ? 'opacity-30' : 'opacity-100']"></video>
            </div>
            <div v-else class="w-full h-full flex flex-col items-center justify-center text-slate-300 gap-3">
              <span class="text-sm">Camera is off</span>
              <button @click="triggerCamera" class="px-4 py-2 md:px-6 md:py-3 rounded-lg bg-blue-600 text-white text-sm md:text-base font-semibold">Open Camera</button>
            </div>
            <canvas ref="canvasRef" style="display:none;"></canvas>
          </div>

          <div class="flex gap-2">
            <button v-if="formState.photo" @click="formState.photo = ''" class="px-4 py-2 md:px-5 md:py-3 rounded-lg bg-slate-200 text-slate-800 font-semibold md:text-base">Retake</button>
            <button v-if="showCam && hasMultipleCams" @click="flipCamera(videoRef)" class="px-4 py-2 md:px-5 md:py-3 rounded-lg bg-slate-200 text-slate-800 font-semibold md:text-base">Switch Camera</button>
            <span v-if="cameraError" class="text-xs md:text-sm text-rose-600 self-center">Camera error</span>
          </div>
          <p v-if="showCam && !formState.photo && autoCaptureSupported" class="text-xs md:text-sm text-slate-500">
            Auto capture status: {{ autoCaptureStatus }}
          </p>
          <p v-if="showCam && !formState.photo && !autoCaptureSupported" class="text-xs md:text-sm text-amber-600">
            Auto capture not supported on this browser. Use manual capture.
          </p>
        </div>
      </div>

      <div class="border-t border-slate-200 bg-slate-50 p-5 md:p-6 flex gap-3">
        <button v-if="step === 1" @click="router.push('/monitor')" class="flex-1 py-3 md:py-4 rounded-xl border border-slate-300 bg-white font-semibold text-slate-700 md:text-lg">Back to DashBoard</button>
        <button v-else @click="prevStep" class="flex-1 py-3 md:py-4 rounded-xl border border-slate-300 bg-white font-semibold text-slate-700 md:text-lg">Back</button>
        <button v-if="step === 1" @click="nextStep" :disabled="!hasCardLookupResult" class="flex-1 py-3 md:py-4 rounded-xl bg-blue-700 text-white font-semibold md:text-lg disabled:opacity-50">Next</button>
        <button v-else disabled class="flex-1 py-3 md:py-4 rounded-xl bg-emerald-100 text-emerald-800 font-semibold md:text-lg cursor-not-allowed">
          {{ submitLoading ? 'Submitting...' : 'Auto submit after face capture' }}
        </button>
      </div>
      </div>
    </div>

  </div>
</template>

<style scoped>
.checkin-mobile-page {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.checkin-shell {
  width: min(92vw, 980px);
}

.checkin-bg-layer {
  position: absolute;
  inset: 0;
  background-image: url('../assets/background.jpg');
  background-size: cover;
  background-position: center;
}

@media (min-width: 1100px) and (min-height: 1700px) {
  .checkin-shell {
    width: min(90vw, 1020px);
  }
}

.checkin-camera-frame {
  aspect-ratio: 4 / 3;
}

.checkin-mobile-header-title {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: clamp(0.95rem, 1.2vw + 0.4rem, 1.6rem);
}

@media (min-width: 1100px) and (orientation: landscape) {
  .checkin-shell {
    width: min(96vw, 1180px);
  }

  .checkin-camera-frame {
    aspect-ratio: 16 / 9;
    min-height: 420px;
  }
}

</style>
