<script setup>
import { ref, reactive, onMounted, onUnmounted, nextTick } from 'vue';
import { useI18n } from '../../composables/useI18n';
import { useRecords } from '../../composables/useRecords';
import { useAudio } from '../../composables/useAudio';
import { useCamera } from '../../composables/useCamera';

const { lang, t } = useI18n();
const { 
  departments, 
  purposes, 
  defaultContact, 
  getCardDetails, 
  executeCardCheckin, 
  executeCardCheckout 
} = useRecords();

const { playBeep } = useAudio();

const {
  ready: camReady,
  cameraError: camErrKey,
  facing: camFacing,
  switching: camSwitching,
  hasMultipleCams,
  showCam: kioskShowCam,
  openCamera,
  closeCamera,
  flipCamera,
  capturePhoto
} = useCamera();

const emit = defineEmits(['close']);

// Clock
const currentTime = ref(Date.now());
let clockInterval = null;

// Kiosk State
const cardInput = ref('');
const kioskStatus = ref({ active: false, success: true, message: '', details: '', returnCardAlert: false });
const showKioskRegister = ref(false);
const waitingForTempCard = ref(false);
const kioskForm = reactive({ name: '', company: '', type: 'vendor', department: '', contact: '', purpose: '業務洽談', note: '' });

const kioskVideoRef = ref(null);
const kioskCanvasRef = ref(null);
const kioskPendingCheckin = ref(null);

// Get actual translation of camera error if occurred
const camError = computed(() => {
  if (camErrKey.value === 'camera_error') {
    return t.value.camError || "Camera unavailable. Check browser permissions.";
  }
  return "";
});

function formatTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  return d.toLocaleString("zh-TW", { hour12: false, month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

function formatSimpleTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  const pad = (n) => n.toString().padStart(2, '0');
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

function formatDuration(ms) {
  if (!ms || ms < 0) return "—";
  const m = Math.floor(ms / 60000), h = Math.floor(m / 60);
  return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
}

function handleKioskTypeChange() {
  if (kioskForm.type === 'vendor') {
    kioskForm.department = '';
  } else if (!kioskForm.department && departments.value.length > 0) {
    kioskForm.department = departments.value[0];
  }
  
  if (kioskForm.type === 'staff') {
    kioskForm.company = '內部員工';
  } else {
    kioskForm.company = '';
  }
}

// Global swipe focus intercept
const handleGlobalClick = () => {
  if (!showKioskRegister.value && !waitingForTempCard.value) {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }
};

const handleGlobalKeydown = (e) => {
  if (showKioskRegister.value && !waitingForTempCard.value) return;
  
  if (document.activeElement && (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'SELECT' || document.activeElement.tagName === 'TEXTAREA')) {
    if (document.activeElement.id !== 'kiosk-card-focus') {
      return;
    }
  }

  const el = document.getElementById('kiosk-card-focus');
  if (el && document.activeElement !== el) {
    el.focus();
  }
};

onMounted(() => {
  clockInterval = setInterval(() => {
    currentTime.value = Date.now();
  }, 1000);

  // Focus scanner field
  nextTick(() => {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  });

  window.addEventListener('click', handleGlobalClick);
  window.addEventListener('keydown', handleGlobalKeydown);
});

onUnmounted(() => {
  if (clockInterval) clearInterval(clockInterval);
  window.removeEventListener('click', handleGlobalClick);
  window.removeEventListener('keydown', handleGlobalKeydown);
  closeCamera();
});

// Trigger simulator action
function handleKioskSimSwipe(cardId) {
  cardInput.value = cardId;
  handleCardSwipe();
}

// Main Card/Barcode Swipe Handler
async function handleCardSwipe() {
  const cardId = cardInput.value.trim().toUpperCase();
  cardInput.value = '';
  if (!cardId) return;

  if (waitingForTempCard.value) {
    // Bind current form details to the temporary card
    await bindTempCard(cardId);
    return;
  }

  try {
    const res = await getCardDetails(cardId);
    
    if (res.success && res.found) {
      if (res.type === 'active_record') {
        const checkoutRes = await executeCardCheckout(cardId);
        if (checkoutRes.success) {
          const record = checkoutRes.data;
          kioskStatus.value = {
            active: true,
            success: true,
            message: `${record.name} ${t.value.kioskSwipeCheckout}`,
            details: `${t.value.kioskSwipeExitTime}: ${formatTime(checkoutRes.exitTime)} | ${t.value.kioskSwipeDuration}: ${formatDuration(checkoutRes.exitTime - record.entryTime)}`,
            returnCardAlert: cardId.startsWith('TEMP')
          };
          playBeep(true);
        }
      } else if (res.type === 'employee') {
        const employee = res.data;
        openKioskCameraFlow({
          cardId: cardId,
          payload: {
            name: employee.name,
            company: '本廠員工',
            type: 'staff',
            department: employee.dept,
            contact: employee.name,
            purpose: '內部員工進出',
            cardId: cardId,
            empAvatar: employee.avatar
          },
          message: `${t.value.kioskSwipeWelcome} ${t.value.types.staff} ${employee.name}`,
          details: `${t.value.empId}: ${employee.id} | ${t.value.dept}: ${employee.dept} | ${t.value.kioskSwipeTime}: ${formatTime(Date.now())}`
        });
      }
    } else {
      if (cardId.startsWith('VISIT')) {
        const demoName = cardId === 'VISIT-002' ? 'Nguyễn Văn An' : '預約來訪廠商';
        const demoCompany = cardId === 'VISIT-002' ? 'SGS Taiwan' : '大同五金';
        openKioskCameraFlow({
          cardId: cardId,
          payload: {
            name: demoName,
            company: demoCompany,
            type: cardId === 'VISIT-002' ? 'audit' : 'vendor',
            department: '倉儲部',
            contact: defaultContact.value.name,
            purpose: '業務洽談',
            cardId: cardId,
            empAvatar: cardId === 'VISIT-002' ? 'https://api.dicebear.com/7.x/thumbs/svg?seed=E006&backgroundColor=1e293b&shapeColor=fcd34d' : ''
          },
          message: `${t.value.kioskSwipeWelcome} ${t.value.types[cardId === 'VISIT-002' ? 'audit' : 'vendor']} ${demoName}`,
          details: `${t.value.cols[0]}: ${cardId} | ${t.value.company}: ${demoCompany} | ${t.value.kioskSwipeTime}: ${formatTime(Date.now())}`
        });
      } else {
        kioskStatus.value = {
          active: true,
          success: false,
          message: t.value.kioskSwipeError,
          details: `${t.value.cols[0]}: ${cardId} | ${t.value.kioskSwipeErrorSub}`,
          returnCardAlert: false
        };
        playBeep(false);
      }
    }
  } catch (e) {
    console.error("Kiosk Swipe Error:", e);
    kioskStatus.value = {
      active: true,
      success: false,
      message: t.value.kioskSystemError || "系統連線錯誤",
      details: `${e.toString()} | ${t.value.kioskSystemErrorSub || "請檢查後端伺服器與網路。"}`,
      returnCardAlert: false
    };
    playBeep(false);
  }

  setTimeout(() => {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }, 100);
}

// Camera activation overlay trigger
function openKioskCameraFlow(pendingData) {
  kioskPendingCheckin.value = pendingData;
  nextTick(() => {
    openCamera(kioskVideoRef.value);
  });
}

function shootKioskPhoto() {
  const photo = capturePhoto(kioskVideoRef.value, kioskCanvasRef.value);
  executeKioskCheckin(photo);
}

function skipKioskPhoto() {
  closeCamera();
  executeKioskCheckin("");
}

function cancelKioskCheckin() {
  closeCamera();
  kioskPendingCheckin.value = null;
}

// Final backend commit post-camera
async function executeKioskCheckin(photoData) {
  if (!kioskPendingCheckin.value) return;
  const { cardId, payload, message, details, isBindMode } = kioskPendingCheckin.value;
  
  try {
    const finalPayload = { ...payload, photo: photoData || "" };
    const checkinRes = await executeCardCheckin(finalPayload);

    if (checkinRes.success) {
      kioskStatus.value = {
        active: true,
        success: true,
        message: message,
        details: details,
        returnCardAlert: false
      };
      if (isBindMode) {
        waitingForTempCard.value = false;
        showKioskRegister.value = false;
      }
      playBeep(true);
    } else {
      kioskStatus.value = {
        active: true,
        success: false,
        message: t.value.kioskSwipeError,
        details: checkinRes.error || "進場登記失敗",
        returnCardAlert: false
      };
      playBeep(false);
    }
  } catch (e) {
    console.error("Kiosk Swipe Checkin Error:", e);
    kioskStatus.value = {
      active: true,
      success: false,
      message: t.value.kioskSystemError || "系統連線錯誤",
      details: `${e.toString()} | ${t.value.kioskSystemErrorSub || "請檢查後端伺服器與網路。"}`,
      returnCardAlert: false
    };
    playBeep(false);
  } finally {
    kioskPendingCheckin.value = null;
  }
}

function startTempCardBinding() {
  if (!kioskForm.name.trim()) {
    alert(t.value.kioskAlertName);
    return;
  }
  if (kioskForm.type === 'staff' && !kioskForm.company.trim()) {
    alert(t.value.kioskAlertCompany);
    return;
  }
  waitingForTempCard.value = true;
  setTimeout(() => {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }, 100);
}

async function bindTempCard(cardId) {
  openKioskCameraFlow({
    cardId: cardId,
    payload: {
      name: kioskForm.name,
      company: kioskForm.type === 'staff' ? kioskForm.company : (kioskForm.company || '無'),
      type: kioskForm.type,
      department: kioskForm.type === 'vendor' ? '' : kioskForm.department,
      contact: defaultContact.value.name,
      purpose: kioskForm.purpose,
      note: kioskForm.note,
      cardId: cardId
    },
    message: t.value.kioskAlertBindSuccess,
    details: `${t.value.cols[0]}: ${cardId} | ${t.value.types[kioskForm.type]}: ${kioskForm.name} (${kioskForm.company}) | ${t.value.kioskSwipeTime}: ${formatTime(Date.now())}`,
    isBindMode: true
  });
}
</script>

<template>
  <div class="fixed inset-0 bg-[#061129]/95 z-[9000] backdrop-blur-md flex items-center justify-center p-4">
    <div class="w-full max-w-[1100px] h-[90vh] bg-[#0c1c3e]/90 border border-white/10 rounded-3xl flex flex-col overflow-hidden shadow-2xl relative">
      <!-- Close Kiosk Header -->
      <div class="flex justify-between items-center px-6 py-4 border-b border-white/5 flex-shrink-0">
        <div class="flex items-center gap-3">
          <span class="w-2.5 h-2.5 rounded-full bg-emerald-400 dot-ring"></span>
          <h2 class="text-white text-lg font-bold font-sans">{{ t.kioskTitle }}</h2>
        </div>
        <button 
          class="bg-white/5 hover:bg-white/10 border border-white/10 text-white/80 hover:text-white px-4 py-1.5 rounded-lg text-xs font-semibold transition"
          @click="$emit('close')"
        >
          {{ t.kioskClose }}
        </button>
      </div>

      <div class="flex-1 flex overflow-hidden">
        <!-- Left panel: Scanner animation -->
        <div class="flex-[1.2] p-8 flex flex-col items-center justify-center border-r border-white/5 relative overflow-y-auto">
          <!-- Clock -->
          <div class="text-2xl font-mono font-black text-white/40 tracking-wider mb-8">
            {{ formatSimpleTime(currentTime) }}
          </div>

          <!-- Scanner Animation Circle -->
          <div class="relative w-48 h-48 rounded-full border border-white/10 flex items-center justify-center mb-6">
            <!-- Pulsing Rings -->
            <div class="absolute inset-0 rounded-full border border-emerald-500/20 radar-ring"></div>
            <div class="absolute inset-3 rounded-full border border-emerald-500/10 radar-ring" style="animation-delay: 0.6s"></div>
            <div class="absolute inset-6 rounded-full border border-emerald-500/5 radar-ring" style="animation-delay: 1.2s"></div>
            
            <div 
              class="w-36 h-36 rounded-full bg-[#112757] border border-white/10 flex items-center justify-center shadow-lg transition-colors"
              :class="{ '!border-red-500/30 !bg-red-950/20': kioskStatus.active && !kioskStatus.success }"
            >
              <div class="flex flex-col items-center gap-1.5 select-none">
                <span class="text-3xl">💳</span>
                <span class="text-[10px] text-white/30 font-black">SCAN HERE</span>
              </div>
            </div>
          </div>

          <p class="text-sm font-semibold text-white/60 mb-6 text-center max-w-xs">{{ t.kioskHint }}</p>

          <!-- Intercept field -->
          <div class="h-0 opacity-0 overflow-hidden">
            <input 
              id="kiosk-card-focus" 
              type="text" 
              v-model="cardInput" 
              @keydown.enter="handleCardSwipe" 
              class="absolute -top-10"
            />
          </div>

          <!-- Status Alert Banner -->
          <div 
            v-if="kioskStatus.active" 
            class="w-full max-w-md p-4 rounded-xl border flex gap-3 text-left transition duration-300 transform scale-100"
            :class="[
              !kioskStatus.success 
                ? 'border-red-500/20 bg-red-500/10 text-red-300' 
                : kioskStatus.returnCardAlert 
                ? 'border-amber-500/20 bg-amber-500/10 text-amber-300' 
                : 'border-emerald-500/20 bg-emerald-500/10 text-emerald-300'
            ]"
          >
            <div class="text-xl font-bold flex items-center shrink-0">
              <span v-if="!kioskStatus.success">✗</span>
              <span v-else>✓</span>
            </div>
            <div class="min-w-0 flex-1">
              <h3 class="text-sm font-black truncate">{{ kioskStatus.message }}</h3>
              <p class="text-xs text-white/60 font-semibold truncate mt-0.5">{{ kioskStatus.details }}</p>
              <div v-if="kioskStatus.returnCardAlert" class="text-[11px] text-amber-400 font-extrabold animate-pulse mt-1">
                {{ t.kioskSwipeReturnCard }}
              </div>
            </div>
          </div>

          <!-- Manual Get Card Trigger -->
          <div class="mt-8" v-if="!showKioskRegister">
            <button 
              class="bg-gradient-to-r from-primary to-primary-light text-white text-xs font-bold px-6 py-3 rounded-xl shadow-lg hover:shadow-primary/20 active:scale-95 transition"
              @click="showKioskRegister = true"
            >
              {{ t.kioskGetTemp }}
            </button>
          </div>
        </div>

        <!-- Right panel: Scanner simulation & Bind forms -->
        <div class="flex-1 p-8 overflow-y-auto bg-[#0a1835]/50 flex flex-col">
          <!-- Manual Form bind -->
          <div class="flex-1 flex flex-col justify-between" v-if="showKioskRegister">
            <div class="space-y-4">
              <h3 class="text-white text-base font-bold">{{ t.kioskBindTitle }}</h3>
              
              <div v-if="!waitingForTempCard" class="space-y-3">
                <div class="flex flex-col gap-1.5 text-left">
                  <label class="text-xs font-semibold text-white/60">人員類型 (Type)</label>
                  <select 
                    v-model="kioskForm.type" 
                    @change="handleKioskTypeChange"
                    class="w-full bg-[#11254e] border border-white/10 text-white/80 rounded-xl px-4 py-2.5 text-sm outline-none cursor-pointer focus:border-primary/50"
                  >
                    <option value="vendor">{{ t.types.vendor }} (Vendor)</option>
                    <option value="brand">{{ t.types.brand }} (Brand)</option>
                    <option value="audit">{{ t.types.audit }} (Auditor)</option>
                    <option value="staff">{{ t.types.staff }} (Employee)</option>
                  </select>
                </div>

                <div class="flex flex-col gap-1.5 text-left">
                  <label class="text-xs font-semibold text-white/60">{{ t.name }} (Name) *</label>
                  <input 
                    type="text" 
                    v-model="kioskForm.name" 
                    :placeholder="t.name" 
                    class="w-full bg-[#11254e] border border-white/10 text-white/80 rounded-xl px-4 py-2.5 text-sm outline-none focus:border-primary/50"
                  />
                </div>

                <div class="flex flex-col gap-1.5 text-left">
                  <label class="text-xs font-semibold text-white/60">{{ t.company }} (Company){{ kioskForm.type === 'staff' ? ' *' : '' }}</label>
                  <input 
                    type="text" 
                    v-model="kioskForm.company" 
                    :placeholder="t.company" 
                    class="w-full bg-[#11254e] border border-white/10 text-white/80 rounded-xl px-4 py-2.5 text-sm outline-none focus:border-primary/50"
                  />
                </div>

                <div class="flex flex-col gap-1.5 text-left">
                  <label class="text-xs font-semibold text-white/60">{{ t.dept }} (Department){{ kioskForm.type === 'vendor' ? t.kioskDeptVendor : '' }}</label>
                  <select 
                    v-model="kioskForm.department" 
                    :disabled="kioskForm.type === 'vendor'"
                    class="w-full bg-[#11254e] border border-white/10 text-white/80 rounded-xl px-4 py-2.5 text-sm outline-none cursor-pointer focus:border-primary/50 disabled:opacity-50"
                  >
                    <option value="" v-if="kioskForm.type === 'vendor'">—</option>
                    <option v-for="d in departments" :key="d" :value="d" v-else>{{ d }}</option>
                  </select>
                </div>

                <div class="flex flex-col gap-1.5 text-left">
                  <label class="text-xs font-semibold text-white/60">{{ t.purpose }} (Purpose)</label>
                  <select 
                    v-model="kioskForm.purpose"
                    class="w-full bg-[#11254e] border border-white/10 text-white/80 rounded-xl px-4 py-2.5 text-sm outline-none cursor-pointer focus:border-primary/50"
                  >
                    <option v-for="p in purposes" :key="p" :value="p">{{ p }}</option>
                  </select>
                </div>

                <div class="flex flex-col gap-1.5 text-left">
                  <label class="text-xs font-semibold text-white/60">{{ t.note }} (Note)</label>
                  <input 
                    type="text" 
                    v-model="kioskForm.note" 
                    :placeholder="t.note" 
                    class="w-full bg-[#11254e] border border-white/10 text-white/80 rounded-xl px-4 py-2.5 text-sm outline-none focus:border-primary/50"
                  />
                </div>
              </div>

              <!-- Waiting Sweep overlay -->
              <div v-else class="flex flex-col items-center justify-center py-10 text-center">
                <div class="w-10 h-10 border-4 border-white/10 border-t-emerald-500 rounded-full animate-spin mb-4"></div>
                <h4 class="text-white text-sm font-bold">{{ t.kioskWaitingBind }}</h4>
                <p class="text-xs text-white/50 font-medium max-w-xs mt-1.5">{{ t.kioskWaitingHint }}</p>
              </div>
            </div>

            <!-- Footer Forms actions -->
            <div class="flex gap-3 mt-6">
              <button 
                class="flex-1 bg-white/5 hover:bg-white/10 border border-white/10 text-white text-xs font-bold py-3 rounded-xl active:scale-95 transition"
                @click="showKioskRegister = false; waitingForTempCard = false;"
              >
                {{ t.cancel }}
              </button>
              <button 
                v-if="!waitingForTempCard"
                class="flex-[2] bg-gradient-to-r from-emerald-500 to-emerald-600 text-white text-xs font-bold py-3 rounded-xl active:scale-95 transition"
                @click="startTempCardBinding"
              >
                {{ t.kioskSubmit }} →
              </button>
              <button 
                v-else
                class="flex-[2] bg-white/10 border border-white/10 text-white text-xs font-bold py-3 rounded-xl active:scale-95 transition"
                @click="waitingForTempCard = false"
              >
                {{ t.kioskBack }}
              </button>
            </div>
          </div>

          <!-- Sensors simulation panel -->
          <div class="space-y-4" :class="{ 'mt-10 border-t border-white/5 pt-8': showKioskRegister }">
            <div class="text-left select-none">
              <h3 class="text-white text-xs font-bold uppercase tracking-wider">{{ t.kioskSimHeader }}</h3>
              <p class="text-[11px] text-white/40 font-medium mt-0.5">{{ t.kioskSimSub }}</p>
            </div>

            <div class="space-y-4 text-left">
              <!-- Staff IC -->
              <div>
                <span class="text-[10px] text-white/35 font-bold uppercase tracking-widest">{{ t.kioskSimStaff }}</span>
                <div class="grid grid-cols-2 gap-3 mt-1.5">
                  <div 
                    class="bg-[#122349] border border-white/5 hover:border-white/15 px-3 py-2.5 rounded-xl flex items-center justify-between cursor-pointer active:scale-95 transition"
                    @click="handleKioskSimSwipe('E001')"
                  >
                    <div class="min-w-0">
                      <p class="text-xs font-bold text-white truncate">{{ lang === 'zh' ? '張志明' : (lang === 'en' ? 'Jimmy Zhang' : 'Trương Chí Minh') }}</p>
                      <span class="text-[9px] text-white/40 font-bold uppercase">E001 (IC)</span>
                    </div>
                    <span class="text-base shrink-0">💳</span>
                  </div>
                  <div 
                    class="bg-[#122349] border border-white/5 hover:border-white/15 px-3 py-2.5 rounded-xl flex items-center justify-between cursor-pointer active:scale-95 transition"
                    @click="handleKioskSimSwipe('E002')"
                  >
                    <div class="min-w-0">
                      <p class="text-xs font-bold text-white truncate">{{ lang === 'zh' ? '林美玲' : (lang === 'en' ? 'May Lin' : 'Lâm Mỹ Linh') }}</p>
                      <span class="text-[9px] text-white/40 font-bold uppercase">E002 (IC)</span>
                    </div>
                    <span class="text-base shrink-0">💳</span>
                  </div>
                </div>
              </div>

              <!-- Visitor Barcode -->
              <div>
                <span class="text-[10px] text-white/35 font-bold uppercase tracking-widest">{{ t.kioskSimVisitor }}</span>
                <div class="grid grid-cols-2 gap-3 mt-1.5">
                  <div 
                    class="bg-[#122349] border border-white/5 hover:border-white/15 px-3 py-2.5 rounded-xl flex items-center justify-between cursor-pointer active:scale-95 transition"
                    @click="handleKioskSimSwipe('VISIT-002')"
                  >
                    <div class="min-w-0">
                      <p class="text-xs font-bold text-white truncate">{{ lang === 'zh' ? '阮先生 (SGS)' : (lang === 'en' ? 'Mr. Nguyen (SGS)' : 'Ông Nguyễn (SGS)') }}</p>
                      <span class="text-[9px] text-white/40 font-bold uppercase">VISIT-002</span>
                    </div>
                    <span class="text-lg shrink-0">🎫</span>
                  </div>
                </div>
              </div>

              <!-- Temporary Card -->
              <div>
                <span class="text-[10px] text-white/35 font-bold uppercase tracking-widest">{{ t.kioskSimTemp }}</span>
                <div class="grid grid-cols-2 gap-3 mt-1.5">
                  <div 
                    class="bg-[#122349] border border-white/5 hover:border-white/15 px-3 py-2.5 rounded-xl flex items-center justify-between cursor-pointer active:scale-95 transition"
                    @click="handleKioskSimSwipe('TEMP-888')"
                  >
                    <div class="min-w-0">
                      <p class="text-xs font-bold text-white truncate">{{ lang === 'zh' ? '臨時卡 A' : (lang === 'en' ? 'Temp Card A' : 'Thẻ Tạm A') }}</p>
                      <span class="text-[9px] text-white/40 font-bold uppercase">TEMP-888</span>
                    </div>
                    <span class="text-base shrink-0">💳</span>
                  </div>
                  <div 
                    class="bg-[#122349] border border-white/5 hover:border-white/15 px-3 py-2.5 rounded-xl flex items-center justify-between cursor-pointer active:scale-95 transition"
                    @click="handleKioskSimSwipe('TEMP-999')"
                  >
                    <div class="min-w-0">
                      <p class="text-xs font-bold text-white truncate">{{ lang === 'zh' ? '臨時卡 B' : (lang === 'en' ? 'Temp Card B' : 'Thẻ Tạm B') }}</p>
                      <span class="text-[9px] text-white/40 font-bold uppercase">TEMP-999</span>
                    </div>
                    <span class="text-base shrink-0">💳</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Kiosk Camera Overlaid Layer -->
    <div class="fixed inset-0 bg-[#061129]/95 z-[9500] flex items-center justify-center p-4" v-if="kioskShowCam">
      <div class="w-full max-w-sm bg-[#0c1c3e] border border-white/10 rounded-2xl overflow-hidden shadow-2xl flex flex-col p-6">
        <div class="flex justify-between items-center mb-4">
          <span class="text-white text-sm font-bold">📷 {{ t.camLabel }}</span>
          <span 
            class="px-2 py-0.5 rounded-md text-[10px] font-bold"
            :class="camFacing === 'user' ? 'bg-blue-500/20 text-blue-300' : 'bg-orange-500/20 text-orange-300'"
          >
            {{ camFacing === 'user' ? t.camFront : t.camBack }}
          </span>
        </div>

        <!-- Camera Preview Screen -->
        <div class="relative w-full aspect-[4/3] rounded-xl overflow-hidden border border-white/15 bg-black/60 flex items-center justify-center shadow-inner">
          <div v-if="camError" class="p-6 text-center text-red-300">
            <span class="text-3xl">📷</span>
            <p class="text-xs leading-relaxed font-semibold mt-2">{{ camError }}</p>
            <div class="flex gap-2 justify-center mt-4">
              <button class="bg-white/10 hover:bg-white/15 border border-white/10 text-white text-xs font-bold px-3 py-1.5 rounded-lg active:scale-95 transition" @click="skipKioskPhoto">{{ t.skipPhoto }}</button>
              <button class="bg-[#11254e] border border-white/10 text-white text-xs font-bold px-3 py-1.5 rounded-lg active:scale-95 transition" @click="cancelKioskCheckin">{{ t.cancel }}</button>
            </div>
          </div>

          <div v-else class="w-full h-full relative">
            <video 
              ref="kioskVideoRef" 
              autoplay 
              playsinline 
              muted
              class="w-full h-full object-cover transition-opacity duration-300"
              :class="[
                camFacing === 'user' ? 'scale-x-[-1]' : '',
                camSwitching ? 'opacity-30' : 'opacity-100'
              ]"
            ></video>
            <!-- Camera frame corners -->
            <div class="absolute w-4 h-4 border-2 border-primary-light top-4 left-4 border-r-0 border-b-0"></div>
            <div class="absolute w-4 h-4 border-2 border-primary-light top-4 right-4 border-l-0 border-b-0"></div>
            <div class="absolute w-4 h-4 border-2 border-primary-light bottom-4 left-4 border-r-0 border-t-0"></div>
            <div class="absolute w-4 h-4 border-2 border-primary-light bottom-4 right-4 border-l-0 border-t-0"></div>
            
            <div v-if="camSwitching" class="absolute inset-0 bg-black/40 flex items-center justify-center text-white text-xs gap-1.5">
              <span>🔄</span> {{ t.camSwitching }}
            </div>
          </div>
          <canvas ref="kioskCanvasRef" style="display: none;"></canvas>
        </div>

        <!-- Camera Controls -->
        <div class="flex items-center justify-between mt-6" v-if="!camError">
          <button 
            class="bg-white/5 hover:bg-white/10 border border-white/10 text-white/80 hover:text-white text-xs font-bold px-4 py-2 rounded-xl active:scale-95 transition" 
            @click="cancelKioskCheckin"
          >
            {{ t.cancel }}
          </button>
          
          <button 
            class="w-14 h-14 rounded-full bg-gradient-to-r from-emerald-500 to-emerald-600 border-4 border-emerald-500/20 text-white text-lg flex items-center justify-center shadow-lg hover:shadow-emerald-500/20 active:scale-90 transition disabled:opacity-40 disabled:pointer-events-none"
            :disabled="camSwitching" 
            @click="shootKioskPhoto"
          >
            📸
          </button>

          <button 
            class="bg-white/5 hover:bg-white/10 border border-white/10 text-white/80 hover:text-white text-xs font-bold px-4 py-2 rounded-xl active:scale-95 transition" 
            @click="skipKioskPhoto"
          >
            {{ t.skipPhoto }}
          </button>
          
          <button 
            v-if="hasMultipleCams" 
            class="bg-white/5 border border-white/10 hover:bg-white/10 text-white p-2 rounded-xl active:scale-95 transition"
            :disabled="camSwitching"
            @click="flipKioskCamera(kioskVideoRef)"
          >
            🔄
          </button>
        </div>

        <div class="text-[10px] text-white/40 mt-3 font-semibold text-center" v-if="!camError">
          {{ camFacing === 'user' ? t.camHintFront : t.camHintBack }}
        </div>
      </div>
    </div>
  </div>
</template>
