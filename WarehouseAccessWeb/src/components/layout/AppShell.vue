<script setup>
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ROUTE_NAMES } from '../../constants/routes'

const route = useRoute()
const drawer = ref(true)

const navItems = [
  { title: 'Home', path: '/', name: ROUTE_NAMES.home, icon: 'mdi-view-dashboard-outline' },
  { title: 'Entry', path: '/entry', name: ROUTE_NAMES.entry, icon: 'mdi-account-plus-outline' },
  { title: 'Monitor', path: '/monitor', name: ROUTE_NAMES.monitor, icon: 'mdi-radar' },
  { title: 'History', path: '/history', name: ROUTE_NAMES.history, icon: 'mdi-history' },
  { title: 'Users', path: '/settings/users', name: ROUTE_NAMES.settingsUsers, icon: 'mdi-account-group-outline' },
  { title: 'Department', path: '/settings/departments', name: ROUTE_NAMES.settingsDepartments, icon: 'mdi-office-building-outline' },
  { title: 'Contact Depts', path: '/settings/contact-depts', name: ROUTE_NAMES.settingsContactDepts, icon: 'mdi-card-account-details-outline' },
  { title: 'Purpose', path: '/settings/purposes', name: ROUTE_NAMES.settingsPurposes, icon: 'mdi-target' }
]

const pageTitle = computed(() => route.meta?.title || 'Warehouse Access')
</script>

<template>
  <v-navigation-drawer v-model="drawer" width="288" class="soft-panel" border="0">
    <div class="pa-4">
      <div class="d-flex align-center ga-3 mb-4">
        <v-avatar color="primary" class="glow-pill"><v-icon icon="mdi-shield-key-outline" /></v-avatar>
        <div>
          <div class="text-subtitle-1 font-weight-bold">AccessHub</div>
          <div class="text-caption text-faded">Neo Enterprise</div>
        </div>
      </div>
      <v-list nav density="comfortable" bg-color="transparent">
        <v-list-item
          v-for="item in navItems"
          :key="item.path"
          :to="item.path"
          :active="route.path === item.path"
          rounded="xl"
          color="info"
          class="mb-2"
        >
          <template #prepend><v-icon :icon="item.icon" /></template>
          <v-list-item-title>{{ item.title }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </div>
  </v-navigation-drawer>

  <v-app-bar elevation="0" class="glass-panel" border="0" height="72">
    <v-app-bar-nav-icon @click="drawer = !drawer" />
    <div>
      <div class="text-caption text-faded">Operations Workspace</div>
      <div class="text-subtitle-1 font-weight-bold">{{ pageTitle }}</div>
    </div>
    <v-spacer />
    <v-chip color="success" variant="tonal" class="mr-2">Online</v-chip>
    <v-chip color="info" variant="tonal">Synced</v-chip>
  </v-app-bar>

  <v-main>
    <v-container fluid class="pa-6">
      <slot />
    </v-container>
  </v-main>
</template>
