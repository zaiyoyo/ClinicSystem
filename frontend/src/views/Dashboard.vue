<template>
  <el-row :gutter="20">
    <el-col :span="6">
      <el-card shadow="hover">
        <div style="text-align: center; padding: 20px 0;">
          <div style="font-size: 36px; color: #409eff; font-weight: bold;">0</div>
          <div style="color: #999; margin-top: 8px; font-size: 14px;">今日挂号</div>
        </div>
      </el-card>
    </el-col>
    <el-col :span="6">
      <el-card shadow="hover">
        <div style="text-align: center; padding: 20px 0;">
          <div style="font-size: 36px; color: #67c23a; font-weight: bold;">0</div>
          <div style="color: #999; margin-top: 8px; font-size: 14px;">今日就诊</div>
        </div>
      </el-card>
    </el-col>
    <el-col :span="6">
      <el-card shadow="hover">
        <div style="text-align: center; padding: 20px 0;">
          <div style="font-size: 36px; color: #e6a23c; font-weight: bold;">¥0</div>
          <div style="color: #999; margin-top: 8px; font-size: 14px;">今日收入</div>
        </div>
      </el-card>
    </el-col>
    <el-col :span="6">
      <el-card shadow="hover">
        <div style="text-align: center; padding: 20px 0;">
          <div style="font-size: 36px; color: #f56c6c; font-weight: bold;">0</div>
          <div style="color: #999; margin-top: 8px; font-size: 14px;">待诊患者</div>
        </div>
      </el-card>
    </el-col>
  </el-row>

  <el-card style="margin-top: 20px;">
    <template #header>
      <span>欢迎使用门诊管理系统</span>
    </template>
    <p>当前用户：{{ authStore.user?.displayName }}</p>
    <p>角色：{{ authStore.role }}</p>
    <p style="margin-top: 12px; color: #999;">系统已就绪，请通过左侧菜单开始工作。</p>
  </el-card>
</template>

<script setup>
import { useAuthStore } from '@/stores/auth'
const authStore = useAuthStore()
</script>
