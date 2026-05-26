<script setup>
import { useI18n } from '../../composables/useI18n';

const { lang, t, setLanguage } = useI18n();

defineEmits(['open-kiosk', 'open-settings', 'open-register']);
</script>

<template>
  <header class="w-full bg-gradient-to-r from-[#0a3575] to-[#0e4391] text-white py-4 border-b-[3.5px] border-accent shadow-xl">
    <div class="max-w-[1240px] mx-auto px-6 flex justify-between items-center">
      <!-- Left: Logo & Titles -->
      <div class="flex items-center gap-4">
        <img class="h-9 w-auto object-contain" src="/logo.png" alt="JIA HSIN" />
        <div class="w-[1.5px] h-9 bg-white/25"></div>
        <div class="font-sans leading-tight">
          <h1 class="text-xl font-extrabold tracking-wide font-sans m-0">{{ t.sysTitle }}</h1>
          <p class="text-[11px] text-white/65 font-medium mt-0.5">{{ t.sysSub }}</p>
        </div>
      </div>

      <!-- Right: Navigation Controls -->
      <div class="flex items-center gap-4">
        <!-- Language Pills -->
        <div class="flex bg-white/10 p-0.5 rounded-lg border border-white/15">
          <button 
            v-for="(label, lCode) in { zh: '中文', en: 'EN', vi: 'VI' }" 
            :key="lCode"
            :class="[
              'px-3.5 py-1 text-xs font-bold rounded-md transition-all duration-200',
              lang === lCode ? 'bg-white text-primary shadow-sm' : 'bg-transparent text-white/80 hover:text-white'
            ]"
            @click="setLanguage(lCode)"
          >
            {{ label }}
          </button>
        </div>

        <!-- Terminal Kiosk Button -->
        <button 
          class="bg-white/8 text-white border border-white/15 text-xs font-semibold px-4 py-2 rounded-lg hover:bg-white/15 transition duration-200 flex items-center gap-1.5 active:scale-95"
          @click="$emit('open-kiosk')"
        >
          <span class="text-sm">💻</span> 自助感應登記
        </button>

        <!-- Settings Button -->
        <button 
          class="bg-white/8 text-white border border-white/15 text-xs font-semibold px-4 py-2 rounded-lg hover:bg-white/15 transition duration-200 flex items-center gap-1.5 active:scale-95"
          @click="$emit('open-settings')"
        >
          <span class="text-sm">⚙</span> {{ t.settings }}
        </button>

        <!-- Primary Register Button -->
        <button 
          class="bg-white text-primary text-xs font-extrabold px-5 py-2.5 rounded-lg hover:translate-y-[-1px] transition-all duration-200 shadow-md hover:shadow-lg active:scale-95"
          @click="$emit('open-register')"
        >
          {{ t.register }}
        </button>
      </div>
    </div>
  </header>
</template>
