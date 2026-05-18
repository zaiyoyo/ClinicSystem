<template>
  <div>
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card shadow="hover">
          <div style="text-align: center; padding: 20px 0;">
            <div style="font-size: 36px; color: #409eff; font-weight: bold;">{{ stats.todayAppointments }}</div>
            <div style="color: #999; margin-top: 8px; font-size: 14px;">今日挂号</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div style="text-align: center; padding: 20px 0;">
            <div style="font-size: 36px; color: #67c23a; font-weight: bold;">{{ stats.todayVisits }}</div>
            <div style="color: #999; margin-top: 8px; font-size: 14px;">今日就诊</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div style="text-align: center; padding: 20px 0;">
            <div style="font-size: 36px; color: #e6a23c; font-weight: bold;">¥{{ stats.todayIncome }}</div>
            <div style="color: #999; margin-top: 8px; font-size: 14px;">今日收入</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="hover">
          <div style="text-align: center; padding: 20px 0;">
            <div style="font-size: 36px; color: #f56c6c; font-weight: bold;">{{ stats.waitingPatients }}</div>
            <div style="color: #999; margin-top: 8px; font-size: 14px;">待诊患者</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-card style="margin-top: 20px;">
      <template #header>
        <span>欢迎使用门诊管理系统</span>
      </template>
      <el-descriptions :column="2" border>
        <el-descriptions-item label="当前用户">{{ authStore.user?.displayName }}</el-descriptions-item>
        <el-descriptions-item label="角色">{{ roleLabel }}</el-descriptions-item>
        <el-descriptions-item label="所属科室">{{ authStore.user?.department || '未分配' }}</el-descriptions-item>
        <el-descriptions-item label="登录账号">{{ authStore.user?.username }}</el-descriptions-item>
      </el-descriptions>
      <p style="margin-top: 16px; color: #999;">系统已就绪，请通过左侧菜单开始工作。</p>
    </el-card>
  </div>
</template>

<script setup>
/**
 * 工作台页面
 * 显示今日运营统计数据（挂号数、就诊数、收入、待诊数）和当前用户信息
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import api from '@/api'

const authStore = useAuthStore()

const roleLabelMap = {
  Admin: '管理员', Doctor: '医生', Nurse: '护士',
  Cashier: '收银员', Pharmacist: '药剂师', Boss: '院长'
}
const roleLabel = computed(() => roleLabelMap[authStore.role] || '未知')

const stats = reactive({
  todayAppointments: 0,
  todayVisits: 0,
  todayIncome: 0,
  waitingPatients: 0
})

/**
 * 获取今日统计数据
 */
async function fetchStats() {
  try {
    const res = await api.get('/dashboard/today-stats')
    Object.assign(stats, res.data)
  } catch {
    /* 保持默认值0 */
  }
}

onMounted(fetchStats)
</script>
