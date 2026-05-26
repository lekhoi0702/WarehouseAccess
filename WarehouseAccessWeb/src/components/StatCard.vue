<script setup>
const props = defineProps({
  title: { type: String, required: true },
  value: { type: [String, Number], required: true },
  subtitle: { type: String, required: true }
})

const iconMap = {
  Inside: 'mdi-account-group-outline',
  'Checked Out Today': 'mdi-door-open',
  'Total Today': 'mdi-chart-timeline-variant'
}

const colorMap = {
  Inside: 'info',
  'Checked Out Today': 'success',
  'Total Today': 'primary'
}
</script>

<template>
  <v-card class="premium-card">
    <v-card-text class="pa-5">
      <div class="d-flex align-center justify-space-between mb-3">
        <div class="text-caption text-faded text-uppercase" style="letter-spacing:.08em">{{ props.title }}</div>
        <v-avatar :color="colorMap[props.title] || 'primary'" variant="tonal" size="36">
          <v-icon :icon="iconMap[props.title] || 'mdi-chart-box-outline'" size="18" />
        </v-avatar>
      </div>
      <div class="text-h3 font-weight-bold" style="line-height:1">{{ props.value }}</div>
      <div class="text-body-2 text-faded mt-2">{{ props.subtitle }}</div>
    </v-card-text>
  </v-card>
</template>
