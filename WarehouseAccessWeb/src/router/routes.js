import { ROUTE_NAMES } from '../constants/routes'
import HomeView from '../views/HomeView.vue'
import EntryView from '../views/EntryView.vue'
import MonitorView from '../views/MonitorView.vue'
import HistoryView from '../views/HistoryView.vue'
import SettingsView from '../views/SettingsView.vue'
import SettingsUsersView from '../views/SettingsUsersView.vue'
import SettingsDepartmentsView from '../views/SettingsDepartmentsView.vue'
import SettingsContactDeptsView from '../views/SettingsContactDeptsView.vue'
import SettingsPurposesView from '../views/SettingsPurposesView.vue'

export const routes = [
  { path: '/', name: ROUTE_NAMES.home, component: HomeView, meta: { shell: true, title: 'Home' } },
  { path: '/entry', name: ROUTE_NAMES.entry, component: EntryView, meta: { shell: true, title: 'Entry' } },
  { path: '/monitor', name: ROUTE_NAMES.monitor, component: MonitorView, meta: { shell: true, title: 'Monitor' } },
  { path: '/history', name: ROUTE_NAMES.history, component: HistoryView, meta: { shell: true, title: 'History' } },
  { path: '/settings', name: ROUTE_NAMES.settings, component: SettingsView, meta: { shell: true, title: 'Settings' } },
  { path: '/settings/users', name: ROUTE_NAMES.settingsUsers, component: SettingsUsersView, meta: { shell: true, title: 'Settings • Users' } },
  { path: '/settings/departments', name: ROUTE_NAMES.settingsDepartments, component: SettingsDepartmentsView, meta: { shell: true, title: 'Settings • Department' } },
  { path: '/settings/contact-depts', name: ROUTE_NAMES.settingsContactDepts, component: SettingsContactDeptsView, meta: { shell: true, title: 'Settings • Contact Depts' } },
  { path: '/settings/purposes', name: ROUTE_NAMES.settingsPurposes, component: SettingsPurposesView, meta: { shell: true, title: 'Settings • Purpose' } }
]
