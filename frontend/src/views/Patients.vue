<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>患者列表</span>
          <el-button type="primary" @click="openDialog(null)">新增患者</el-button>
        </div>
      </template>

      <el-form :inline="true" style="margin-bottom: 16px;">
        <el-form-item>
          <el-input v-model="keyword" placeholder="姓名/手机号/身份证" clearable @clear="fetchData" @keyup.enter="fetchData" style="width: 240px;" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchData">查询</el-button>
        </el-form-item>
      </el-form>

      <el-table :data="list" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="name" label="姓名" width="100" />
        <el-table-column prop="gender" label="性别" width="60" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="idCard" label="身份证号" width="180" />
        <el-table-column prop="dateOfBirth" label="出生日期" width="120">
          <template #default="{ row }">
            {{ row.dateOfBirth ? dayjs(row.dateOfBirth).format('YYYY-MM-DD') : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="allergies" label="过敏史" min-width="120" show-overflow-tooltip />
        <el-table-column prop="medicalHistory" label="既往病史" min-width="120" show-overflow-tooltip />
        <el-table-column label="操作" width="180" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="openDialog(row)">编辑</el-button>
            <el-button type="success" link size="small" @click="openFamilyDialog(row)">家庭成员</el-button>
            <el-button
              type="danger" link size="small"
              @click="handleDelete(row)"
              v-if="authStore.role === 'Admin'"
            >删除</el-button>
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
      :title="isEdit ? '编辑患者' : '新增患者'"
      width="650px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="姓名" prop="name">
              <el-input v-model="form.name" placeholder="患者姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="性别">
              <el-radio-group v-model="form.gender">
                <el-radio value="男">男</el-radio>
                <el-radio value="女">女</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="手机号">
              <el-input v-model="form.phone" placeholder="手机号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="出生日期">
              <el-date-picker
                v-model="form.dateOfBirth"
                type="date"
                placeholder="选择日期"
                value-format="YYYY-MM-DD"
                style="width: 100%;"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="身份证号">
              <el-input v-model="form.idCard" placeholder="18位身份证号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="医保号">
              <el-input v-model="form.medicalInsuranceNo" placeholder="医保号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="地址">
          <el-input v-model="form.address" placeholder="家庭住址" />
        </el-form-item>
        <el-form-item label="过敏史">
          <el-input v-model="form.allergies" type="textarea" :rows="2" placeholder="患者过敏史" />
        </el-form-item>
        <el-form-item label="既往病史">
          <el-input v-model="form.medicalHistory" type="textarea" :rows="2" placeholder="既往病史" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="familyDialogVisible"
      :title="`家庭成员 - ${currentPatient?.name || ''}`"
      width="650px"
      :close-on-click-modal="false"
    >
      <div style="margin-bottom: 12px;">
        <el-button type="primary" size="small" @click="openFamilyMemberDialog(null)">添加成员</el-button>
      </div>
      <el-table :data="familyMembers" stripe>
        <el-table-column prop="name" label="姓名" width="100" />
        <el-table-column prop="relationship" label="关系" width="100" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="idCard" label="身份证号" width="180" />
        <el-table-column label="操作" width="120">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="openFamilyMemberDialog(row)">编辑</el-button>
            <el-button type="danger" link size="small" @click="handleDeleteMember(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <el-dialog
      v-model="memberDialogVisible"
      :title="isEditMember ? '编辑家庭成员' : '添加家庭成员'"
      width="450px"
      :close-on-click-modal="false"
    >
      <el-form ref="memberFormRef" :model="memberForm" :rules="memberRules" label-width="80px">
        <el-form-item label="姓名" prop="name">
          <el-input v-model="memberForm.name" placeholder="成员姓名" />
        </el-form-item>
        <el-form-item label="关系" prop="relationship">
          <el-select v-model="memberForm.relationship" placeholder="选择关系" style="width: 100%;">
            <el-option label="配偶" value="配偶" />
            <el-option label="父亲" value="父亲" />
            <el-option label="母亲" value="母亲" />
            <el-option label="儿子" value="儿子" />
            <el-option label="女儿" value="女儿" />
            <el-option label="兄弟" value="兄弟" />
            <el-option label="姐妹" value="姐妹" />
            <el-option label="其他" value="其他" />
          </el-select>
        </el-form-item>
        <el-form-item label="手机号">
          <el-input v-model="memberForm.phone" placeholder="手机号" />
        </el-form-item>
        <el-form-item label="身份证号">
          <el-input v-model="memberForm.idCard" placeholder="身份证号" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="memberDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="memberSubmitting" @click="handleSubmitMember">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
/**
 * 患者管理页面
 * 提供患者列表、新增、编辑、删除、家庭成员管理功能
 */
import { ref, reactive, onMounted, nextTick } from 'vue'
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
const keyword = ref('')

const dialogVisible = ref(false)
const isEdit = ref(false)
const editId = ref(null)
const submitting = ref(false)
const formRef = ref()

const form = reactive({
  name: '',
  gender: '男',
  phone: '',
  dateOfBirth: null,
  idCard: '',
  medicalInsuranceNo: '',
  address: '',
  allergies: '',
  medicalHistory: '',
  remark: ''
})

const rules = {
  name: [{ required: true, message: '请输入患者姓名', trigger: 'blur' }]
}

const familyDialogVisible = ref(false)
const currentPatient = ref(null)
const familyMembers = ref([])

const memberDialogVisible = ref(false)
const isEditMember = ref(false)
const editMemberId = ref(null)
const memberSubmitting = ref(false)
const memberFormRef = ref()

const memberForm = reactive({
  name: '',
  relationship: '配偶',
  phone: '',
  idCard: ''
})

const memberRules = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  relationship: [{ required: true, message: '请选择关系', trigger: 'change' }]
}

/**
 * 获取患者列表
 */
async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/patients', {
      params: { page: page.value, pageSize: pageSize.value, keyword: keyword.value }
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
    form.name = row.name
    form.gender = row.gender || '男'
    form.phone = row.phone || ''
    form.dateOfBirth = row.dateOfBirth ? dayjs(row.dateOfBirth).format('YYYY-MM-DD') : null
    form.idCard = row.idCard || ''
    form.medicalInsuranceNo = ''
    form.address = row.address || ''
    form.allergies = row.allergies || ''
    form.medicalHistory = row.medicalHistory || ''
    form.remark = ''
  } else {
    isEdit.value = false
    editId.value = null
    form.name = ''
    form.gender = '男'
    form.phone = ''
    form.dateOfBirth = null
    form.idCard = ''
    form.medicalInsuranceNo = ''
    form.address = ''
    form.allergies = ''
    form.medicalHistory = ''
    form.remark = ''
  }
  dialogVisible.value = true
}

