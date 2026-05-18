<template>
  <div class="main-layout">
    <div class="sidebar">
      <div class="logo">门诊管理系统</div>
      <el-menu
        :default-active="route.path"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
        router
        style="flex: 1; border-right: none;"
      >
        <el-menu-item index="/dashboard">
          <el-icon><Odometer /></el-icon>
          <span>工作台</span>
        </el-menu-item>
        <el-menu-item
          v-for="menu in filteredMenus"
          :key="menu.path"
          :index="menu.path"
        >
          <el-icon><component :is="menu.icon" /></el-icon>
          <span>{{ menu.title }}</span>
        </el-menu-item>
      </el-menu>
    </div>

    <div class="main-area">
      <div class="header">
        <el-breadcrumb>
          <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
          <el-breadcrumb-item v-if="route.meta.title">{{ route.meta.title }}</el-breadcrumb-item>
        </el-breadcrumb>
        <div style="display: flex; align-items: center; gap: 12px;">
          <el-tag size="small">{{ roleLabel }}</el-tag>
          <el-dropdown @command="handleCommand">
            <span style="cursor: pointer;">
              {{ authStore.user?.displayName || '用户' }}
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="changePassword">修改密码</el-dropdown-item>
                <el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
      <div class="content">
        <router-view />
      </div>
    </div>

    <el-dialog v-model="passwordDialogVisible" title="修改密码" width="400px" :close-on-click-modal="false">
      <el-form ref="pwdFormRef" :model="pwdForm" :rules="pwdRules" label-width="80px">
        <el-form-item label="原密码" prop="oldPassword">
          <el-input v-model="pwdForm.oldPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="pwdForm.newPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input v-model="pwdForm.confirmPassword" type="password" show-password />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="passwordDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="pwdSubmitting" @click="handleChangePassword">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
/**
 * 主布局组件
 * 包含侧边栏导航、顶部面包屑、用户下拉菜单和修改密码功能
 * 根据用户角色自动过滤可见菜单项
 */
import { ref, reactive, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { ElMessage } from 'element-plus'
import api from '@/api'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const roleLabelMap = {
  Admin: '管理员', Doctor: '医生', Nurse: '护士',
  Cashier: '收银员', Pharmacist: '药剂师', Boss: '院长'
}

const roleLabel = computed(() => roleLabelMap[authStore.role] || '未知')

const menuItems = [
  { path: '/appointments', title: '挂号管理', icon: 'Document', roles: ['Admin', 'Nurse', 'Cashier'] },
  { path: '/patients', title: '患者管理', icon: 'UserFilled', roles: ['Admin', 'Doctor', 'Nurse', 'Cashier'] },
  { path: '/departments', title: '科室管理', icon: 'Setting', roles: ['Admin'] },
  { path: '/charge-items', title: '收费项目', icon: 'Tickets', roles: ['Admin'] },
  { path: '/drugs', title: '药品字典', icon: 'FirstAidKit', roles: ['Admin', 'Pharmacist'] },
  { path: '/schedules', title: '排班管理', icon: 'Calendar', roles: ['Admin'] },
  { path: '/doctor-workstation', title: '医生工作站', icon: 'Monitor', roles: ['Admin', 'Doctor'] },
  { path: '/payments', title: '收费管理', icon: 'Money', roles: ['Admin', 'Cashier'] },
  { path: '/pharmacy', title: '药房管理', icon: 'Goods', roles: ['Admin', 'Pharmacist'] },
  { path: '/users', title: '用户管理', icon: 'Avatar', roles: ['Admin'] }
]

const filteredMenus = computed(() =>
  menuItems.filter(m => m.roles.includes(authStore.role))
)

const passwordDialogVisible = ref(false)
const pwdSubmitting = ref(false)
const pwdFormRef = ref()

const pwdForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

const validateConfirmPassword = (rule, value, callback) => {
  if (value !== pwdForm.newPassword) {
    callback(new Error('两次密码输入不一致'))
  } else {
    callback()
  }
}

const pwdRules = {
  oldPassword: [{ required: true, message: '请输入原密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少6位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
}

/**
 * 处理用户下拉菜单命令
 * @param {string} command - 命令名 (logout/changePassword)
 */
function handleCommand(command) {
  if (command === 'logout') {
    authStore.logout()
    router.push('/login')
  } else if (command === 'changePassword') {
    pwdForm.oldPassword = ''
    pwdForm.newPassword = ''
    pwdForm.confirmPassword = ''
    pwdFormRef.value?.resetFields()
    passwordDialogVisible.value = true
  }
}

/**
 * 提交修改密码
 */
async function handleChangePassword() {
  const valid = await pwdFormRef.value.validate().catch(() => false)
  if (!valid) return

  pwdSubmitting.value = true
  try {
    await api.post('/auth/change-password', {
      oldPassword: pwdForm.oldPassword,
      newPassword: pwdForm.newPassword
    })
    ElMessage.success('密码修改成功，请重新登录')
    passwordDialogVisible.value = false
    authStore.logout()
    router.push('/login')
  } finally {
    pwdSubmitting.value = false
  }
}
</script>

<style scoped>
.main-layout {
  display: flex;
  height: 100vh;
  overflow: hidden;
}

.sidebar {
  width: 220px;
  background: #304156;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.logo {
  height: 60px;
  line-height: 60px;
  text-align: center;
  color: #fff;
  font-size: 18px;
  font-weight: bold;
  letter-spacing: 2px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.main-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.header {
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  background: #fff;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  z-index: 1;
  flex-shrink: 0;
}

.content {
  flex: 1;
  padding: 20px;
  overflow-y: auto;
  background: #f0f2f5;
}
</style>
