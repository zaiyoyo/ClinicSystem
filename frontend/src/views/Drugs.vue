<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>药品字典管理</span>
          <el-button type="primary" @click="openDialog(null)" v-if="authStore.role === 'Admin'">
            新增药品
          </el-button>
        </div>
      </template>

      <el-form :inline="true" style="margin-bottom: 16px;">
        <el-form-item>
          <el-input v-model="keyword" placeholder="名称/编码/通用名" clearable @clear="fetchData" @keyup.enter="fetchData" style="width: 220px;" />
        </el-form-item>
        <el-form-item>
          <el-select v-model="categoryFilter" placeholder="分类" clearable @change="fetchData" style="width: 140px;">
            <el-option label="西药" value="西药" />
            <el-option label="中成药" value="中成药" />
            <el-option label="中药饮片" value="中药饮片" />
            <el-option label="中药颗粒" value="中药颗粒" />
            <el-option label="材料" value="材料" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchData">查询</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="code" label="编码" width="100" />
        <el-table-column prop="name" label="药品名称" min-width="150" show-overflow-tooltip />
        <el-table-column prop="specification" label="规格" width="100" />
        <el-table-column prop="manufacturer" label="厂家" width="120" show-overflow-tooltip />
        <el-table-column prop="category" label="分类" width="90">
          <template #default="{ row }">
            <el-tag size="small">{{ row.category }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="dosageForm" label="剂型" width="80" />
        <el-table-column prop="unit" label="单位" width="60" />
        <el-table-column prop="price" label="零售价" width="90">
          <template #default="{ row }">¥{{ row.price.toFixed(2) }}</template>
        </el-table-column>
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
      :title="isEdit ? '编辑药品' : '新增药品'"
      width="650px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="药品编码" prop="code">
              <el-input v-model="form.code" :disabled="isEdit" placeholder="唯一编码" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="药品名称" prop="name">
              <el-input v-model="form.name" placeholder="药品名称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="通用名">
              <el-input v-model="form.commonName" placeholder="通用名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="规格">
              <el-input v-model="form.specification" placeholder="如: 10mg*24片" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="生产厂家">
              <el-input v-model="form.manufacturer" placeholder="生产厂家" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="分类" prop="category">
              <el-select v-model="form.category" placeholder="选择分类" style="width: 100%;">
                <el-option label="西药" value="西药" />
                <el-option label="中成药" value="中成药" />
                <el-option label="中药饮片" value="中药饮片" />
                <el-option label="中药颗粒" value="中药颗粒" />
                <el-option label="材料" value="材料" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="剂型">
              <el-input v-model="form.dosageForm" placeholder="如: 片剂/胶囊/颗粒" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="单位">
              <el-input v-model="form.unit" placeholder="如: 盒/瓶/袋" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="零售价" prop="price">
              <el-input-number v-model="form.price" :min="0" :precision="2" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="成本价">
              <el-input-number v-model="form.costPrice" :min="0" :precision="2" style="width: 100%;" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="处方药">
              <el-switch v-model="form.isPrescription" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="启用" v-if="isEdit">
              <el-switch v-model="form.isActive" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
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
 * 药品字典管理页面
 * 提供药品的列表查看、新增、编辑、软删除功能
 * 支持按名称/编码搜索和分类筛选
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
  commonName: '',
  specification: '',
  manufacturer: '',
  category: '西药',
  dosageForm: '',
  unit: '',
  price: 0,
  costPrice: null,
  isPrescription: true,
  isActive: true,
  remark: ''
})

const rules = {
  code: [{ required: true, message: '请输入药品编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入药品名称', trigger: 'blur' }],
  category: [{ required: true, message: '请选择分类', trigger: 'change' }],
  price: [{ required: true, message: '请输入零售价', trigger: 'blur' }]
}

/**
 * 获取药品列表
 */
async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/drugs', {
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
    form.commonName = row.commonName || ''
    form.specification = row.specification || ''
    form.manufacturer = row.manufacturer || ''
    form.category = row.category
    form.dosageForm = row.dosageForm || ''
    form.unit = row.unit || ''
    form.price = row.price
    form.costPrice = row.costPrice ?? null
    form.isPrescription = row.isPrescription
    form.isActive = row.isActive
    form.remark = row.remark || ''
  } else {
    isEdit.value = false
    editId.value = null
    form.code = ''
    form.name = ''
    form.commonName = ''
    form.specification = ''
    form.manufacturer = ''
    form.category = '西药'
    form.dosageForm = ''
    form.unit = ''
    form.price = 0
    form.costPrice = null
    form.isPrescription = true
    form.isActive = true
    form.remark = ''
  }
  dialogVisible.value = true
}

/**
 * 提交新增/编辑药品
 */
async function handleSubmit() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    const payload = {
      code: form.code,
      name: form.name,
      commonName: form.commonName || null,
      specification: form.specification || null,
      manufacturer: form.manufacturer || null,
      category: form.category,
      dosageForm: form.dosageForm || null,
      unit: form.unit || null,
      price: form.price,
      costPrice: form.costPrice,
      isPrescription: form.isPrescription,
      isActive: form.isActive,
      remark: form.remark || null
    }
    if (isEdit.value) {
      await api.put(`/drugs/${editId.value}`, payload)
      ElMessage.success('更新成功')
    } else {
      await api.post('/drugs', payload)
      ElMessage.success('创建成功')
    }
    dialogVisible.value = false
    await fetchData()
  } finally {
    submitting.value = false
  }
}

/**
 * 软删除药品
 * @param {Object} row - 药品行数据
 */
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm(`确定要删除药品「${row.name}」吗？`, '提示', { type: 'warning' })
    await api.delete(`/drugs/${row.id}`)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* 用户取消 */ }
}

onMounted(fetchData)
</script>
