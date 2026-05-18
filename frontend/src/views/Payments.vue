<template>
  <div>
    <el-tabs v-model="activeTab" type="card">
      <el-tab-pane label="收费管理" name="list">
        <el-card>
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>收费记录</span>
              <el-button type="primary" @click="activeTab = 'charge'" v-if="canCharge">新增收费</el-button>
            </div>
          </template>

          <el-form :inline="true" style="margin-bottom: 16px;">
            <el-form-item>
              <el-date-picker v-model="filterDate" type="date" placeholder="日期" value-format="YYYY-MM-DD" @change="fetchList" />
            </el-form-item>
            <el-form-item>
              <el-select v-model="filterStatus" placeholder="状态" clearable @change="fetchList" style="width: 120px;">
                <el-option label="已支付" value="已支付" />
                <el-option label="已退款" value="已退款" />
              </el-select>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="fetchList">查询</el-button>
            </el-form-item>
          </el-form>

          <el-table :data="list" v-loading="loading" stripe>
            <el-table-column prop="paymentNo" label="收费编号" width="180" />
            <el-table-column prop="patientName" label="患者" width="100" />
            <el-table-column prop="totalAmount" label="总金额" width="90">
              <template #default="{ row }">¥{{ row.totalAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="discountAmount" label="优惠" width="80">
              <template #default="{ row }">¥{{ (row.discountAmount || 0).toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="actualAmount" label="实收" width="90">
              <template #default="{ row }">¥{{ row.actualAmount.toFixed(2) }}</template>
            </el-table-column>
            <el-table-column prop="paymentMethod" label="支付方式" width="90" />
            <el-table-column prop="status" label="状态" width="80">
              <template #default="{ row }">
                <el-tag :type="row.status === '已退款' ? 'danger' : 'success'" size="small">{{ row.status }}</el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="operatorName" label="操作人" width="90" />
            <el-table-column prop="createdAt" label="收费时间" width="170">
              <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm') }}</template>
            </el-table-column>
            <el-table-column label="操作" width="100" fixed="right">
              <template #default="{ row }">
                <el-button v-if="row.status === '已支付'" type="danger" link size="small" @click="handleRefund(row)">退费</el-button>
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

      <el-tab-pane label="新增收费" name="charge" v-if="canCharge">
        <el-card>
          <template #header><span>新增收费</span></template>
          <el-form ref="chargeFormRef" :model="chargeForm" label-width="100px" style="max-width: 700px;">
            <el-form-item label="患者" prop="patientId">
              <el-select v-model="chargeForm.patientId" filterable placeholder="搜索患者" @focus="searchPatients('')" style="width: 100%;">
                <el-option v-for="p in patientOptions" :key="p.id" :label="`${p.name} - ${p.phone || '无电话'}`" :value="p.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="支付方式" prop="paymentMethod">
              <el-radio-group v-model="chargeForm.paymentMethod">
                <el-radio value="现金">现金</el-radio>
                <el-radio value="微信">微信</el-radio>
                <el-radio value="支付宝">支付宝</el-radio>
                <el-radio value="银行卡">银行卡</el-radio>
                <el-radio value="医保">医保</el-radio>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="优惠金额">
              <el-input-number v-model="chargeForm.discountAmount" :min="0" :precision="2" />
            </el-form-item>
            <el-form-item label="收费项目">
              <div style="width: 100%;">
                <div v-for="(item, idx) in chargeForm.extraItems" :key="idx" style="display: flex; gap: 8px; margin-bottom: 8px; align-items: center;">
                  <el-select v-model="item.itemType" style="width: 120px;">
                    <el-option label="挂号费" value="挂号费" />
                    <el-option label="药品费" value="药品费" />
                    <el-option label="检查费" value="检查费" />
                    <el-option label="治疗费" value="治疗费" />
                    <el-option label="其他" value="其他" />
                  </el-select>
                  <el-input v-model="item.itemName" placeholder="项目名称" style="width: 200px;" />
                  <el-input-number v-model="item.quantity" :min="1" style="width: 80px;" />
                  <el-input-number v-model="item.unitPrice" :min="0" :precision="2" style="width: 120px;" />
                  <span>小计: ¥{{ (item.quantity * item.unitPrice).toFixed(2) }}</span>
                  <el-button type="danger" icon="Delete" circle size="small" @click="chargeForm.extraItems.splice(idx, 1)" />
                </div>
                <el-button type="primary" size="small" @click="addChargeItem">添加项目</el-button>
              </div>
            </el-form-item>
            <el-form-item label="合计">
              <span style="font-size: 20px; color: #f56c6c; font-weight: bold;">
                ¥{{ totalChargeAmount.toFixed(2) }}
              </span>
              <span v-if="chargeForm.discountAmount" style="margin-left: 12px; font-size: 14px; color: #67c23a;">
                应收: ¥{{ (totalChargeAmount - chargeForm.discountAmount).toFixed(2) }}
              </span>
            </el-form-item>
            <el-form-item>
              <el-button type="primary" :loading="chargeSubmitting" @click="handleCharge">确认收款</el-button>
            </el-form-item>
          </el-form>
        </el-card>
      </el-tab-pane>

      <el-tab-pane label="日结对账" name="summary" v-if="canCharge">
        <el-card>
          <el-form :inline="true" style="margin-bottom: 16px;">
            <el-form-item>
              <el-date-picker v-model="summaryDate" type="date" value-format="YYYY-MM-DD" @change="fetchSummary" />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="fetchSummary">查询</el-button>
            </el-form-item>
          </el-form>

          <el-descriptions v-if="summary" :column="2" border>
            <el-descriptions-item label="日期">{{ summary.date?.split('T')[0] }}</el-descriptions-item>
            <el-descriptions-item label="总交易笔数">{{ summary.totalCount }}</el-descriptions-item>
            <el-descriptions-item label="成功笔数">{{ summary.paidCount }}</el-descriptions-item>
            <el-descriptions-item label="退款笔数">{{ summary.refundedCount }}</el-descriptions-item>
            <el-descriptions-item label="总收入">¥{{ (summary.totalIncome || 0).toFixed(2) }}</el-descriptions-item>
            <el-descriptions-item label="退款总额">¥{{ (summary.totalRefunded || 0).toFixed(2) }}</el-descriptions-item>
          </el-descriptions>

          <h4 v-if="summary?.byMethod?.length" style="margin-top: 20px;">按支付方式汇总</h4>
          <el-table v-if="summary?.byMethod?.length" :data="summary.byMethod" stripe style="margin-top: 8px;">
            <el-table-column prop="paymentMethod" label="支付方式" width="120" />
            <el-table-column prop="count" label="笔数" width="100" />
            <el-table-column label="金额" width="150">
              <template #default="{ row }">¥{{ (row.totalAmount || 0).toFixed(2) }}</template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
