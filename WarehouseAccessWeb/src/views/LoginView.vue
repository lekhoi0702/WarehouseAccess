<script setup>
import { nextTick, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import headerLogo from '../assets/logo-jiahsin-co-chu.png'
import bgImage from '../assets/background.jpg'
import { login } from '../stores/auth.store'
import { useSweetAlert } from '../composables/useSweetAlert'

const route = useRoute()
const router = useRouter()

const cardNumber = ref('')
const loading = ref(false)
const cardInputRef = ref(null)
const { showError } = useSweetAlert()

function focusCardInput() {
  nextTick(() => {
    cardInputRef.value?.focus()
  })
}

onMounted(() => {
  focusCardInput()
})

async function submitLogin() {
  const value = cardNumber.value.trim()
  if (!value || loading.value) return

  loading.value = true
  const response = await login(value)
  loading.value = false

  if (!response?.success) {
    await showError(response?.message || 'Unable to login')
    cardNumber.value = ''
    focusCardInput()
    return
  }

  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/monitor'
  router.replace(redirect)
}
</script>

<template>
  <div class="min-h-screen flex bg-slate-950 text-slate-100 overflow-hidden font-sans">
    <!-- Left Panel: Graphic & Sci-Fi Security Vibe -->
    <div class="hidden lg:flex lg:w-7/12 relative items-center justify-center bg-slate-950 overflow-hidden border-r border-slate-800/40">
      <!-- Background Image -->
      <img :src="bgImage" class="absolute inset-0 w-full h-full object-cover opacity-50 mix-blend-luminosity scale-105 filter blur-[0.5px]" alt="Security Background" />
      
      <!-- Gradient overlay -->
      <div class="absolute inset-0 bg-gradient-to-tr from-[#0a1e3d]/90 via-slate-950/95 to-[#0e4391]/30"></div>
      
      <!-- Animated futuristic grids/circles -->
      <div class="absolute inset-0 bg-[linear-gradient(to_right,#ffffff03_1px,transparent_1px),linear-gradient(to_bottom,#ffffff03_1px,transparent_1px)] bg-[size:40px_40px] [mask-image:radial-gradient(ellipse_60%_50%_at_50%_50%,#000_70%,transparent_100%)]"></div>
      
      <!-- Tech elements & Info text -->
      <div class="relative z-10 p-12 max-w-xl text-left animate-fade-in-up">
        <div class="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-500/10 border border-blue-500/25 text-blue-400 text-xs font-semibold mb-6 tracking-wider uppercase">
          <span class="w-2 h-2 rounded-full bg-blue-400 animate-ping"></span> Access Control
        </div>
        
        <h2 class="text-4xl md:text-5xl font-black tracking-tight leading-tight text-white mb-6">
          Finished Goods Warehouse <br/>
          <span class="text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-emerald-400">Inbound/Outbound Management</span>
        </h2>
      </div>
    </div>

    <!-- Right Panel: Login Card -->
    <div class="w-full lg:w-5/12 flex items-center justify-center p-6 md:p-12 bg-slate-900/60 relative">
      <!-- Glow bubble background -->
      <div class="absolute w-[300px] h-[300px] rounded-full bg-blue-500/10 blur-[120px] top-1/4 right-1/4 pointer-events-none"></div>
      <div class="absolute w-[200px] h-[200px] rounded-full bg-emerald-500/5 blur-[80px] bottom-1/4 left-1/4 pointer-events-none"></div>

      <!-- Login container -->
      <div class="w-full max-w-md space-y-8 relative z-10 animate-fade-in-up delay-1">
        <!-- Logo block -->
        <div class="flex items-center gap-3 sm:gap-4 mb-8">
          <img class="h-9 sm:h-10 w-auto object-contain shrink-0" :src="headerLogo" alt="JIA HSIN" />
          <span class="h-8 sm:h-9 w-px bg-white/20 shrink-0"></span>
          <div class="min-w-0 text-left">
            <h1 class="text-base sm:text-lg md:text-xl font-black whitespace-nowrap leading-tight tracking-wide text-white uppercase">WAREHOUSE ACCESS</h1>
            <p class="text-[9px] sm:text-[10px] md:text-xs text-slate-400 font-medium mt-0.5 truncate">Finished Goods Warehouse Access System</p>
          </div>
        </div>

        <!-- Credentials Input form -->
        <div class="space-y-5">
          <div class="space-y-2 text-left">
            <label class="text-xs font-bold text-slate-400 uppercase tracking-wider">Access Card Number</label>
            <div class="relative flex items-center">
              <input
                ref="cardInputRef"
                v-model="cardNumber"
                :disabled="loading"
                @keydown.enter.prevent="submitLogin"
                class="w-full bg-slate-950/60 border border-slate-700/80 rounded-xl pl-11 pr-4 py-3.5 text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500/30 transition-all duration-200"
                placeholder="Scan card or enter ID"
              />
              <div class="absolute left-4 text-slate-500">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                </svg>
              </div>
            </div>
          </div>

          <button
            @click="submitLogin"
            :disabled="loading"
            class="w-full py-3.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-semibold shadow-lg shadow-blue-500/20 active:scale-[0.98] transition-all duration-200 disabled:opacity-50 disabled:pointer-events-none flex items-center justify-center gap-2"
          >
            <span v-if="loading" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            <span>{{ loading ? 'Authenticating...' : 'Sign In' }}</span>
          </button>
        </div>

        <!-- Footer -->
        <p class="text-xs text-slate-500 text-center">
          &copy; 2026 JIA HSIN Co., Ltd. All rights reserved.
        </p>
      </div>
    </div>
  </div>
</template>
