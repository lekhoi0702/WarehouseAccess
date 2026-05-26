import { ref } from 'vue'

export function useCapturePolling({ getStatus, onPhoto, intervalMs = 1500 }) {
  const isPolling = ref(false)
  let timerId = null

  function stopPolling() {
    if (timerId) {
      clearInterval(timerId)
      timerId = null
    }
    isPolling.value = false
  }

  function startPolling(sessionId) {
    stopPolling()
    isPolling.value = true

    timerId = setInterval(async () => {
      const response = await getStatus(sessionId)
      if (!response?.success) {
        return
      }

      if (response.data?.hasPhoto && response.data?.photoBase64) {
        onPhoto(response.data.photoBase64)
        stopPolling()
      }
    }, intervalMs)
  }

  return {
    isPolling,
    startPolling,
    stopPolling
  }
}
