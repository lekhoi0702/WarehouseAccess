import { APP_CONFIG } from '../../constants/app-config'

async function request(path, options = {}) {
  const controller = new AbortController()
  const timeout = setTimeout(() => controller.abort(), APP_CONFIG.apiTimeoutMs)

  try {
    const isFormData = options.body instanceof FormData
    const baseHeaders = isFormData ? {} : { 'Content-Type': 'application/json' }
    const response = await fetch(`${APP_CONFIG.apiBaseUrl}${path}`, {
      ...options,
      headers: {
        ...baseHeaders,
        ...(options.headers || {})
      },
      signal: controller.signal
    })

    const contentType = response.headers.get('content-type') || ''
    const isJson = contentType.includes('application/json')
    const payload = isJson ? await response.json() : null

    if (!response.ok) {
      return {
        success: false,
        data: null,
        message: payload?.message || `HTTP ${response.status}`
      }
    }

    if (!isJson) {
      return {
        success: false,
        data: null,
        message: 'Expected JSON response but received non-JSON content.'
      }
    }

    return payload
  } catch (error) {
    return {
      success: false,
      data: null,
      message: error?.message || 'Network error'
    }
  } finally {
    clearTimeout(timeout)
  }
}

export const apiClient = {
  get(path) {
    return request(path, { method: 'GET' })
  },
  post(path, body) {
    const payload = body instanceof FormData ? body : JSON.stringify(body)
    return request(path, { method: 'POST', body: payload })
  },
  put(path, body) {
    return request(path, { method: 'PUT', body: JSON.stringify(body) })
  },
  delete(path) {
    return request(path, { method: 'DELETE' })
  }
}
