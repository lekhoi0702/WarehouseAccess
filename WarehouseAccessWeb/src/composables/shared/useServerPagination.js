import { ref } from 'vue'

export function useServerPagination(initialPage = 1, initialPageSize = 20) {
  const page = ref(initialPage)
  const pageSize = ref(initialPageSize)
  const total = ref(0)
  const totalPages = ref(0)

  function applyFromResponse(payload) {
    page.value = payload?.page || 1
    pageSize.value = payload?.pageSize || pageSize.value
    total.value = payload?.total || 0
    totalPages.value = payload?.totalPages || 0
  }

  function resetPage() {
    page.value = 1
  }

  function clearTotals() {
    total.value = 0
    totalPages.value = 0
  }

  return {
    page,
    pageSize,
    total,
    totalPages,
    applyFromResponse,
    resetPage,
    clearTotals
  }
}
