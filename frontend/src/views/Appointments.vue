<template>
  <div>
    <el-tabs v-model="activeTab" type="card">
      <el-tab-pane label="挂号列表" name="list">
        <el-card>
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>挂号记录</span>
              <el-button type="primary" @click="activeTab = 'register'" v-if="canRegister">现场挂号</el-button>
            </div>
          </template>

          <el-form :inline="true" style="margin-bottom: 16px;">
            <el-form-item>
              <el-date-picker v-model="filterDate" type="date" placeholder="选择日期" value-format="YYYY-MM-DD" @change="fetchList" />
            </el-form-item>
            <el-form-item>
              <el-select v-model="filterStatus" placeholder="状态" clearable @change="fetchList" style="width: 120px;">
                <el-option label="待诊" value="Pending" />
                <el-option label="就诊中" value="InProgress" />
                <el-option label="已完成" value="Completed" />
                <el-option label="已取消" value="Cancelled" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-input v-model="filterKeyword" placeholder="患者姓名/挂号号" clearable @clear="fetchList" @keyup.enter="fetchList" style="width: 200px;" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="fetchList">查询</el-button>
            </el-form-item>
          </el-form>

          <el-table :data="list" v-loading="loading" stripe>
            <el-table-column prop="appointmentNo" label="挂号编号" width="180" />
            <el-table-column prop="patientName" label="患者" width="100" />
            <el-table-column prop="patientGender" label="性别" width="60" />
            <el-table-column prop="departmentName" label="科室" width="100" />
            <el-table-column prop="doctorName" label="医生" width="100" />
            <el-table-column prop="appointmentDate" label="就诊日期" width="110">
              <template #default="{ row }">{{ dayjs(row.appointmentDate).format('YYYY-MM-DD') }}</template>
            </el-table-column>
            <el-table-column prop="timeSlot" label="时段" width="70" />
            <el-table-column prop="queueNumber" label="排队号" width="70" />
            <el-table-column prop="status" label="状态" width="90">
              <template #default="{ row }">
                <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="createdAt" label="挂号时间" width="170">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
            </el-table-column>
            <el-table-column label="操作" width="200" fixed="right">
              <template #default="{ row }">
                <el-button v-if="row.status === 'Pending'" type="success" link size="small" @click="handleStartVisit(row)">叫号</el-button>
                <el-button v-if="row.status === 'InProgress'" type="primary" link size="small" @click="handleComplete(row)">完成</el-button>
                <el-button v-if="row.status === 'Pending'" type="warning" link size="small" @click="openReschedule(row)">改签</el-button>
                <el-button v-if="row.status === 'Pending'" type="danger" link size="small" @click="handleCancel(row)">退号</el-button>
              </template>
            </el-table-column>
          </el-table>

          <div style="display: flex; justify-content: flex-end; margin-top: 16px;">
            <el-pagination
              v-model:current-page="page"
              :page-size="pageSize"
              :total="total"
              layout="total, prev, pager, next"
              @current-change="fetchList"
            />
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="现场挂号" name="register" v-if="canRegister">
        <el-card>
          <template #header><span>现场挂号</span></template>
          <el-form ref="regFormRef" :model="regForm" :rules="regRules" label-width="100px" style="max-width: 700px;">
            <el-form-item label="就诊日期" prop="appointmentDate">
              <el-date-picker v-model="regForm.appointmentDate" type="date" value-format="YYYY-MM-DD" style="width: 200px;" />
            </el-form-item>
            <el-form-item label="选择患者" prop="patientId">
              <el-select v-model="regForm.patientId" filterable placeholder="搜索患者姓名/手机号" style="width: 100%;" @focus="searchPatients('')">
                <el-option v-for="p in patientOptions" :key="p.id" :label="`${p.name} - ${p.phone || '无电话'}`" :value="p.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="选择医生" prop="doctorId">
              <div style="width: 100%;">
                <el-table :data="availableDoctors" stripe highlight-current-row @current-change="selectDoctor" max-height="300" style="width: 100%;">
                  <el-table-column prop="doctorName" label="医生" width="100" />
                  <el-table-column prop="title" label="职称" width="80" />
                  <el-table-column prop="departmentName" label="科室" width="100" />
                  <el-table-column prop="timeSlot" label="时段" width="70" />
                  <el-table-column label="费用" width="80">
                    <template #default="{ row }">¥{{ row.consultationFee }}</template>
                  </el-table-column>
                  <el-table-column label="剩余号源" width="90">
                    <template #default="{ row }">
                      <el-tag :type="row.remainingSlots > 0 ? 'success' : 'danger'" size="small">
                        {{ row.remainingSlots ?? row.maxPatients }}
                      </el-tag>
                    </template>
                  </el-table-column>
                </el-table>
              </div>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="regSubmitting" @click="handleRegister">确认挂号</el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="叫号大屏" name="queue">
        <QueueDisplay />
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="rescheduleVisible" title="改签" width="500px">
      <el-form ref="rsFormRef" :model="rsForm" :rules="rsRules" label-width="100px">
        <el-form-item label="新日期" prop="appointmentDate">
          <el-date-picker v-model="rsForm.appointmentDate" type="date" value-format="YYYY-MM-DD" style="width: 100%;" />
        </el-form-item>
        <el-form-item label="选择医生" prop="doctorId">
          <div style="width: 100%;">
            <el-table :data="rsDoctors" stripe highlight-current-row @current-change="selectRsDoctor" max-height="250">
              <el-table-column prop="doctorName" label="医生" width="100" />
              <el-table-column prop="departmentName" label="科室" width="100" />
              <el-table-column prop="timeSlot" label="时段" width="70" />
              <el-table-column label="费用" width="80">
                <template #default="{ row }">¥{{ row.consultationFee }}</template>
              </el-table-column>
            </el-table>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="rescheduleVisible = false">取消</el-button>
        <el-button type="primary" :loading="rsSubmitting" @click="handleReschedule">确认改签</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
