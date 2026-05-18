<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>用户管理</span>
          <el-button type="primary" @click="openDialog(null)" v-if="authStore.role === 'Admin'">
            新增用户
          </el-button>
        </div>
      </template>

      <el-table :data="list" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="username" label="用户名" width="120" />
        <el-table-column prop="displayName" label="姓名" width="100" />
        <el-table-column prop="role" label="角色" width="100">
          <template #default="{ row }">
            <el-tag :type="roleTagType(row.role)" size="small">{{ roleLabel(row.role) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="departmentName" label="科室" width="120" />
        <el-table-column prop="title" label="职称" width="100" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="isActive" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'" size="small">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="lastLoginAt" label="最后登录" width="180">
          <template #default="{ row }">
            {{ row.lastLoginAt ? dayjs(row.lastLoginAt).format('YYYY-MM-DD HH:mm') : '从未登录' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" v-if="authStore.role === 'Admin'">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="openDialog(row)">编辑</el-button>
            <el-button
              type="danger" link size="small"
              @click="handleToggleActive(row)"
              :disabled="row.username === 'admin'"
            >
              {{ row.isActive ? '禁用' : '启用' }}
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <div style="display: flex; justify-content: flex-end; margin-top: 16px;">
        <el-pagination
          v-model:current-page="page"
          :page-size="pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="fetchData"
        />
      </div>
    </el-card>

    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑用户' : '新增用户'"
      width="560px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" :disabled="isEdit" placeholder="登录账号" />
        </el-form-item>
        <el-form-item label="密码" :prop="isEdit ? '' : 'password'">
          <el-input
            v-model="form.password"
            type="password"
            show-password
            :placeholder="isEdit ? '留空则不修改' : '请输入密码'"
          />
        </el-form-item>
        <el-form-item label="姓名" prop="displayName">
          <el-input v-model="form.displayName" placeholder="显示名称" />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-select v-model="form.role" placeholder="选择角色" style="width: 100%;">
            <el-option label="系统管理员" :value="1" />
            <el-option label="医生" :value="2" />
            <el-option label="护士" :value="3" />
            <el-option label="收银员" :value="4" />
            <el-option label="药剂师" :value="5" />
            <el-option label="院长" :value="6" />
          </el-select>
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="form.phone" placeholder="手机号" />
        </el-form-item>
        <el-form-item label="所属科室">
          <el-select v-model="form.departmentId" placeholder="选择科室" clearable style="width: 100%;">
            <el-option
              v-for="dept in departments"
              :key="dept.id"
              :label="dept.name"
              :value="dept.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="职称">
          <el-input v-model="form.title" placeholder="如: 主任医师" />
        </el-form-item>
        <el-form-item label="挂号费">
          <el-input-number v-model="form.consultationFee" :min="0" :precision="2" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="日接诊上限">
          <el-input-number v-model="form.maxDailyPatients" :min="1" :max="200" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="启用状态" v-if="isEdit">
          <el-switch v-model="form.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
/**
 * 用户管理页面
 * 提供用户列表查看、新增用户、编辑用户、启用/禁用功能
 * 仅Admin角色可操作
 */
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'
import dayjs from 'dayjs'

const authStore = useAuthStore()

const loading = ref(false)
const list = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const departments = ref([])

const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(null)
const submitting = ref(false)
const formRef = ref()

const form = reactive({
  username: '',
  password: '',
  displayName: '',
  role: 2,
  phone: '',
  departmentId: null,
  title: '',
  consultationFee: null,
  maxDailyPatients: null,
  isActive: true
})

const rules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 20, message: '用户名长度3-20个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少6位', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '请输入姓名', trigger: 'blur' }
  ],
  role: [
    { required: true, message: '请选择角色', trigger: 'change' }
  ]
}

const roleLabelMap = {
  Admin: '管理员', Doctor: '医生', Nurse: '护士',
  Cashier: '收银员', Pharmacist: '药剂师', Boss: '院长'
}

const roleTagTypeMap = {
  Admin: 'danger', Doctor: 'primary', Nurse: 'warning',
  Cashier: 'info', Pharmacist: 'success', Boss: ''
}

/**
 * 获取角色中文名
 * @param {string} role - 角色枚举名
 * @returns {string} 角色中文名
 */
function roleLabel(role) {
  return roleLabelMap[role] || role
}

/**
 * 获取角色标签颜色类型
 * @param {string} role - 角色枚举名
 * @returns {string} Element Plus tag type
 */
function roleTagType(role) {
  return roleTagTypeMap[role] || 'info'
}

/**
 * 获取用户列表
 */
async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/users', { params: { page: page.value, pageSize: pageSize.value } })
    list.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

/**
 * 获取科室列表用于下拉选择
 */
async function fetchDepartments() {
  try {
    const res = await api.get('/departments')
    departments.value = res.data
  } catch { /* ignore */ }
}

/**
 * 打开新增/编辑弹窗
 * @param {Object|null} row - 行数据，null表示新增
 */
function openDialog(row) {
  formRef.value?.resetFields()
  if (row) {
    isEdit.value = true
    editId.value = row.id
    form.username = row.username
    form.password = ''
    form.displayName = row.displayName
    form.role = roleValue(row.role)
    form.phone = row.phone || ''
    form.departmentId = null
    form.title = row.title || ''
    form.consultationFee = null
    form.maxDailyPatients = null
    form.isActive = row.isActive
  } else {
    isEdit.value = false
    editId.value = null
    form.username = ''
    form.password = ''
    form.displayName = ''
    form.role = 2
    form.phone = ''
    form.departmentId = null
    form.title = ''
    form.consultationFee = null
    form.maxDailyPatients = null
    form.isActive = true
  }
  dialogVisible.value = true
}

/**
 * 将角色字符串转为枚举数值
 * @param {string} roleStr - 角色字符串
 * @returns {number} 角色枚举值
 */
function roleValue(roleStr) {
  const map = { Admin: 1, Doctor: 2, Nurse: 3, Cashier: 4, Pharmacist: 5, Boss: 6 }
  return map[roleStr] || 2
}

/**
 * 提交新增/编辑用户
 */
async function handleSubmit() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      const payload = {
        displayName: form.displayName,
        phone: form.phone || null,
        role: form.role,
        title: form.title || null,
        departmentId: form.departmentId,
        isActive: form.isActive,
        consultationFee: form.consultationFee,
        maxDailyPatients: form.maxDailyPatients
      }
      if (form.password) {
        payload.password = form.password
      }
      await api.put(`/users/${editId.value}`, payload)
      ElMessage.success('用户更新成功')
    } else {
      await api.post('/users', {
        username: form.username,
        password: form.password,
        displayName: form.displayName,
        phone: form.phone || null,
        role: form.role,
        title: form.title || null,
        departmentId: form.departmentId,
        consultationFee: form.consultationFee,
        maxDailyPatients: form.maxDailyPatients
      })
      ElMessage.success('用户创建成功')
    }
    dialogVisible.value = false
    await fetchData()
  } finally {
    submitting.value = false
  }
}

/**
 * 启用/禁用用户
 * @param {Object} row - 用户行数据
 */
async function handleToggleActive(row) {
  const action = row.isActive ? '禁用' : '启用'
  try {
    await ElMessageBox.confirm(`确定要${action}用户「${row.displayName}」吗？`, '提示', {
      type: 'warning'
    })
    await api.put(`/users/${row.id}`, {
      displayName: row.displayName,
      phone: row.phone || null,
      role: roleValue(row.role),
      title: row.title || null,
      departmentId: null,
      isActive: !row.isActive,
      consultationFee: null,
      maxDailyPatients: null
    })
    ElMessage.success(`${action}成功`)
    await fetchData()
  } catch { /* 用户取消 */ }
}

onMounted(() => {
  fetchData()
  fetchDepartments()
})
</script>
