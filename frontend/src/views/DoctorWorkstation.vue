<template>
  <div>
    <el-row :gutter="20" style="height: calc(100vh - 130px);">
      <el-col :span="7" style="height: 100%;">
        <el-card style="height: 100%;" body-style="padding: 0; height: 100%; display: flex; flex-direction: column;">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>待诊队列</span>
              <el-button size="small" @click="fetchQueue">刷新</el-button>
            </div>
          </template>
          <div style="flex: 1; overflow-y: auto; padding: 8px;">
            <div v-if="queueList.length === 0" style="text-align: center; color: #999; padding: 40px 0;">暂无待诊患者</div>
            <div
              v-for="item in queueList"
              :key="item.id"
              class="queue-card"
              :class="{ active: currentAppointment?.id === item.id }"
              @click="selectPatient(item)"
            >
              <div style="display: flex; justify-content: space-between;">
                <strong>#{{ item.queueNumber }} {{ item.patientName }}</strong>
                <el-tag :type="item.status === 'Pending' ? 'warning' : ''" size="small">
                  {{ item.status === 'Pending' ? '待诊' : '就诊中' }}
                </el-tag>
              </div>
              <div style="font-size: 12px; color: #666; margin-top: 4px;">
                {{ item.timeSlot }} | {{ item.departmentName }}
              </div>
            </div>
          </div>
        </el-card>
      </el-col>

      <el-col :span="17" style="height: 100%;">
        <el-card v-if="!currentAppointment" style="height: 100%; display: flex; align-items: center; justify-content: center;">
          <el-empty description="请从左侧队列选择患者开始接诊" />
        </el-card>

        <div v-else style="height: 100%; overflow-y: auto;">
          <el-card style="margin-bottom: 16px;">
            <template #header>
              <div style="display: flex; justify-content: space-between; align-items: center;">
                <span>【{{ currentAppointment.patientName }}】病历信息</span>
                <div>
                  <el-button type="warning" size="small" v-if="currentAppointment.status === 'Pending'" @click="handleStartVisit">
                    开始接诊
                  </el-button>
                  <el-button type="success" size="small" @click="handleCompleteVisit">
                    结束就诊
                  </el-button>
                </div>
              </div>
            </template>

            <el-tabs v-model="recordTab">
              <el-tab-pane label="西医病历" name="western">
                <el-form :model="recordForm" label-width="100px">
                  <el-form-item label="主诉">
                    <el-input v-model="recordForm.chiefComplaint" type="textarea" :rows="2" placeholder="患者主诉" />
                  </el-form-item>
                  <el-form-item label="现病史">
                    <el-input v-model="recordForm.presentIllness" type="textarea" :rows="3" placeholder="现病史" />
                  </el-form-item>
                  <el-form-item label="既往史">
                    <el-input v-model="recordForm.pastHistory" type="textarea" :rows="2" placeholder="既往史" />
                  </el-form-item>
                  <el-form-item label="体格检查">
                    <el-input v-model="recordForm.physicalExamination" type="textarea" :rows="3" placeholder="体格检查" />
                  </el-form-item>
                  <el-form-item label="辅助检查">
                    <el-input v-model="recordForm.auxiliaryExamination" type="textarea" :rows="2" placeholder="辅助检查" />
                  </el-form-item>
                  <el-form-item label="西医诊断">
                    <el-input v-model="recordForm.westernDiagnosis" type="textarea" :rows="2" placeholder="西医诊断" />
                  </el-form-item>
                  <el-form-item label="医嘱">
                    <el-input v-model="recordForm.doctorAdvice" type="textarea" :rows="2" placeholder="医嘱" />
                  </el-form-item>
                </el-form>
              </el-tab-pane>
              <el-tab-pane label="中医四诊" name="tcm">
                <el-form :model="recordForm" label-width="100px">
                  <el-form-item label="望诊">
                    <el-input v-model="recordForm.tcmObservation" type="textarea" :rows="2" placeholder="望诊" />
                  </el-form-item>
                  <el-form-item label="闻诊">
                    <el-input v-model="recordForm.tcmAuscultation" type="textarea" :rows="2" placeholder="闻诊" />
                  </el-form-item>
                  <el-form-item label="问诊">
                    <el-input v-model="recordForm.tcmInquiry" type="textarea" :rows="2" placeholder="问诊" />
                  </el-form-item>
                  <el-form-item label="切诊">
                    <el-input v-model="recordForm.tcmPalpation" type="textarea" :rows="2" placeholder="切诊" />
                  </el-form-item>
                  <el-form-item label="中医诊断">
                    <el-input v-model="recordForm.tcmDiagnosis" type="textarea" :rows="2" placeholder="中医诊断" />
                  </el-form-item>
                  <el-form-item label="中医证候">
                    <el-input v-model="recordForm.tcmSyndrome" type="textarea" :rows="2" placeholder="证候" />
                  </el-form-item>
                </el-form>
              </el-tab-pane>
              <el-tab-pane label="处方" name="prescription">
                <div style="margin-bottom: 12px;">
                  <el-select v-model="rxType" style="width: 150px; margin-right: 12px;">
                    <el-option label="西药" value="西药" />
                    <el-option label="中成药" value="中成药" />
                    <el-option label="中药饮片" value="中药饮片" />
                    <el-option label="中药颗粒" value="中药颗粒" />
                  </el-select>
                  <el-button type="primary" size="small" @click="addRxItem">添加药品</el-button>
                </div>

                <el-table :data="rxItems" stripe>
                  <el-table-column label="药品" width="160">
                    <template #default="{ row }">
                      <el-select v-model="row.drugId" filterable placeholder="选择药品" size="small" @change="onDrugSelected($event, row)" style="width: 100%;">
                        <el-option v-for="d in drugOptions" :key="d.id" :label="`${d.name} (${d.specification || '无规格'})`" :value="d.id" />
                      </el-select>
                    </template>
                  </el-table-column>
                  <el-table-column label="数量" width="80">
                    <template #default="{ row }">
                      <el-input-number v-model="row.quantity" :min="0.1" size="small" controls-position="right" />
                    </template>
                  </el-table-column>
                  <el-table-column label="用量" width="100">
                    <template #default="{ row }">
                      <el-input v-model="row.dosage" size="small" placeholder="如: 1片" />
                    </template>
                  </el-table-column>
                  <el-table-column label="频次" width="100">
                    <template #default="{ row }">
                      <el-select v-model="row.frequency" size="small" placeholder="频次">
                        <el-option label="tid(每日3次)" value="tid" />
                        <el-option label="bid(每日2次)" value="bid" />
                        <el-option label="qd(每日1次)" value="qd" />
                        <el-option label="qod(隔日1次)" value="qod" />
                        <el-option label="prn(必要时)" value="prn" />
                      </el-select>
                    </template>
                  </el-table-column>
                  <el-table-column label="用法" width="100">
                    <template #default="{ row }">
                      <el-select v-model="row.usage" size="small" placeholder="用法">
                        <el-option label="口服" value="口服" />
                        <el-option label="外用" value="外用" />
                        <el-option label="静脉注射" value="静脉注射" />
                        <el-option label="肌肉注射" value="肌肉注射" />
                      </el-select>
                    </template>
                  </el-table-column>
                  <el-table-column label="天数" width="80">
                    <template #default="{ row }">
                      <el-input v-model="row.days" size="small" placeholder="如: 7" />
                    </template>
                  </el-table-column>
                  <el-table-column label="单价" width="90">
                    <template #default="{ row }">¥{{ row.unitPrice.toFixed(2) }}</template>
                  </el-table-column>
                  <el-table-column label="操作" width="60">
                    <template #default="{ $index }">
                      <el-button type="danger" link size="small" @click="rxItems.splice($index, 1)">删除</el-button>
                    </template>
                  </el-table-column>
                </el-table>

                <div v-if="rxType === '中药饮片' || rxType === '中药颗粒'" style="margin-top: 12px;">
                  <el-form-item label="煎药方法" v-if="rxType === '中药饮片'">
                    <el-input v-model="rxDecoctingMethod" placeholder="先煎/后下/包煎/烊化等" style="width: 400px;" />
                  </el-form-item>
                  <el-form-item label="用法说明">
                    <el-input v-model="rxDirection" type="textarea" :rows="2" placeholder="如: 每日一剂，分早晚两次温服" style="width: 500px;" />
                  </el-form-item>
                </div>

                <el-button type="primary" style="margin-top: 12px;" :loading="rxSubmitting" @click="handleSavePrescription">
                  提交处方
                </el-button>
              </el-tab-pane>
            </el-tabs>

            <div style="margin-top: 16px; text-align: right;">
              <el-button type="primary" :loading="recordSubmitting" @click="handleSaveRecord">保存病历</el-button>
            </div>
          </el-card>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
