<script setup>
import { ref, computed } from 'vue'
import { useRecords } from '../../composables/useRecords'
import { useToast } from '../../composables/useToast'
import { useAudio } from '../../composables/useAudio'

const props = defineProps({
  record: {
    type: Object,
    required: true
  }
})

const emit = defineEmits(['close', 'checkoutSuccess'])

const { submitCheckOut } = useRecords()
const { showToast } = useToast()
const { playBeep } = useAudio()

const checkOutLoading = ref(false)

const isCheckedOut = computed(() => !!props.record.checkOutTime)

function formatDateTime(value) {
  if (!value) return '-'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return date.toLocaleString()
}

// Calculates stay duration in human-readable form
const stayDuration = computed(() => {
  if (!props.record.checkInTime) return '-'
  const start = new Date(props.record.checkInTime).getTime()
  const end = props.record.checkOutTime ? new Date(props.record.checkOutTime).getTime() : Date.now()
  
  const diffMs = end - start
  if (diffMs < 0) return '-'
  
  const mins = Math.floor(diffMs / 60000)
  const hrs = Math.floor(mins / 60)
  const days = Math.floor(hrs / 24)
  
  if (days > 0) return `${days}d ${hrs % 24}h ${mins % 60}m`
  if (hrs > 0) return `${hrs}h ${mins % 60}m`
  return `${mins}m`
})

async function handleCheckOut() {
  if (!props.record.logId || checkOutLoading.value) return
  
  checkOutLoading.value = true
  try {
    const res = await submitCheckOut(props.record.logId)
    if (res?.success) {
      playBeep(true)
      showToast(`${props.record.fullName || 'User'} checked out successfully!`, 'success')
      emit('checkoutSuccess')
      emit('close')
    } else {
      playBeep(false)
      showToast(res?.message || 'CheckOut failed.', 'error')
    }
  } catch (err) {
    playBeep(false)
    showToast('System error confirming checkout.', 'error')
  } finally {
    checkOutLoading.value = false
  }
}
</script>

<template>
  <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-md z-[80] flex items-center justify-center p-4">
    <!-- Modal Card -->
    <div class="bg-white rounded-3xl w-full max-w-md shadow-2xl overflow-hidden border border-slate-100 flex flex-col max-h-[90vh]">
      <!-- Header -->
      <div class="bg-slate-50 border-b border-slate-100 px-6 py-5 flex justify-between items-center">
        <div>
          <h2 class="text-lg font-bold text-slate-800">Check-In Profile</h2>
          <p class="text-xs text-slate-400 mt-0.5">Access Log ID: #{{ record.logId }}</p>
        </div>
        <button @click="emit('close')" class="w-8 h-8 rounded-full bg-slate-100 hover:bg-slate-200 text-slate-500 flex items-center justify-center transition">
          ✕
        </button>
      </div>

      <!-- Content -->
      <div class="p-6 flex-1 overflow-y-auto space-y-6">
        <!-- Profile Picture Viewer -->
        <div class="relative w-40 h-40 mx-auto rounded-full overflow-hidden border-4 border-slate-100 shadow-md bg-slate-50 flex items-center justify-center">
          <img
            v-if="record.photo"
            :src="`data:image/jpeg;base64,${record.photo}`"
            class="w-full h-full object-cover"
            alt="employee verification"
          />
          <div v-else class="text-4xl text-slate-300 select-none">
            👤
          </div>
          
          <!-- Status Tag Overlay -->
          <div class="absolute bottom-2 left-1/2 transform -translate-x-1/2 shadow">
            <span 
              class="text-[10px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full"
              :class="isCheckedOut ? 'bg-slate-200 text-slate-600' : 'bg-emerald-500 text-white'"
            >
              {{ isCheckedOut ? 'Exited' : 'On-Site' }}
            </span>
          </div>
        </div>

        <!-- Details Grid -->
        <div class="space-y-3">
          <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wide border-b border-slate-100 pb-1.5">Employee Information</h3>
          
          <div class="grid grid-cols-2 gap-y-3.5 gap-x-4 text-xs">
            <div>
              <span class="text-slate-400 block mb-0.5">Full Name</span>
              <span class="font-bold text-slate-800 text-sm">{{ record.fullName || 'Unknown' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-0.5">User Code</span>
              <span class="font-semibold text-slate-700">{{ record.userCode || '-' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-0.5">Card ID</span>
              <span class="font-semibold text-slate-700">{{ record.cardNumber || '-' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-0.5">Department</span>
              <span class="font-semibold text-slate-700">{{ record.deptName || record.deptCode || '-' }}</span>
            </div>
          </div>
        </div>

        <div class="space-y-3">
          <h3 class="text-xs font-bold text-slate-400 uppercase tracking-wide border-b border-slate-100 pb-1.5">Visit Logs</h3>
          
          <div class="grid grid-cols-2 gap-y-3.5 gap-x-4 text-xs">
            <div>
              <span class="text-slate-400 block mb-0.5">Contact Department</span>
              <span class="font-semibold text-slate-700">{{ record.contactDept || '-' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-0.5">Visit Purpose</span>
              <span class="font-semibold text-slate-700">{{ record.purpose || '-' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-0.5">Check-In Time</span>
              <span class="font-semibold text-slate-700">{{ formatDateTime(record.checkInTime) }}</span>
            </div>
            <div>
              <span class="text-slate-400 block mb-0.5">Check-Out Time</span>
              <span class="font-semibold text-slate-700" :class="isCheckedOut ? 'text-slate-700' : 'text-amber-600 font-bold italic'">
                {{ isCheckedOut ? formatDateTime(record.checkOutTime) : 'On-Site' }}
              </span>
            </div>
            <div class="col-span-2">
              <span class="text-slate-400 block mb-0.5">Stay Duration</span>
              <span class="font-bold" :class="isCheckedOut ? 'text-slate-700' : 'text-emerald-600'">
                {{ stayDuration }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Action Footer -->
      <div class="p-6 bg-slate-50 border-t border-slate-100 flex gap-3">
        <button
          @click="emit('close')"
          class="flex-1 bg-white hover:bg-slate-50 border border-slate-200 text-slate-700 text-sm font-semibold py-3 rounded-xl transition duration-150 active:scale-[0.98]"
        >
          Close
        </button>
        <button
          v-if="!isCheckedOut"
          @click="handleCheckOut"
          :disabled="checkOutLoading"
          class="flex-1 bg-rose-500 hover:bg-rose-600 text-white text-sm font-semibold py-3 rounded-xl shadow-lg shadow-rose-500/10 transition duration-150 active:scale-[0.98] flex items-center justify-center gap-1.5"
        >
          <span v-if="checkOutLoading" class="animate-spin text-sm">🔄</span>
          <span>Confirm Checkout</span>
        </button>
      </div>
    </div>
  </div>
</template>
