<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>收费项目管理</span>
          <el-button type="primary" @click="openDialog(null)" v-if="authStore.role === 'Admin'">
            新增项目
          </el-button>
        </div>
      </template>

      <el-form :inline="true" style="margin-bottom: 16px;">
        <el-form-item>
          <el-input v-model="keyword" placeholder="名称/编码" clearable @clear="fetchData" @keyup.enter="fetchData" style="width: 200px;" />
        </el-form-item>
        <el-form-item>
          <el-select v-model="categoryFilter" placeholder="分类" clearable @change="fetchData" style="width: 140px;">
            <el-option label="挂号" value="挂号" />
            <el-option label="诊疗" value="诊疗" />
            <el-option label="检查" value="检查" />
            <el-option label="治疗" value="治疗" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchData">查询</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="code" label="编码" width="100" />
        <el-table-column prop="name" label="项目名称" min-width="160" />
        <el-table-column prop="category" label="分类" width="80">
          <template #default="{ row }">
            <el-tag size="small">{{ row.category }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="price" label="单价" width="100">
          <template #default="{ row }">¥{{ row.price.toFixed(2) }}</template>
        </el-table-column>
        <el-table-column prop="unit" label="单位" width="70" />
        <el-table-column prop="isActive" label="状态" width="70">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'danger'" size="small">
              {{ row.isActive ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right" v-if="authStore.role === 'Admin'">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="openDialog(row)">编辑</el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
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
      :title="isEdit ? '编辑收费项目' : '新增收费项目'"
      width="550px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
        <el-form-item label="编码" prop="code">
          <el-input v-model="form.code" :disabled="isEdit" placeholder="项目编码" />
        </el-form-item>
        <el-form-item label="名称" prop="name">
          <el-input v-model="form.name" placeholder="项目名称" />
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-select v-model="form.category" placeholder="选择分类" style="width: 100%;">
            <el-option label="挂号" value="挂号" />
            <el-option label="诊疗" value="诊疗" />
            <el-option label="检查" value="检查" />
            <el-option label="治疗" value="治疗" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="单价" prop="price">
          <el-input-number v-model="form.price" :min="0" :precision="2" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="单位" prop="unit">
          <el-input v-model="form.unit" placeholder="如: 次/盒/项" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
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
 * 收费项目管理页面
 * 提供收费项目的列表查看、新增、编辑、软删除功能
 */
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const loading = ref(false)
const list = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const keyword = ref('')
const categoryFilter = ref('')
const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(null)
const submitting = ref(false)
const formRef = ref()

const form = reactive({
  code: '',
  name: '',
  category: '诊疗',
  price: 0,
  unit: '次',
  remark: '',
  isActive: true
})

const rules = {
  code: [{ required: true, message: '请输入编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入名称', trigger: 'blur' }],
  category: [{ required: true, message: '请选择分类', trigger: 'change' }],
  price: [{ required: true, message: '请输入单价', trigger: 'blur' }]
}

/**
 * 获取收费项目列表
 */
async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/chargeitems', {
      params: { page: page.value, pageSize: pageSize.value, keyword: keyword.value, category: categoryFilter.value }
    })
    list.value = res.data.items
    total.value = res.data.total
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
    form.code = row.code
    form.name = row.name
    form.category = row.category
    form.price = row.price
    form.unit = row.unit || ''
    form.remark = row.remark || ''
    form.isActive = row.isActive
  } else {
    isEdit.value = false
    editId.value = null
    form.code = ''
    form.name = ''
    form.category = '诊疗'
    form.price = 0
    form.unit = '次'
    form.remark = ''
    form.isActive = true
  }
  dialogVisible.value = true
}

/**
 * 提交新增/编辑
 */
async function handleSubmit() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    const payload = {
      code: form.code,
      name: form.name,
      category: form.category,
      price: form.price,
      unit: form.unit,
      remark: form.remark,
      isActive: form.isActive
    }
    if (isEdit.value) {
      await api.put(`/chargeitems/${editId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await api.post('/chargeitems', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    await fetchData()
  } finally {
    submitting.value = false
  }
}

/**
 * 软删除收费项目
 * @param {Object} row - 行数据
 */
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm(`确定要删除项目「${row.name}」吗？`, '提示', { type: 'warning' })
    await api.delete(`/chargeitems/${row.id}`)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* 用户取消 */ }
}

onMounted(fetchData)
</script>