/**
 * 医生工作站页面
 * 提供接诊列队查看、电子病历（西医+中医四诊）编辑、处方开立功能
 */
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import api from '@/api'
import { useAuthStore } from '@/stores/auth'
import dayjs from 'dayjs'

const authStore = useAuthStore()

const queueList = ref([])
const currentAppointment = ref(null)
const recordTab = ref('western')
const recordSubmitting = ref(false)

const recordForm = reactive({
  chiefComplaint: '',
  presentIllness: '',
  pastHistory: '',
  physicalExamination: '',
  auxiliaryExamination: '',
  tcmObservation: '',
  tcmAuscultation: '',
  tcmInquiry: '',
  tcmPalpation: '',
  westernDiagnosis: '',
  tcmDiagnosis: '',
  tcmSyndrome: '',
  doctorAdvice: '',
  remark: ''
})

const savedRecordId = ref(null)

const rxType = ref('西药')
const rxDecoctingMethod = ref('')
const rxDirection = ref('')
const rxItems = ref([])
const rxSubmitting = ref(false)
const drugOptions = ref([])

/**
 * 获取今日待诊队列
 */
async function fetchQueue() {
  try {
    const res = await api.get('/appointments/queue', { params: { date: dayjs().format('YYYY-MM-DD') } })
    queueList.value = res.data
  } catch { queueList.value = [] }
}

