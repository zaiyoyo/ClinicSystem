import { createRouter, createWebHashHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Login.vue'),
    meta: { requiresAuth: false }
  },
  {
    path: '/',
    component: () => import('@/layouts/MainLayout.vue'),
    meta: { requiresAuth: true },
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard.vue'),
        meta: { title: '工作台', roles: ['Admin', 'Doctor', 'Nurse', 'Cashier', 'Pharmacist', 'Boss'] }
      },
      {
        path: 'appointments',
        name: 'Appointments',
        component: () => import('@/views/Appointments.vue'),
        meta: { title: '挂号管理', roles: ['Admin', 'Nurse', 'Cashier'] }
      },
      {
        path: 'patients',
        name: 'Patients',
        component: () => import('@/views/Patients.vue'),
        meta: { title: '患者管理', roles: ['Admin', 'Doctor', 'Nurse', 'Cashier'] }
      },
      {
        path: 'departments',
        name: 'Departments',
        component: () => import('@/views/Departments.vue'),
        meta: { title: '科室管理', roles: ['Admin'] }
      },
      {
        path: 'users',
        name: 'Users',
        component: () => import('@/views/Users.vue'),
        meta: { title: '用户管理', roles: ['Admin'] }
      },
      {
        path: 'charge-items',
        name: 'ChargeItems',
        component: () => import('@/views/ChargeItems.vue'),
        meta: { title: '收费项目', roles: ['Admin'] }
      },
      {
        path: 'drugs',
        name: 'Drugs',
        component: () => import('@/views/Drugs.vue'),
        meta: { title: '药品字典', roles: ['Admin', 'Pharmacist'] }
      },
      {
        path: 'schedules',
        name: 'ScheduleManagement',
        component: () => import('@/views/ScheduleManagement.vue'),
        meta: { title: '排班管理', roles: ['Admin'] }
      },
      {
        path: 'doctor-workstation',
        name: 'DoctorWorkstation',
        component: () => import('@/views/DoctorWorkstation.vue'),
        meta: { title: '医生工作站', roles: ['Admin', 'Doctor'] }
      },
      {
        path: 'payments',
        name: 'Payments',
        component: () => import('@/views/Payments.vue'),
        meta: { title: '收费管理', roles: ['Admin', 'Cashier'] }
      },
      {
        path: 'pharmacy',
        name: 'Pharmacy',
        component: () => import('@/views/Pharmacy.vue'),
        meta: { title: '药房管理', roles: ['Admin', 'Pharmacist'] }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

/**
 * 路由守卫：检查登录状态与角色权限
 * - 未登录（无token或残余token无角色）跳转登录页
 * - 无角色权限跳转登录页重新认证
 */
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth === false) {
    next()
    return
  }

  if (!authStore.token || !authStore.role) {
    next('/login')
    return
  }

  if (to.meta.roles && !to.meta.roles.includes(authStore.role)) {
    next('/login')
    return
  }

  next()
})

export default router
