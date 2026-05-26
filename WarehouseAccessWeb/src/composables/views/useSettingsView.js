import { reactive, ref, onMounted } from 'vue'

export function useSettingsView() {
  const title = 'System & Terminal Settings'

  // Reactive state for settings
  const settings = reactive({
    isDarkMode: false,
    kioskId: 'Kiosk #1',
    requireEntryPhoto: true,
    requireExitPhoto: false,
    allowEmployeeLookup: true,
    defaultTimeoutHours: '8',
    syncIntervalSeconds: '7'
  })

  const saveFeedback = ref('')

  function applyTheme() {
    if (settings.isDarkMode) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  // Initialize values from localStorage or default
  function loadSettings() {
    const saved = localStorage.getItem('accesshub_settings')
    if (saved) {
      try {
        const parsed = JSON.parse(saved)
        Object.assign(settings, parsed)
        applyTheme()
      } catch (e) {
        console.error('Failed to parse settings', e)
      }
    }
  }

  // Save all states to localstorage
  function saveSettings() {
    localStorage.setItem('accesshub_settings', JSON.stringify(settings))
    applyTheme()
    
    saveFeedback.value = 'Settings successfully saved to local storage.'
    setTimeout(() => {
      saveFeedback.value = ''
    }, 3000)
  }

  onMounted(() => {
    loadSettings()
  })

  return {
    title,
    settings,
    saveFeedback,
    saveSettings
  }
}
