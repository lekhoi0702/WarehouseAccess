import { apiClient } from './api/api-client'

export function loginByCard(cardNumber) {
  return apiClient.post('/WarehouseAccess/Auth/LoginByCard', { cardNumber })
}

