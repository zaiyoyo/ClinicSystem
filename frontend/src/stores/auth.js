import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))

  const isLoggedIn = computed(() => !!token.value)
  const role = computed(() => user.value?.role || '')

  async function login(username, password) {
    const res = await api.post('/auth/login', { username, password })
    token.value = res.data.token
    user.value = res.data.user
    localStorage.setItem('token', res.data.token)
    localStorage.setItem('user', JSON.stringify(res.data.user))
    // 设置 axios 默认头部
    api.defaults.headers.common['Authorization'] = `Bearer ${res.data.token}`
    return res.data
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    delete api.defaults.headers.common['Authorization']
  }

  // 从 localStorage 恢复 token 到 axios
  if (token.value) {
    api.defaults.headers.common['Authorization'] = `Bearer ${token.value}`
  }

  return { token, user, isLoggedIn, role, login, logout }
})
