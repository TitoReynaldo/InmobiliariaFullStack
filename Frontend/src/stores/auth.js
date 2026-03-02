import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import api from '../services/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('jwt_token') || null)
  const user = ref(
    localStorage.getItem('user_data') ? JSON.parse(localStorage.getItem('user_data')) : null,
  )

  const isAuthenticated = computed(() => !!token.value)

  const login = async (username, password) => {
    try {
      const response = await api.post('/auth/login', { username, password })

      token.value = response.data.token
      user.value = { username: response.data.username, rol: response.data.rol }

      localStorage.setItem('jwt_token', token.value)
      localStorage.setItem('user_data', JSON.stringify(user.value))

      return { success: true }
    } catch (error) {
      console.error('Error Login:', error)
      return {
        success: false,
        message: error.response?.data?.message || 'Credenciales incorrectas o error de conexión.',
      }
    }
  }

  const register = async (username, password, rol = 'Cliente') => {
    try {
      await api.post('/auth/register', { username, password, rol })
      return { success: true }
    } catch (error) {
      console.error('Error Registro:', error)
      return {
        success: false,
        message: error.response?.data || 'El usuario ya existe o los datos son inválidos.',
      }
    }
  }

  const logout = () => {
    token.value = null
    user.value = null
    localStorage.clear()
    sessionStorage.clear()
    window.location.reload()
  }

  return { token, user, isAuthenticated, login, register, logout }
})