import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'
import { createVuetify } from 'vuetify'

const colors = {
  primary: '#1A8DFF',
  secondary: '#57B4FF',
  success: '#10B981',
  warning: '#F59E0B',
  error: '#EF4444',
  info: '#00BCD4',
  background: '#08121F',
  surface: '#0E1D30'
}

export const vuetify = createVuetify({
  theme: {
    defaultTheme: 'dark',
    themes: {
      dark: { dark: true, colors },
      light: {
        dark: false,
        colors: {
          primary: '#0F7AE5',
          secondary: '#1FA3FF',
          success: '#10B981',
          warning: '#F59E0B',
          error: '#EF4444',
          info: '#0EA5E9',
          background: '#F4F8FC',
          surface: '#FFFFFF'
        }
      }
    }
  },
  defaults: {
    VCard: { rounded: 'xl', elevation: 0 },
    VBtn: { rounded: 'pill', height: 40 },
    VTextField: { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' },
    VSelect: { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' },
    VTextarea: { variant: 'outlined', density: 'comfortable', hideDetails: 'auto' },
    VDialog: { scrim: 'rgba(3,10,20,0.72)' },
    VDataTable: { density: 'comfortable' },
    VChip: { rounded: 'pill' }
  }
})
