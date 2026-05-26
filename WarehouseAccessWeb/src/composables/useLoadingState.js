export function useLoadingState() {
  return {
    loading: false,
    start() {
      this.loading = true
    },
    stop() {
      this.loading = false
    }
  }
}
