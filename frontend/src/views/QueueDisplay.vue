<template>
  <div>
    <el-form :inline="true" style="margin-bottom: 16px;">
      <el-form-item label="日期">
        <el-date-picker v-model="queueDate" type="date" value-format="YYYY-MM-DD" @change="fetchQueue" />
      </el-form-item>
      <el-form-item label="科室">
        <el-select v-model="queueDept" placeholder="全部科室" clearable @change="fetchQueue" style="width: 160px;">
          <el-option v-for="dept in departments" :key="dept.id" :label="dept.name" :value="dept.id" />
        </el-select>
      </el-form-item>
      <el-form-item>
        <el-button type="primary" @click="fetchQueue">刷新</el-button>
      </el-form-item>
    </el-form>

    <el-row :gutter="20">
      <el-col :span="8">
        <el-card shadow="hover" class="queue-section">
          <template #header>
            <div class="queue-title pending-title">待诊</div>
          </template>
          <div v-if="pendingList.length === 0" class="queue-empty">暂无待诊患者</div>
          <div v-for="item in pendingList" :key="item.id" class="queue-item">
            <span class="queue-no">{{ item.queueNumber }}号</span>
            <span class="queue-name">{{ item.patientName }}</span>
            <span class="queue-doctor">{{ item.doctorName }}</span>
            <span class="queue-dept">{{ item.departmentName }}</span>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover" class="queue-section">
          <template #header>
            <div class="queue-title progress-title">就诊中</div>
          </template>
          <div v-if="inProgressList.length === 0" class="queue-empty">暂无就诊中患者</div>
          <div v-for="item in inProgressList" :key="item.id" class="queue-item-progress">
            <span class="queue-no">{{ item.queueNumber }}号</span>
            <span class="queue-name">{{ item.patientName }}</span>
            <span class="queue-doctor">{{ item.doctorName }}医生</span>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover" class="queue-section">
          <template #header>
            <div class="queue-title completed-title">已完成</div>
          </template>
          <div v-if="completedList.length === 0" class="queue-empty">暂无完成记录</div>
          <div v-for="item in completedList" :key="item.id" class="queue-item">
            <span class="queue-no">{{ item.queueNumber }}号</span>
            <span class="queue-name">{{ item.patientName }}</span>
            <span class="queue-doctor">{{ item.doctorName }}</span>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
/**
 * 叫号大屏组件
 * 分三列显示待诊、就诊中、已完成患者队列
 * 支持按日期和科室筛选，自动每30秒刷新
 */
import { ref, computed, onMounted, onUnmounted } from 'vue'
import api from '@/api'
import dayjs from 'dayjs'

const queueDate = ref(dayjs().format('YYYY-MM-DD'))
const queueDept = ref(null)
const departments = ref([])
const queueData = ref([])
let timer = null

const pendingList = computed(() => queueData.value.filter(q => q.status === 'Pending'))
const inProgressList = computed(() => queueData.value.filter(q => q.status === 'InProgress'))
const completedList = computed(() => queueData.value.filter(q => q.status === 'Completed'))

/**
 * 获取叫号队列数据
 */
async function fetchQueue() {
  try {
    const params = { date: queueDate.value }
    if (queueDept.value) params.departmentId = queueDept.value
    const res = await api.get('/appointments/queue', { params })
    queueData.value = res.data
  } catch { queueData.value = [] }
}

/**
 * 获取科室列表用于筛选
 */
async function fetchDepartments() {
  try {
    const res = await api.get('/departments')
    departments.value = res.data
  } catch { /* ignore */ }
}

onMounted(() => {
  fetchQueue()
  fetchDepartments()
  timer = setInterval(fetchQueue, 30000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.queue-section {
  min-height: 300px;
}

.queue-title {
  font-size: 18px;
  font-weight: bold;
  text-align: center;
}

.pending-title { color: #e6a23c; }
.progress-title { color: #409eff; }
.completed-title { color: #67c23a; }

.queue-empty {
  text-align: center;
  color: #999;
  padding: 40px 0;
}

.queue-item, .queue-item-progress {
  display: flex;
  align-items: center;
  padding: 10px 12px;
  border-bottom: 1px solid #ebeef5;
  gap: 8px;
}

.queue-item-progress {
  background: #ecf5ff;
}

.queue-no {
  font-weight: bold;
  font-size: 16px;
  color: #409eff;
  min-width: 50px;
}

.queue-name {
  font-size: 15px;
  flex: 1;
}

.queue-doctor {
  font-size: 13px;
  color: #666;
}

.queue-dept {
  font-size: 12px;
  color: #999;
}
</style>
