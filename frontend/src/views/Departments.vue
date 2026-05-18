<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>科室管理</span>
          <el-button type="primary" @click="openDialog(null)" v-if="authStore.role === 'Admin'">
            新增科室
          </el-button>
        </div>
      </template>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="name" label="科室名称" min-width="150" />
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
        <el-table-column prop="sortOrder" label="排序" width="80" />
        <el-table-column label="操作" width="150" fixed="right" v-if="authStore.role === 'Admin'">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="openDialog(row)">编辑</el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑科室' : '新增科室'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="科室名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="科室描述" />
        </el-form-item>
        <el-form-item label="排序" prop="sortOrder">
          <el-input-number v-model="form.sortOrder" :min="0" :max="999" />
        </el-form-item>
        <el-form-item label="启用" v-if="isEdit">
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
 * 科室管理页面
 * 提供科室列表查看、新增、编辑、软删除功能
 */
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const loading = ref(false)
const list = ref([])
const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(null)
const submitting = ref(false)
const formRef = ref()

const form = reactive({
  name: '',
  description: '',
  sortOrder: 0,
  isActive: true
})

const rules = {
  name: [{ required: true, message: '请输入科室名称', trigger: 'blur' }],
  sortOrder: [{ required: true, message: '请输入排序号', trigger: 'blur' }]
}

/**
 * 获取科室列表
 */
async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/departments')
    list.value = res.data
  } finally {
    loading.value = false
  }
}

/**
 * 打开新增/编辑弹窗
 * @param {Object|null} row - 行数据，null为新增
 */
function openDialog(row) {
  formRef.value?.resetFields()
  if (row) {
    isEdit.value = true
    editId.value = row.id
    form.name = row.name
    form.description = row.description || ''
    form.sortOrder = row.sortOrder
    form.isActive = true
  } else {
    isEdit.value = false
    editId.value = null
    form.name = ''
    form.description = ''
    form.sortOrder = 0
    form.isActive = true
  }
  dialogVisible.value = true
}

/**
 * 提交新增/编辑科室
 */
async function handleSubmit() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      await api.put(`/departments/${editId.value}`, form)
      ElMessage.success('科室更新成功')
    } else {
      await api.post('/departments', form)
      ElMessage.success('科室创建成功')
    }
    dialogVisible.value = false
    await fetchData()
  } finally {
    submitting.value = false
  }
}

/**
 * 软删除科室
 * @param {Object} row - 科室行数据
 */
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm(`确定要删除科室「${row.name}」吗？`, '提示', { type: 'warning' })
    await api.delete(`/departments/${row.id}`)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* 用户取消 */ }
}

onMounted(fetchData)
</script>
