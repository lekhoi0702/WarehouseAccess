import App from '../App.vue'
import '../styles/index.css'
import { router } from '../router'

export function createWarehouseApp(createApp) {
  const app = createApp(App)
  app.use(router)
  return app
}
