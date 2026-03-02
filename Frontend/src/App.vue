<script setup>
import { RouterView, useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { ref, onMounted, onUnmounted } from 'vue'

const authStore = useAuthStore()
const router = useRouter()

const isDarkMode = ref(false)

const toggleDarkMode = () => {
  isDarkMode.value = !isDarkMode.value
  if (isDarkMode.value) {
    document.documentElement.setAttribute('data-theme', 'dark')
  } else {
    document.documentElement.removeAttribute('data-theme')
  }
}

const INACTIVITY_TIME = 120
const WARNING_TIME = 60

const isIdleModalOpen = ref(false)
const countdown = ref(WARNING_TIME)

let idleTimer = null
let countdownInterval = null

const resetTimers = () => {
  if (isIdleModalOpen.value) return

  clearTimeout(idleTimer)
  clearInterval(countdownInterval)

  if (authStore.isAuthenticated) {
    idleTimer = setTimeout(showIdleWarning, INACTIVITY_TIME * 1000)
  }
}

const showIdleWarning = () => {
  isIdleModalOpen.value = true
  countdown.value = WARNING_TIME

  countdownInterval = setInterval(() => {
    countdown.value--
    if (countdown.value <= 0) {
      handleTimeoutLogout()
    }
  }, 1000)
}

const stayConnected = () => {
  isIdleModalOpen.value = false
  clearInterval(countdownInterval)
  resetTimers()
}

const handleTimeoutLogout = () => {
  isIdleModalOpen.value = false
  clearInterval(countdownInterval)
  handleLogout()
}

const handleLogout = () => {
  authStore.logout()
  router.push('/')
}

onMounted(() => {
  if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
    isDarkMode.value = true
    document.documentElement.setAttribute('data-theme', 'dark')
  }

  const events = ['mousemove', 'keydown', 'scroll', 'click']
  events.forEach((event) => window.addEventListener(event, resetTimers))

  resetTimers()
})

onUnmounted(() => {
  const events = ['mousemove', 'keydown', 'scroll', 'click']
  events.forEach((event) => window.removeEventListener(event, resetTimers))

  clearTimeout(idleTimer)
  clearInterval(countdownInterval)
})
</script>

<template>
  <div v-if="isIdleModalOpen" class="modal-overlay">
    <div class="modal-content">
      <h3 class="modal-title">¿Sigues ahí?</h3>
      <p class="modal-text">Por seguridad, su sesión se cerrará en:</p>
      <div class="countdown">{{ countdown }} segundos</div>
      <div class="modal-actions">
        <button @click="handleTimeoutLogout" class="btn-outline-danger">Cerrar conexión</button>
        <button @click="stayConnected" class="btn-primary">Quedarme</button>
      </div>
    </div>
  </div>

  <header v-if="authStore.isAuthenticated" class="navbar">
    <div class="brand">
      <span class="logo-icon">🏢</span> Inmobiliaria <span class="highlight">App</span>
    </div>

    <nav class="nav-links">
      <RouterLink to="/simulador" class="nav-item" active-class="active-link"
        >Simulador Hipotecario</RouterLink
      >
      <RouterLink to="/historial" class="nav-item" active-class="active-link"
        >Mi Historial</RouterLink
      >
    </nav>

    <div class="user-info">
      <button
        @click="toggleDarkMode"
        class="theme-toggle"
        type="button"
        title="Alternar Modo Oscuro"
      >
        {{ isDarkMode ? '☀️' : '🌙' }}
      </button>
      <div class="avatar">{{ authStore.user?.username?.charAt(0).toUpperCase() || 'U' }}</div>
      <span class="username">Hola, {{ authStore.user?.username || 'Usuario' }}</span>
      <button @click="handleLogout" class="btn-logout" title="Cerrar Sesión">Salir</button>
    </div>
  </header>

  <main class="app-container">
    <RouterView />
  </main>
</template>

<style>
:root {
  --bg-color: #f8fafc;
  --panel-bg: #ffffff;
  --text-color: #334155;
  --text-title: #0f172a;
  --text-muted: #64748b;
  --border-color: #e2e8f0;

  --primary: #3b82f6;
  --primary-hover: #2563eb;

  --fieldset-border: #cbd5e1;
  --input-bg: #ffffff;
  --kpi-bg: #f1f5f9;
  --kpi-highlight: #2563eb;

  --table-head: #1e293b;
  --table-head-text: #ffffff;
  --table-hover: #f8fafc;

  --cuota-bg: #ecfdf5;
  --cuota-color: #059669;
  --summary-bg: #fffbeb;
  --summary-border: #fde68a;

  --nav-bg: #ffffff;
  --nav-border: #e2e8f0;
  --nav-text: #64748b;
  --nav-text-hover: #3b82f6;
}

