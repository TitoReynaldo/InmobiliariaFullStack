<template>
  <div v-if="alertas && alertas.length > 0" class="alertas-container">
    <div v-for="(alerta, index) in alertas" :key="index" class="alerta-item" :class="getAlertaClass(alerta)">
      {{ alerta }}
    </div>
  </div>
</template>

<script setup>
import { defineProps } from 'vue'

const props = defineProps({
  alertas: {
    type: Array,
    default: () => []
  }
})

const getAlertaClass = (alerta) => {
  if (alerta.includes('SBS')) return 'alerta-warning'
  if (alerta.includes('VIVIENDA')) return 'alerta-danger'
  return 'alerta-info'
}
</script>

<style scoped>
.alertas-container { margin: 15px 0; }
.alerta-item { padding: 10px; margin-bottom: 5px; border-radius: 4px; font-weight: bold; }
.alerta-warning { background-color: #fff3cd; color: #856404; border: 1px solid #ffeeba; }
.alerta-danger { background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
.alerta-info { background-color: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb; }
</style>
