<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>医生排班管理</span>
        </div>
      </template>

      <el-form :inline="true" style="margin-bottom: 16px;">
        <el-form-item label="医生">
          <el-select v-model="selectedDoctorId" placeholder="选择医生" @change="fetchSchedules" filterable style="width: 200px;">
            <el-option v-for="doc in doctors" :key="doc.id" :label="`${doc.displayName} - ${doc.departmentName || '无科室'}`" :value="doc.id" />
          </el-select>
        </el-form-item>
      </el-form>

      <div v-if="selectedDoctorId">
        <h4 style="margin-bottom: 12px;">常规排班</h4>
        <el-table :data="regularSchedules" stripe>
          <el-table-column label="星期" width="100">
            <template #default="{ row }">{{ weekDayLabel(row.dayOfWeek) }}</template>
          </el-table-column>
          <el-table-column prop="timeSlot" label="时段" width="100" />
          <el-table-column label="开始时间" width="120">
            <template #default="{ row }">{{ row.startTime }}</template>
          </el-table-column>
          <el-table-column label="结束时间" width="120">
            <template #default="{ row }">{{ row.endTime }}</template>
          </el-table-column>
          <el-table-column prop="maxPatients" label="限额" width="80" />
          <el-table-column label="操作" width="80">
            <template #default="{ $index }">
              <el-button type="danger" link size="small" @click="removeRegularSchedule($index)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>

        <el-button type="primary" style="margin-top: 12px;" @click="addRegularSchedule">添加排班时段</el-button>

        <el-dialog v-model="scheduleDialogVisible" title="添加排班时段" width="400px">
          <el-form ref="schedFormRef" :model="schedForm" label-width="80px">
            <el-form-item label="星期">
              <el-select v-model="schedForm.dayOfWeek" style="width: 100%;">
                <el-option v-for="(label, val) in weekDayMap" :key="val" :label="label" :value="Number(val)" />
              </el-select>
            </el-form-item>
            <el-form-item label="时段">
              <el-select v-model="schedForm.timeSlot" style="width: 100%;">
                <el-option label="上午" value="上午" />
                <el-option label="下午" value="下午" />
                <el-option label="晚班" value="晚班" />
              </el-select>
            </el-form-item>
            <el-form-item label="开始时间">
              <el-time-picker v-model="schedForm.startTime" format="HH:mm" value-format="HH:mm" style="width: 100%;" />
            </el-form-item>
            <el-form-item label="结束时间">
              <el-time-picker v-model="schedForm.endTime" format="HH:mm" value-format="HH:mm" style="width: 100%;" />
            </el-form-item>
            <el-form-item label="限额">
              <el-input-number v-model="schedForm.maxPatients" :min="1" :max="200" />
            </el-form-item>
          </el-form>
          <template #footer>
            <el-button @click="scheduleDialogVisible = false">取消</el-button>
            <el-button type="primary" @click="confirmAddSchedule">确定</el-button>
          </template>
        </el-dialog>

        <div style="margin-top: 24px; display: flex; justify-content: flex-start;">
          <el-button type="success" @click="saveRegularSchedules">保存常规排班</el-button>
        </div>
      </div>
      <el-empty v-else description="请选择医生查看排班" />
    </el-card>
  </div>
</template>

<script setup>
/**
 * 医生排班管理页面
 * 提供医生常规排班的查看、添加、编辑、删除功能
 */
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import api from '@/api'

const doctors = ref([])
const selectedDoctorId = ref(null)
const regularSchedules = ref([])

const scheduleDialogVisible = ref(false)
const schedFormRef = ref()

const schedForm = reactive({
  dayOfWeek: 1,
  timeSlot: '上午',
  startTime: '08:00',
  endTime: '12:00',
  maxPatients: 30
})

const weekDayMap = { 0: '周日', 1: '周一', 2: '周二', 3: '周三', 4: '周四', 5: '周五', 6: '周六' }

/**
 * 获取医生列表
 */
async function fetchDoctors() {
  try {
    const res = await api.get('/users', { params: { pageSize: 200 } })
    doctors.value = res.data.items.filter(u => u.role === 'Doctor' && u.isActive)
  } catch { /* ignore */ }
}

/**
 * 获取选中医生的排班
 */
async function fetchSchedules() {
  if (!selectedDoctorId.value) return
  try {
    const res = await api.get(`/schedules/doctors/${selectedDoctorId.value}`)
    regularSchedules.value = res.data.map(s => ({
      ...s,
      startTime: s.startTime ? s.startTime.substring(0, 5) : '08:00',
      endTime: s.endTime ? s.endTime.substring(0, 5) : '12:00'
    }))
  } catch { regularSchedules.value = [] }
}

function weekDayLabel(day) {
  return weekDayMap[day] || day
}

/**
 * 添加排班时段弹窗
 */
function addRegularSchedule() {
  schedForm.dayOfWeek = 1
  schedForm.timeSlot = '上午'
  schedForm.startTime = '08:00'
  schedForm.endTime = '12:00'
  schedForm.maxPatients = 30
  scheduleDialogVisible.value = true
}

/**
 * 确认添加排班时段
 */
function confirmAddSchedule() {
  if (!schedForm.startTime || !schedForm.endTime) {
    ElMessage.warning('请选择时间')
    return
  }
  regularSchedules.value.push({ ...schedForm })
  scheduleDialogVisible.value = false
}

/**
 * 删除一条排班
 */
function removeRegularSchedule(index) {
  regularSchedules.value.splice(index, 1)
}

/**
 * 保存排班到后端
 */
async function saveRegularSchedules() {
  try {
    const payload = regularSchedules.value.map(s => ({
      dayOfWeek: s.dayOfWeek,
      timeSlot: s.timeSlot,
      startTime: `0001-01-01T${s.startTime}:00`,
      endTime: `0001-01-01T${s.endTime}:00`,
      maxPatients: s.maxPatients,
      isActive: true
    }))
    await api.post(`/schedules/doctors/${selectedDoctorId.value}`, payload)
    ElMessage.success('排班保存成功')
  } catch { /* error handled by interceptor */ }
}

onMounted(fetchDoctors)
</script>
