<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>科室管理</span>
          <el-button type="primary" @click="showDialog = true">新增科室</el-button>
        </div>
      </template>

      <el-table :data="list" v-loading="loading" stripe>
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="name" label="科室名称" />
        <el-table-column prop="description" label="描述" show-overflow-tooltip />
        <el-table-column prop="sortOrder" label="排序" width="80" />
        <el-table-column label="操作" width="150">
          <template #default>
            <el-button type="primary" link size="small">编辑</el-button>
            <el-button type="danger" link size="small">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/api'

const loading = ref(false)
const list = ref([])
const showDialog = ref(false)

async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/departments')
    list.value = res.data
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>
