<template>
  <div>
    <el-tabs v-model="activeTab" type="card">
      <el-tab-pane label="待发药" name="pending">
        <el-card>
          <template #header><span>待发药处方</span></template>
          <el-form :inline="true" style="margin-bottom: 16px;">
            <el-form-item>
              <el-select v-model="filterType" placeholder="处方类型" clearable @change="fetchPending" style="width: 140px;">
                <el-option label="西药" value="西药" />
                <el-option label="中成药" value="中成药" />
                <el-option label="中药饮片" value="中药饮片" />
                <el-option label="中药颗粒" value="中药颗粒" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="fetchPending">刷新</el-button>
            </el-form-item>
          </el-form>

          <el-table :data="pendingList" v-loading="pendingLoading" stripe>
            <el-table-column prop="prescriptionNo" label="处方编号" width="180" />
            <el-table-column prop="patientName" label="患者" width="100" />
            <el-table-column prop="doctorName" label="开方医生" width="100" />
            <el-table-column prop="type" label="类型" width="90">
              <template #default="{ row }">
                <el-tag size="small">{{ row.type }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="totalAmount" label="金额" width="90">
              <template #default="{ row }">¥{{ row.totalAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="createdAt" label="开方时间" width="170">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
            </el-table-column>
            <el-table-column label="操作" width="180">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="handleDispense(row)">发药</el-button>
                <el-button type="info" link size="small" @click="viewPrescription(row)">查看详情</el-button>
              </template>
            </el-table-column>
          </el-table>

          <div style="display: flex; justify-content: flex-end; margin-top: 16px;">
            <el-pagination v-model:current-page="pendingPage" :page-size="20" :total="pendingTotal" layout="total, prev, pager, next" @current-change="fetchPending" />
          </div>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="已发药" name="dispensed">
        <el-card>
          <template #header><span>已发药处方</span></template>
          <el-table :data="dispensedList" v-loading="dispensedLoading" stripe>
            <el-table-column prop="prescriptionNo" label="处方编号" width="180" />
            <el-table-column prop="patientName" label="患者" width="100" />
            <el-table-column prop="doctorName" label="开方医生" width="100" />
            <el-table-column prop="type" label="类型" width="90">
              <template #default="{ row }">
                <el-tag size="small">{{ row.type }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="totalAmount" label="金额" width="90">
              <template #default="{ row }">¥{{ row.totalAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column label="发药时间" width="170">
              <template #default="{ row }">
                {{ row.dispensedAt ? dayjs(row.dispensedAt).format('YYYY-MM-DD HH:mm') : '-' }}
              </template>
            </el-table-column>
            <el-table-column label="操作" width="100">
              <template #default="{ row }">
                <el-button type="danger" link size="small" @click="handleReturn(row)">退药</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>
    </el-tabs>

    <el-drawer v-model="drawerVisible" title="处方详情" size="500px">
      <div v-if="currentPrescription">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="处方编号">{{ currentPrescription.prescriptionNo }}</el-descriptions-item>
          <el-descriptions-item label="类型">{{ currentPrescription.type }}</el-descriptions-item>
          <el-descriptions-item label="患者">{{ currentPrescription.patientName }}</el-descriptions-item>
          <el-descriptions-item label="医生">{{ currentPrescription.doctorName }}</el-descriptions-item>
          <el-descriptions-item label="金额">¥{{ currentPrescription.totalAmount?.toFixed(2) }}</el-descriptions-item>
          <el-descriptions-item label="状态">{{ currentPrescription.status }}</el-descriptions-item>
        </el-descriptions>
        <h4 style="margin-top: 16px;">药品明细</h4>
        <el-table :data="prescriptionItems" stripe size="small">
          <el-table-column prop="drugName" label="药品" />
          <el-table-column prop="specification" label="规格" width="100" />
          <el-table-column prop="quantity" label="数量" width="70" />
          <el-table-column prop="unitPrice" label="单价" width="80">
            <template #default="{ row }">¥{{ row.unitPrice.toFixed(2) }}</template>
          </el-table-column>
          <el-table-column prop="subTotal" label="小计" width="80">
            <template #default="{ row }">¥{{ row.subTotal.toFixed(2) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
/**
 * 药房管理页面
 * 提供待发药列表、发药确认、退药、处方详情查看功能
 */
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import dayjs from 'dayjs'

const activeTab = ref('pending')

const pendingLoading = ref(false)
const pendingList = ref([])
const pendingTotal = ref(0)
const pendingPage = ref(1)
const filterType = ref('')

const dispensedLoading = ref(false)
const dispensedList = ref([])

const drawerVisible = ref(false)
const currentPrescription = ref(null)
const prescriptionItems = ref([])

/**
 * 获取待发药处方列表
 */
async function fetchPending() {
  pendingLoading.value = true
  try {
    const params = { page: pendingPage.value, pageSize: 20 }
    if (filterType.value) params.type = filterType.value
    const res = await api.get('/prescriptions/pending-dispense', { params })
    pendingList.value = res.data.items
    pendingTotal.value = res.data.total
  } finally {
    pendingLoading.value = false
  }
}

/**
 * 获取已发药处方列表
 */
async function fetchDispensed() {
  dispensedLoading.value = true
  try {
    const res = await api.get('/prescriptions', { params: { status: '已发药', pageSize: 100 } })
    dispensedList.value = res.data.items
  } finally {
    dispensedLoading.value = false
  }
}

/**
 * 发药确认
 * @param {Object} row - 处方行
 */
async function handleDispense(row) {
  try {
    await ElMessageBox.confirm(`确定对处方「${row.prescriptionNo}」进行发药吗？`, '发药确认')
    await api.put(`/prescriptions/${row.id}/dispense`)
    ElMessage.success('发药成功')
    await fetchPending()
  } catch { /* 取消 */ }
}

/**
 * 退药
 * @param {Object} row - 处方行
 */
async function handleReturn(row) {
  try {
    await ElMessageBox.confirm(`确定对处方「${row.prescriptionNo}」进行退药吗？`, '退药确认', { type: 'warning' })
    await api.put(`/prescriptions/${row.id}/return`)
    ElMessage.success('退药成功')
    await fetchDispensed()
  } catch { /* 取消 */ }
}

/**
 * 查看处方详情
 * @param {Object} row - 处方行
 */
async function viewPrescription(row) {
  try {
    const res = await api.get(`/prescriptions/${row.id}`)
    currentPrescription.value = { ...row, ...res.data }
    prescriptionItems.value = res.data.items || []
    drawerVisible.value = true
  } catch { /* ignore */ }
}

onMounted(() => {
  fetchPending()
  fetchDispensed()
})
</script>
