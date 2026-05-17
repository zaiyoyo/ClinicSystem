<template>
  <div class="main-layout">
    <!-- 侧边栏 -->
    <div class="sidebar">
      <div class="logo">门诊管理系统</div>
      <el-menu
        :default-active="route.path"
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409eff"
        router
        style="flex: 1; border-right: none;"
      >
        <el-menu-item index="/dashboard">
          <el-icon><Odometer /></el-icon>
          <span>工作台</span>
        </el-menu-item>
        <el-menu-item index="/appointments">
          <el-icon><Document /></el-icon>
          <span>挂号管理</span>
        </el-menu-item>
        <el-menu-item index="/patients">
          <el-icon><UserFilled /></el-icon>
          <span>患者管理</span>
        </el-menu-item>
        <el-menu-item index="/departments">
          <el-icon><Setting /></el-icon>
          <span>科室管理</span>
        </el-menu-item>
      </el-menu>
    </div>

    <!-- 主区域 -->
    <div class="main-area">
      <div class="header">
        <el-breadcrumb>
          <el-breadcrumb-item :to="'/'">首页</el-breadcrumb-item>
          <el-breadcrumb-item v-if="route.meta.title">{{ route.meta.title }}</el-breadcrumb-item>
        </el-breadcrumb>
        <div>
          <el-dropdown @command="handleCommand">
            <span style="cursor: pointer;">
              {{ authStore.user?.displayName || '用户' }}
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="logout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
      <div class="content">
        <router-view />
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

function handleCommand(command) {
  if (command === 'logout') {
    authStore.logout()
    router.push('/login')
  }
}
</script>
