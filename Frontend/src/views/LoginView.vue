<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const handleLogin = async () => {
  error.value = ''
  loading.value = true

  if (!username.value || !password.value) {
    error.value = 'Por favor, complete todos los campos.'
    loading.value = false
    return
  }

  try {
    const success = await authStore.login(username.value, password.value)

    if (success.success) {
      router.push({ name: 'simulador' })
    } else {
      error.value = success.message
    }
  } catch (e) {
    error.value = 'Error de conexión con el servidor.'
    console.error(e)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-container">
    <div class="auth-card">
      <div class="auth-header">
        <h1>Bienvenido</h1>
        <p>Sistema de Créditos Hipotecarios</p>
      </div>

      <form @submit.prevent="handleLogin" class="auth-form">
        <div class="form-group">
          <label for="username">Usuario</label>
          <input
            id="username"
            type="text"
            v-model="username"
            placeholder="Ingrese su usuario"
            :disabled="loading"
            class="form-control"
            required
          />
        </div>

        <div class="form-group">
          <label for="password">Contraseña</label>
          <input
            id="password"
            type="password"
            v-model="password"
            placeholder="******"
            :disabled="loading"
            class="form-control"
            required
          />
        </div>

        <div v-if="error" class="alert alert-error">
          {{ error }}
        </div>

        <button type="submit" class="btn-primary" :disabled="loading">
          <span v-if="loading">Ingresando...</span>
          <span v-else>Iniciar Sesión</span>
        </button>
      </form>

      <div class="auth-footer">
        <p>
          ¿No tienes cuenta? <router-link to="/register" class="link">Regístrate aquí</router-link>
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.auth-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f4f7f6;
  padding: 1rem;
}

.auth-card {
  background: white;
  padding: 2.5rem;
  border-radius: 12px;
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.1),
    0 2px 4px -1px rgba(0, 0, 0, 0.06);
  width: 100%;
  max-width: 400px;
}

.auth-header {
  text-align: center;
  margin-bottom: 2rem;
}

.auth-header h1 {
  color: #2c3e50;
  margin: 0;
  font-size: 1.8rem;
}

.auth-header p {
  color: #64748b;
  margin: 0.5rem 0 0;
}

.form-group {
  margin-bottom: 1.25rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  color: #374151;
  font-weight: 500;
  font-size: 0.9rem;
}

.form-control {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 1rem;
  transition: border-color 0.2s;
  box-sizing: border-box;
}

.form-control:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.btn-primary {
  width: 100%;
  padding: 0.75rem;
  background-color: #2563eb;
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  font-size: 1rem;
  cursor: pointer;
  transition: background-color 0.2s;
}

.btn-primary:hover:not(:disabled) {
  background-color: #1d4ed8;
}

.btn-primary:disabled {
  background-color: #93c5fd;
  cursor: not-allowed;
}

.alert {
  padding: 0.75rem;
  border-radius: 6px;
  margin-bottom: 1.25rem;
  font-size: 0.9rem;
  text-align: center;
}

.alert-error {
  background-color: #fef2f2;
  color: #ef4444;
  border: 1px solid #fee2e2;
}

.auth-footer {
  margin-top: 1.5rem;
  text-align: center;
  font-size: 0.85rem;
  color: #6b7280;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb;
}

.link {
  color: #2563eb;
  text-decoration: none;
  font-weight: 600;
}

.link:hover {
  text-decoration: underline;
}
</style>