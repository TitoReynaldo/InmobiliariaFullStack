import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LoginView from '../views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'login', component: LoginView, meta: { guest: true } },
    {
      path: '/register',
      name: 'register',
      component: () => import('../views/RegisterView.vue'),
      meta: { guest: true },
    },
    {
      path: '/simulador',
      name: 'simulador',
      component: () => import('../views/SimuladorView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/historial',
      name: 'historial',
      component: () => import('../views/HistorialView.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth && !authStore.isAuthenticated) return next({ name: 'login' })
  if (to.meta.guest && authStore.isAuthenticated) return next({ name: 'simulador' })
  next()
})

export default router