/**
 * 收费管理页面
 * 提供收费记录查看、新增收费、退费、日结对账功能
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'
import dayjs from 'dayjs'

const authStore = useAuthStore()
const canCharge = computed(() => ['Admin', 'Cashier'].includes(authStore.role))

const activeTab = ref('list')

const loading = ref(false)
const list = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const filterDate = ref('')
const filterStatus = ref('')

/**
 * 获取收费记录列表
 */
async function fetchList() {
  loading.value = true
  try {
    const params = { page: page.value, pageSize: pageSize.value }
    if (filterDate.value) params.date = filterDate.value
    if (filterStatus.value) params.status = filterStatus.value
    const res = await api.get('/payments', { params })
    list.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

/**
 * 退费
 * @param {Object} row - 收费记录
 */
async function handleRefund(row) {
  try {
    await ElMessageBox.confirm(`确定要对收费「${row.paymentNo}」进行退费吗？`, '退费确认', { type: 'warning' })
    await api.put(`/payments/${row.id}/refund`, { reason: '手动退费' })
    ElMessage.success('退费成功')
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

const chargeFormRef = ref()
const chargeSubmitting = ref(false)

const chargeForm = reactive({
  patientId: null,
  paymentMethod: '现金',
  discountAmount: 0,
  extraItems: []
})

const totalChargeAmount = computed(() =>
  chargeForm.extraItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0)
)

/**
 * 添加收费项目行
 */
function addChargeItem() {
  chargeForm.extraItems.push({
    itemType: '药品费',
    itemName: '',
    itemId: null,
    quantity: 1,
    unitPrice: 0
  })
}

/**
 * 提交收费
 */
async function handleCharge() {
  if (!chargeForm.patientId) { ElMessage.warning('请选择患者'); return }
  if (chargeForm.extraItems.length === 0) { ElMessage.warning('请添加收费项目'); return }

  chargeSubmitting.value = true
  try {
    await api.post('/payments/from-prescription', {
      patientId: chargeForm.patientId,
      prescriptionId: null,
      paymentMethod: chargeForm.paymentMethod,
      discountAmount: chargeForm.discountAmount,
      extraItems: chargeForm.extraItems.map(e => ({
        itemType: e.itemType,
        itemName: e.itemName,
        quantity: e.quantity,
        unitPrice: e.unitPrice
      }))
    })
    ElMessage.success('收费成功')
    chargeForm.patientId = null
    chargeForm.extraItems = []
    chargeForm.discountAmount = 0
    await fetchList()
    activeTab.value = 'list'
  } finally {
    chargeSubmitting.value = false
  }
}

const summaryDate = ref(dayjs().format('YYYY-MM-DD'))
const summary = ref(null)

/**
 * 获取日结数据
 */
async function fetchSummary() {
  try {
    const res = await api.get('/payments/daily-summary', { params: { date: summaryDate.value } })
    summary.value = res.data
  } catch { summary.value = null }
}

onMounted(fetchList)
</script>