[data-theme='dark'] {
  --bg-color: #121212;
  --panel-bg: #1e1e1e;
  --text-color: #e0e0e0;
  --text-title: #ffffff;
  --text-muted: #a0a0a0;
  --border-color: #333333;

  --primary: #3b82f6;
  --primary-hover: #60a5fa;

  --fieldset-border: #333333;
  --input-bg: #121212;
  --kpi-bg: #121212;
  --kpi-highlight: #3b82f6;

  --table-head: #121212;
  --table-head-text: #ffffff;
  --table-hover: #1a1a1a;

  --cuota-bg: #064e3b;
  --cuota-color: #34d399;
  --summary-bg: #1e1e1e;
  --summary-border: #333333;

  --nav-bg: #1e1e1e;
  --nav-border: #333333;
  --nav-text: #a0a0a0;
  --nav-text-hover: #60a5fa;
}

body {
  margin: 0;
  padding: 0;
  font-family: 'Inter', system-ui, Avenir, Helvetica, Arial, sans-serif;
  background-color: var(--bg-color);
  color: var(--text-color);
  -webkit-font-smoothing: antialiased;
  transition:
    background-color 0.3s,
    color 0.3s;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background-color: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}

.modal-content {
  background: var(--panel-bg);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 2.5rem;
  text-align: center;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
  max-width: 400px;
  width: 90%;
  color: var(--text-color);
}

.modal-title {
  color: var(--text-title);
  margin-top: 0;
  font-size: 1.5rem;
}

.modal-text {
  color: var(--text-muted);
}

.countdown {
  font-size: 2.5rem;
  font-weight: 800;
  color: #ef4444;
  margin: 1rem 0 2rem;
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-variant-numeric: tabular-nums;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
}

.btn-primary {
  background-color: var(--primary);
  color: white;
  border: none;
  padding: 0.6rem 1.2rem;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.3s;
}

.btn-primary:hover {
  background-color: var(--primary-hover);
}

.btn-outline-danger {
  background-color: transparent;
  color: #ef4444;
  border: 2px solid #ef4444;
  padding: 0.6rem 1.2rem;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s;
}
.btn-outline-danger:hover {
  background-color: #ef4444;
  color: white;
}

.navbar {
  background: var(--nav-bg);
  border-bottom: 1px solid var(--nav-border);
  padding: 0 2rem;
  height: 70px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  position: sticky;
  top: 0;
  z-index: 50;
  transition:
    background 0.3s,
    border-color 0.3s;
}

.brand {
  font-weight: 800;
  font-size: 1.25rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--text-title);
}

.logo-icon {
  font-size: 1.5rem;
}
.highlight {
  color: var(--primary);
}

.nav-links {
  display: flex;
  gap: 2rem;
  height: 100%;
}

.nav-item {
  text-decoration: none;
  color: var(--nav-text);
  font-weight: 500;
  display: flex;
  align-items: center;
  border-bottom: 3px solid transparent;
  transition: all 0.2s ease;
}

.nav-item:hover {
  color: var(--nav-text-hover);
}

.active-link {
  color: var(--primary);
  border-bottom-color: var(--primary);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.theme-toggle {
  background: transparent;
  border: 1px solid var(--border-color);
  color: var(--text-color);
  border-radius: 50%;
  width: 36px;
  height: 36px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.3s;
}
.theme-toggle:hover {
  background: var(--border-color);
}

.avatar {
  background-color: var(--primary);
  color: white;
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  font-size: 0.9rem;
}

.username {
  font-size: 0.95rem;
  font-weight: 500;
  color: var(--text-title);
}

.btn-logout {
  padding: 0.4rem 0.8rem;
  background-color: transparent;
  color: #ef4444;
  border: 1px solid #f87171;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-logout:hover {
  background-color: #fef2f2;
}
[data-theme='dark'] .btn-logout:hover {
  background-color: #450a0a;
}

.app-container {
  padding: 2rem;
  max-width: 1280px;
  margin: 0 auto;
}

@media (max-width: 768px) {
  .navbar {
    flex-direction: column;
    height: auto;
    padding: 1rem;
    gap: 1rem;
  }
  .nav-links {
    gap: 1rem;
  }
}
</style>
