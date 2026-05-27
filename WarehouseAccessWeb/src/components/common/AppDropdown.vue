<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  modelValue: {
    type: [String, Number],
    default: ''
  },
  items: {
    type: Array,
    default: () => []
  },
  valueKey: {
    type: String,
    default: 'value'
  },
  labelKey: {
    type: String,
    default: 'label'
  },
  placeholder: {
    type: String,
    default: 'Select...'
  },
  searchPlaceholder: {
    type: String,
    default: 'Search...'
  },
  disabled: {
    type: Boolean,
    default: false
  },
  searchable: {
    type: Boolean,
    default: false
  },
  clearable: {
    type: Boolean,
    default: true
  }
})

const emit = defineEmits(['update:modelValue'])

const dropdownOpen = ref(false)
const dropdownRef = ref(null)
const searchKeyword = ref('')

const normalizedItems = computed(() => {
  return (props.items || []).map((item) => ({
    value: item?.[props.valueKey] ?? '',
    label: item?.[props.labelKey] ?? ''
  }))
})

const selectedLabel = computed(() => {
  const selected = normalizedItems.value.find((item) => String(item.value) === String(props.modelValue ?? ''))
  return selected?.label || props.placeholder
})

const filteredItems = computed(() => {
  if (!props.searchable) return normalizedItems.value
  const keywordValue = searchKeyword.value.trim().toLowerCase()
  if (!keywordValue) return normalizedItems.value
  return normalizedItems.value.filter((item) =>
    String(item.label || '').toLowerCase().includes(keywordValue) ||
    String(item.value || '').toLowerCase().includes(keywordValue)
  )
})

function toggleDropdown() {
  if (props.disabled) return
  dropdownOpen.value = !dropdownOpen.value
}

function selectValue(value) {
  emit('update:modelValue', value)
  dropdownOpen.value = false
}

function handleClickOutside(event) {
  if (!dropdownRef.value) return
  if (!dropdownRef.value.contains(event.target)) {
    dropdownOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', handleClickOutside)
})

watch(() => props.modelValue, () => {
  // keep typed search lightweight across selections
  if (!dropdownOpen.value) {
    searchKeyword.value = ''
  }
})
</script>

<template>
  <div ref="dropdownRef" class="relative">
    <button
      type="button"
      :disabled="disabled"
      class="w-full bg-white border border-slate-200 rounded-xl px-4 py-3 md:py-3.5 text-sm md:text-base text-left outline-none focus:border-primary/50 flex items-center justify-between disabled:opacity-50"
      @click="toggleDropdown"
    >
      <span class="truncate">{{ selectedLabel }}</span>
      <span class="text-slate-400 text-sm md:text-base">▾</span>
    </button>

    <div v-if="dropdownOpen" class="absolute z-[120] mt-2 w-full rounded-xl border border-slate-200 bg-white shadow-lg p-2 space-y-2">
      <input
        v-if="searchable"
        type="text"
        v-model="searchKeyword"
        :placeholder="searchPlaceholder"
        class="w-full border border-slate-200 rounded-lg px-3 py-2.5 md:py-3 text-sm md:text-base outline-none focus:border-primary/50"
      />

      <div class="max-h-44 overflow-auto">
        <button
          v-if="clearable"
          type="button"
          class="w-full text-left px-3 py-2.5 md:py-3 rounded-lg text-sm md:text-base hover:bg-slate-100 transition"
          @click="selectValue('')"
        >
          {{ placeholder }}
        </button>

        <button
          v-for="item in filteredItems"
          :key="`${item.value}`"
          type="button"
          class="w-full text-left px-3 py-2.5 md:py-3 rounded-lg text-sm md:text-base hover:bg-slate-100 transition"
          @click="selectValue(item.value)"
        >
          {{ item.label }}
        </button>

        <p v-if="filteredItems.length === 0" class="px-3 py-2.5 md:py-3 text-sm md:text-base text-slate-400">No data found.</p>
      </div>
    </div>
  </div>
</template>