/**
 * 选择患者开始接诊
 * @param {Object} item - 队列项
 */
async function selectPatient(item) {
  currentAppointment.value = item
  savedRecordId.value = null
  resetRecordForm()
  await fetchDrugs()
}

function resetRecordForm() {
  recordForm.chiefComplaint = ''
  recordForm.presentIllness = ''
  recordForm.pastHistory = ''
  recordForm.physicalExamination = ''
  recordForm.auxiliaryExamination = ''
  recordForm.tcmObservation = ''
  recordForm.tcmAuscultation = ''
  recordForm.tcmInquiry = ''
  recordForm.tcmPalpation = ''
  recordForm.westernDiagnosis = ''
  recordForm.tcmDiagnosis = ''
  recordForm.tcmSyndrome = ''
  recordForm.doctorAdvice = ''
  recordForm.remark = ''
  rxItems.value = []
  rxType.value = '西药'
  rxDecoctingMethod.value = ''
  rxDirection.value = ''
}

/**
 * 开始接诊
 */
async function handleStartVisit() {
  try {
    await api.put(`/appointments/${currentAppointment.value.id}/start-visit`)
    ElMessage.success('已开始接诊')
    currentAppointment.value.status = 'InProgress'
    await fetchQueue()
  } catch { /* handled */ }
}

/**
 * 结束就诊
 */
async function handleCompleteVisit() {
  try {
    await ElMessageBox.confirm('确定结束该患者的就诊吗？', '确认')
    await api.put(`/appointments/${currentAppointment.value.id}/complete`)
    ElMessage.success('就诊完成')
    currentAppointment.value = null
    resetRecordForm()
    await fetchQueue()
  } catch { /* 取消 */ }
}

/**
 * 获取药品列表
 */
async function fetchDrugs() {
  try {
    const res = await api.get('/drugs/active')
    drugOptions.value = res.data
  } catch { drugOptions.value = [] }
}

/**
 * 添加处方药品行
 */
function addRxItem() {
  rxItems.value.push({
    drugId: null,
    drugName: '',
    specification: '',
    quantity: 1,
    unitPrice: 0,
    dosage: '',
    frequency: 'tid',
    usage: '口服',
    days: '7',
    remark: ''
  })
}

/**
 * 选择药品后自动填充信息
 * @param {number} drugId - 药品ID
 * @param {Object} row - 处方行
 */
function onDrugSelected(drugId, row) {
  const drug = drugOptions.value.find(d => d.id === drugId)
  if (drug) {
    row.drugName = drug.name
    row.specification = drug.specification
    row.unitPrice = drug.price
  }
}

/**
 * 保存病历
 */
async function handleSaveRecord() {
  if (!currentAppointment.value) return

  recordSubmitting.value = true
  try {
    const payload = {
      patientId: currentAppointment.value.patientId || 0,
      appointmentId: currentAppointment.value.id,
      ...recordForm
    }

    if (savedRecordId.value) {
      await api.put(`/medicalrecords/${savedRecordId.value}`, payload)
      ElMessage.success('病历更新成功')
    } else {
      const res = await api.post('/medicalrecords', payload)
      savedRecordId.value = res.data.id
      ElMessage.success('病历保存成功')
    }
  } finally {
    recordSubmitting.value = false
  }
}

/**
 * 提交处方
 */
async function handleSavePrescription() {
  if (!savedRecordId.value) {
    ElMessage.warning('请先保存病历')
    return
  }
  if (rxItems.value.length === 0) {
    ElMessage.warning('请添加药品')
    return
  }

  rxSubmitting.value = true
  try {
    await api.post('/prescriptions', {
      medicalRecordId: savedRecordId.value,
      type: rxType.value,
      decoctingMethod: rxType.value === '中药饮片' ? rxDecoctingMethod.value || null : null,
      direction: rxDirection.value || null,
      items: rxItems.value.map(item => ({
        drugId: item.drugId,
        drugName: item.drugName,
        specification: item.specification,
        quantity: item.quantity,
        dosage: item.dosage || null,
        frequency: item.frequency || null,
        usage: item.usage || null,
        days: item.days || null,
        remark: item.remark || null,
        unitPrice: item.unitPrice
      }))
    })
    ElMessage.success('处方提交成功')
    rxItems.value = []
    rxType.value = '西药'
    rxDecoctingMethod.value = ''
    rxDirection.value = ''
  } finally {
    rxSubmitting.value = false
  }
}

onMounted(fetchQueue)
</script>

<style scoped>
.queue-card {
  padding: 10px 12px;
  border-bottom: 1px solid #ebeef5;
  cursor: pointer;
  transition: background 0.2s;
}

.queue-card:hover {
  background: #f5f7fa;
}

.queue-card.active {
  background: #ecf5ff;
  border-left: 3px solid #409eff;
}
</style>