/**
 * 提交新增/编辑患者
 */
async function handleSubmit() {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    const payload = {
      name: form.name,
      gender: form.gender,
      dateOfBirth: form.dateOfBirth ? new Date(form.dateOfBirth).toISOString() : null,
      phone: form.phone || null,
      idCard: form.idCard || null,
      address: form.address || null,
      medicalInsuranceNo: form.medicalInsuranceNo || null,
      allergies: form.allergies || null,
      medicalHistory: form.medicalHistory || null,
      remark: form.remark || null
    }
    if (isEdit.value) {
      await api.put(`/patients/${editId.value}`, payload)
      ElMessage.success('患者信息更新成功')
    } else {
      await api.post('/patients', payload)
      ElMessage.success('患者创建成功')
    }
    dialogVisible.value = false
    await fetchData()
  } finally {
    submitting.value = false
  }
}

/**
 * 删除患者
 * @param {Object} row - 患者行数据
 */
async function handleDelete(row) {
  try {
    await ElMessageBox.confirm(`确定要删除患者「${row.name}」吗？此操作为软删除。`, '提示', { type: 'warning' })
    await api.delete(`/patients/${row.id}`)
    ElMessage.success('删除成功')
    await fetchData()
  } catch { /* 用户取消 */ }
}

/**
 * 打开家庭成员弹窗
 * @param {Object} row - 患者行数据
 */
async function openFamilyDialog(row) {
  currentPatient.value = row
  await fetchFamilyMembers(row.id)
  familyDialogVisible.value = true
}

/**
 * 获取家庭成员列表
 * @param {number} patientId - 患者ID
 */
async function fetchFamilyMembers(patientId) {
  try {
    const res = await api.get(`/patients/${patientId}/family`)
    familyMembers.value = res.data
  } catch {
    familyMembers.value = []
  }
}

/**
 * 打开家庭成员新增/编辑弹窗
 * @param {Object|null} row - 成员数据，null为新增
 */
async function openFamilyMemberDialog(row) {
  if (row) {
    isEditMember.value = true
    editMemberId.value = row.id
    memberForm.name = row.name
    memberForm.relationship = row.relationship
    memberForm.phone = row.phone || ''
    memberForm.idCard = row.idCard || ''
  } else {
    isEditMember.value = false
    editMemberId.value = null
    memberForm.name = ''
    memberForm.relationship = '配偶'
    memberForm.phone = ''
    memberForm.idCard = ''
  }
  memberDialogVisible.value = true
  await nextTick()
  memberFormRef.value?.resetFields()
}

/**
 * 提交新增/编辑家庭成员
 */
async function handleSubmitMember() {
  const valid = await memberFormRef.value.validate().catch(() => false)
  if (!valid) return

  memberSubmitting.value = true
  try {
    const payload = {
      name: memberForm.name,
      relationship: memberForm.relationship,
      phone: memberForm.phone || null,
      idCard: memberForm.idCard || null
    }
    const patientId = currentPatient.value.id
    if (isEditMember.value) {
      await api.put(`/patients/${patientId}/family/${editMemberId.value}`, payload)
      ElMessage.success('成员更新成功')
    } else {
      await api.post(`/patients/${patientId}/family`, payload)
      ElMessage.success('成员添加成功')
    }
    memberDialogVisible.value = false
    await fetchFamilyMembers(patientId)
  } finally {
    memberSubmitting.value = false
  }
}

/**
 * 删除家庭成员
 * @param {Object} row - 成员行数据
 */
async function handleDeleteMember(row) {
  try {
    await ElMessageBox.confirm(`确定要删除成员「${row.name}」吗？`, '提示', { type: 'warning' })
    await api.delete(`/patients/${currentPatient.value.id}/family/${row.id}`)
    ElMessage.success('删除成功')
    await fetchFamilyMembers(currentPatient.value.id)
  } catch { /* 用户取消 */ }
}

onMounted(fetchData)
</script>