/**
 * 挂号管理页面
 * 提供挂号列表、现场挂号、改签、退号、叫号大屏等功能
 */
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'
import dayjs from 'dayjs'
import QueueDisplay from './QueueDisplay.vue'

const authStore = useAuthStore()
const canRegister = computed(() => ['Admin', 'Nurse', 'Cashier'].includes(authStore.role))
const canCancel = computed(() => ['Admin', 'Nurse', 'Cashier'].includes(authStore.role))

const activeTab = ref('list')

const statusMap = { Pending: '待诊', InProgress: '就诊中', Completed: '已完成', Cancelled: '已取消' }
const statusTagMap = { Pending: 'warning', InProgress: '', Completed: 'success', Cancelled: 'info' }

function statusLabel(s) { return statusMap[s] || s }
function statusTagType(s) { return statusTagMap[s] || '' }

const loading = ref(false)
const list = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const filterDate = ref('')
const filterStatus = ref('')
const filterKeyword = ref('')

/**
 * 获取挂号列表
 */
async function fetchList() {
  loading.value = true
  try {
    const params = { page: page.value, pageSize: pageSize.value }
    if (filterDate.value) params.date = filterDate.value
    if (filterStatus.value) params.status = filterStatus.value
    if (filterKeyword.value) params.keyword = filterKeyword.value
    const res = await api.get('/appointments', { params })
    list.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

/**
 * 叫号/开始就诊
 */
async function handleStartVisit(row) {
  try {
    await ElMessageBox.confirm(`确定叫号患者「${row.patientName}」吗？`, '叫号确认')
    await api.put(`/appointments/${row.id}/start-visit`)
    ElMessage.success('已叫号')
    await fetchList()
  } catch { /* 取消 */ }
}

/**
 * 完成就诊
 */
async function handleComplete(row) {
  try {
    await ElMessageBox.confirm(`确定完成患者「${row.patientName}」的就诊吗？`, '完成确认')
    await api.put(`/appointments/${row.id}/complete`)
    ElMessage.success('就诊完成')
    await fetchList()
  } catch { /* 取消 */ }
}

/**
 * 退号
 */
async function handleCancel(row) {
  try {
    await ElMessageBox.confirm(`确定退掉患者「${row.patientName}」的挂号吗？`, '退号确认', { type: 'warning' })
    await api.put(`/appointments/${row.id}/cancel`, { reason: '手动退号' })
    ElMessage.success('退号成功')
    await fetchList()
  } catch { /* 取消 */ }
}

const patientOptions = ref([])

/**
 * 搜索患者
 */
async function searchPatients(keyword) {
  try {
    const res = await api.get('/patients', { params: { keyword, pageSize: 50 } })
    patientOptions.value = res.data.items
  } catch { patientOptions.value = [] }
}

const availableDoctors = ref([])
const regFormRef = ref()
const regSubmitting = ref(false)
const regForm = reactive({
  appointmentDate: dayjs().format('YYYY-MM-DD'),
  patientId: null,
  doctorId: null
})
const selectedRegDoctor = ref(null)

const regRules = {
  appointmentDate: [{ required: true, message: '选择日期', trigger: 'change' }],
  patientId: [{ required: true, message: '选择患者', trigger: 'change' }],
  doctorId: [{ required: true, message: '选择医生', trigger: 'change' }]
}

/**
 * 选择医生行
 */
function selectDoctor(row) {
  if (!row) return
  selectedRegDoctor.value = row
  regForm.doctorId = row.doctorId
}

/**
 * 获取可用医生
 */
async function fetchAvailableDoctors() {
  if (!regForm.appointmentDate) return
  try {
    const res = await api.get('/schedules/available', { params: { date: regForm.appointmentDate } })
    availableDoctors.value = res.data
  } catch { availableDoctors.value = [] }
}

watch(() => regForm.appointmentDate, () => {
  selectedRegDoctor.value = null
  regForm.doctorId = null
  fetchAvailableDoctors()
}, { immediate: true })

/**
 * 提交挂号
 */
async function handleRegister() {
  const valid = await regFormRef.value.validate().catch(() => false)
  if (!valid) return
  if (!selectedRegDoctor.value) { ElMessage.warning('请选择医生'); return }

  regSubmitting.value = true
  try {
    await api.post('/appointments', {
      patientId: regForm.patientId,
      doctorId: regForm.doctorId,
      appointmentDate: new Date(regForm.appointmentDate).toISOString(),
      timeSlot: selectedRegDoctor.value.timeSlot,
      source: '现场'
    })
    ElMessage.success('挂号成功')
    regForm.patientId = null
    selectedRegDoctor.value = null
    regForm.doctorId = null
    await fetchAvailableDoctors()
    await fetchList()
    activeTab.value = 'list'
  } finally {
    regSubmitting.value = false
  }
}

const rescheduleVisible = ref(false)
const rescheduleId = ref(null)
const rsFormRef = ref()
const rsSubmitting = ref(false)
const rsDoctors = ref([])
const selectedRsDoctor = ref(null)

const rsForm = reactive({
  appointmentDate: '',
  doctorId: null
})

const rsRules = {
  appointmentDate: [{ required: true, message: '选择日期', trigger: 'change' }],
  doctorId: [{ required: true, message: '选择医生', trigger: 'change' }]
}

/**
 * 打开改签弹窗
 */
function openReschedule(row) {
  rescheduleId.value = row.id
  rsForm.appointmentDate = ''
  rsForm.doctorId = null
  selectedRsDoctor.value = null
  rsDoctors.value = []
  rescheduleVisible.value = true
}

watch(() => rsForm.appointmentDate, async () => {
  if (!rsForm.appointmentDate) return
  try {
    const res = await api.get('/schedules/available', { params: { date: rsForm.appointmentDate } })
    rsDoctors.value = res.data
  } catch { rsDoctors.value = [] }
})

function selectRsDoctor(row) {
  if (!row) return
  selectedRsDoctor.value = row
  rsForm.doctorId = row.doctorId
}

/**
 * 提交改签
 */
async function handleReschedule() {
  const valid = await rsFormRef.value.validate().catch(() => false)
  if (!valid) return
  if (!selectedRsDoctor.value) { ElMessage.warning('请选择医生'); return }

  rsSubmitting.value = true
  try {
    await api.put(`/appointments/${rescheduleId.value}/reschedule`, {
      doctorId: rsForm.doctorId,
      appointmentDate: new Date(rsForm.appointmentDate).toISOString(),
      timeSlot: selectedRsDoctor.value.timeSlot
    })
    ElMessage.success('改签成功')
    rescheduleVisible.value = false
    await fetchList()
  } finally {
    rsSubmitting.value = false
  }
}

onMounted(fetchList)
</script>
