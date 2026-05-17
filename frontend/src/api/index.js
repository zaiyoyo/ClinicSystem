import axios from 'axios'
import { ElMessage } from 'element-plus'

const api = axios.create({
  baseURL: '/api',
  timeout: 30000
})

// 响应拦截器：统一错误处理
api.interceptors.response.use(
  response => response,
  error => {
    const status = error.response?.status
    const msg = error.response?.data?.message || error.message

    if (status === 401) {
      // Token 过期，清除登录状态
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.hash = '#/login'
      ElMessage.error('登录已过期，请重新登录')
    } else if (status === 403) {
      ElMessage.error('权限不足')
    } else if (status === 500) {
      ElMessage.error('服务器错误')
    } else if (msg) {
      ElMessage.error(msg)
    }

    return Promise.reject(error)
  }
)

export default api
