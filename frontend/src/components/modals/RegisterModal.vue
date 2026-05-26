<script setup>
import { ref, reactive, computed, onUnmounted, nextTick } from 'vue';
import { useCamera } from '../../composables/useCamera';

const props = defineProps({
  t: Object,
  departments: Array,
  purposes: Array,
  employees: Object,
  defaultContact: Object
});

const emit = defineEmits(['submit', 'close']);

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

const videoRef = ref(null);
const canvasRef = ref(null);

// Employee Lookup States
const empError = ref("");
const empFound = ref(false);

const camError = computed(() => {
  if (camErrKey.value === 'camera_error') {
    return props.t.camError || "Camera unavailable. Check permissions.";
  }
  return "";
});

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

function openCameraFlow() {
  openCamera(null); // Will bind nextTick
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

const VISITOR_TYPES = ["vendor", "brand", "audit", "staff"];
const TYPE_COLORS = { vendor: "#F97316", brand: "#22C55E", audit: "#EF4444", staff: "#3B82F6" };
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

        <!-- Camera box -->
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

    <!-- Main Register Form Wrapper -->
    <div class="bg-white border border-slate-200 rounded-3xl p-7 w-full max-w-[440px] text-slate-800 shadow-2xl relative max-h-[90vh] overflow-y-auto" v-else>
      <!-- Header -->
      <div class="flex justify-between items-start mb-4">
        <div>
          <h2 class="text-lg font-extrabold text-slate-900 m-0">{{ t.regTitle }}</h2>
          <p class="text-xs text-slate-500 mt-0.5">{{ t.step }} {{ step }} {{ t.of }} 2</p>
        </div>
        <button class="text-slate-400 hover:text-slate-600 text-lg transition" @click="emit('close')">✕</button>
      </div>

      <!-- Progress Indicators -->
      <div class="flex gap-2 mb-6">
        <div 
          v-for="i in [0, 1]" 
          :key="i"
          class="flex-1 h-1 rounded-full transition-all duration-300"
          :class="step > i ? 'bg-primary' : 'bg-slate-200'"
        ></div>
      </div>

      <!-- Step 1 Form fields -->
      <div class="space-y-4" v-if="step === 1">
        <!-- Emp lookup -->
        <div class="bg-slate-50 border border-slate-200/60 rounded-xl p-3.5 space-y-2">
          <label class="block text-xs font-bold text-slate-500">🔍 {{ t.empId }}</label>
          <div class="flex gap-2">
            <input 
              type="text" 
              v-model="form.empId" 
              placeholder="E001..." 
              @keydown.enter="lookupEmployee"
              class="flex-1 bg-white border border-slate-200 rounded-lg px-3 py-1.5 text-sm outline-none focus:border-primary/50"
            />
            <button 
              class="bg-indigo-500 hover:bg-indigo-600 text-white text-xs font-bold px-4 py-1.5 rounded-lg transition active:scale-95"
              @click="lookupEmployee"
            >
              {{ t.lookupBtn }}
            </button>
          </div>
          <p class="text-xs text-red-500 font-semibold mt-1" v-if="empError">⚠ {{ empError }}</p>
          <p class="text-xs text-emerald-500 font-bold mt-1" v-if="empFound">✓ {{ form.name }} · {{ form.department }}</p>
        </div>

        <!-- Name Input -->
        <div class="flex flex-col gap-1 text-left">
          <input 
            type="text" 
            v-model="form.name" 
            :placeholder="t.name + ' *'" 
            class="w-full bg-white border rounded-xl px-4 py-2.5 text-sm outline-none focus:border-primary/50"
            :class="errors.name ? 'border-red-500 focus:border-red-500 focus:ring-red-100' : 'border-slate-200'"
          />
          <p class="text-[11px] text-red-500 font-semibold mt-0.5 pl-1" v-if="errors.name">{{ errors.name }}</p>
        </div>

        <!-- Company Input -->
        <div class="flex flex-col gap-1 text-left">
          <input 
            type="text" 
            v-model="form.company" 
            :placeholder="t.company + ' *'" 
            class="w-full bg-white border rounded-xl px-4 py-2.5 text-sm outline-none focus:border-primary/50"
            :class="errors.company ? 'border-red-500 focus:border-red-500 focus:ring-red-100' : 'border-slate-200'"
          />
          <p class="text-[11px] text-red-500 font-semibold mt-0.5 pl-1" v-if="errors.company">{{ errors.company }}</p>
        </div>

        <!-- Visitor Types Badges Selection -->
        <div class="flex flex-wrap gap-2 py-1">
          <button 
            v-for="v in VISITOR_TYPES" 
            :key="v"
            type="button"
            class="px-3.5 py-1.5 rounded-full border text-xs font-bold transition duration-200"
            :style="{
              borderColor: form.type === v ? TYPE_COLORS[v] : '#e2e8f0',
              color: form.type === v ? TYPE_COLORS[v] : '#64748b',
              backgroundColor: form.type === v ? TYPE_COLORS[v] + '12' : 'transparent'
            }"
            @click="form.type = v"
          >
            {{ t.types[v] }}
          </button>
        </div>

        <!-- Department select -->
        <div class="flex flex-col text-left">
          <select 
            v-model="form.department" 
            class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-sm outline-none cursor-pointer focus:border-primary/50"
          >
            <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
          </select>
        </div>

        <!-- Contact default readonly info -->
        <div class="bg-indigo-50/50 border border-indigo-100 rounded-xl p-3.5 flex justify-between items-center">
          <div class="text-left">
            <label class="block text-[10px] font-bold text-slate-500 uppercase tracking-wide">{{ t.contact }}</label>
            <span class="text-sm font-black text-indigo-700">{{ form.contact }}</span>
          </div>
          <span class="text-[10px] font-bold text-indigo-400 flex items-center gap-1">⚙ {{ t.settings }}</span>
        </div>

        <!-- Purpose selection badges -->
        <div class="flex flex-wrap gap-2 py-1">
          <button 
            v-for="p in purposes" 
            :key="p"
            type="button"
            class="px-3 py-1 rounded-lg border text-[11px] transition-all duration-200 font-semibold"
            :class="[
              form.purpose === p 
                ? 'border-primary bg-primary/5 text-primary' 
                : 'border-slate-200 text-slate-500 hover:bg-slate-50'
            ]"
            @click="form.purpose = p"
          >
            {{ p }}
          </button>
        </div>

        <!-- Note Textarea -->
        <div class="flex flex-col text-left">
          <textarea 
            v-model="form.note" 
            :placeholder="t.note" 
            rows="2" 
            class="w-full bg-white border border-slate-200 rounded-xl px-4 py-2.5 text-sm outline-none resize-y focus:border-primary/50"
          ></textarea>
        </div>

        <!-- Next Button -->
        <button 
          class="w-full bg-gradient-to-r from-primary to-primary-light text-white text-sm font-bold py-3 rounded-xl shadow-lg hover:shadow-primary/20 transition duration-200 active:scale-[0.98] mt-4" 
          @click="nextStep"
        >
          {{ t.nextPhoto }}
        </button>
      </div>

      <!-- Step 2: Camera Capture Review -->
      <div class="space-y-6" v-if="step === 2">
        <!-- Photo snapshot viewer -->
        <div class="relative w-full aspect-[4/3] bg-slate-50 border border-slate-200 rounded-2xl overflow-hidden flex items-center justify-center shadow-inner">
          <div class="w-full h-full relative" v-if="photo">
            <img :src="photo" alt="entry snapshot" class="w-full h-full object-cover" />
            <span class="absolute top-3 left-3 bg-emerald-500 text-white text-[10px] font-bold px-2.5 py-1 rounded-full shadow-sm">
              {{ t.photoTaken }}
            </span>
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
            <span class="text-4xl group-hover:scale-110 transition duration-200">📷</span>
            <span class="text-xs font-bold text-slate-500 mt-2">{{ t.photoHint }}</span>
            <span class="text-[10px] text-slate-400 font-semibold mt-1">{{ t.photoSub }}</span>
          </div>
        </div>

        <!-- Form summary details list -->
        <div class="bg-slate-50 border border-slate-200/60 rounded-xl p-4 text-xs text-slate-600 font-medium space-y-2 text-left">
          <div><span class="text-slate-400 font-bold uppercase tracking-wider">{{ t.name }}: </span><strong class="text-slate-800 text-sm pl-1">{{ form.name }}</strong></div>
          <div><span class="text-slate-400 font-bold uppercase tracking-wider">{{ t.company }}: </span><span class="text-slate-700 pl-1">{{ form.company }}</span></div>
          <div><span class="text-slate-400 font-bold uppercase tracking-wider">{{ t.purpose }}: </span><span class="text-slate-700 pl-1">{{ form.purpose }}</span></div>
          <div><span class="text-slate-400 font-bold uppercase tracking-wider">{{ t.contact }}: </span><span class="text-slate-700 pl-1">{{ form.contact }}</span></div>
          <div><span class="text-slate-400 font-bold uppercase tracking-wider">{{ t.dept }}: </span><span class="text-slate-700 pl-1">{{ form.department }}</span></div>
        </div>

        <!-- Footer Actions -->
        <div class="flex gap-3">
          <button 
            class="flex-1 bg-slate-100 hover:bg-slate-200/80 border border-slate-200 text-slate-600 text-xs font-bold py-3 rounded-xl active:scale-[0.98] transition"
            @click="step = 1"
          >
            {{ t.back }}
          </button>
          
          <button 
            class="flex-[2] bg-gradient-to-r from-emerald-500 to-emerald-600 text-white text-sm font-bold py-3 rounded-xl shadow-lg hover:shadow-emerald-500/20 active:scale-[0.98] transition"
            @click="handleSubmit"
          >
            {{ t.confirmEntry }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
