import { reactive } from 'vue'

const state = reactive({
  isAuthenticated: false
})

export function useAuthState() {
  return state
}
