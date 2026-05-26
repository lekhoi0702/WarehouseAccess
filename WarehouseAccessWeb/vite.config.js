import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import fs from 'node:fs'
import path from 'node:path'

const certDir = path.resolve(process.cwd(), '.cert')
const certFile = path.join(certDir, 'dev-cert.pem')
const keyFilePem = path.join(certDir, 'dev-key.pem')
const keyFileKey = path.join(certDir, 'dev-cert.key')
const keyFile = fs.existsSync(keyFilePem) ? keyFilePem : keyFileKey
const hasHttpsFiles = fs.existsSync(certFile) && fs.existsSync(keyFile)

export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true })],
  server: {
    host: true,
    https: hasHttpsFiles
      ? {
          cert: fs.readFileSync(certFile),
          key: fs.readFileSync(keyFile)
        }
      : false,
    proxy: {
      '/WarehouseAccess': {
        target: 'https://localhost:7146',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
