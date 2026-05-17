<template>
  <div>
    <el-card>
      <template #header>
        <div style="display: flex; justify-content: space-between; align-items: center;">
          <span>患者列表</span>
          <el-button type="primary" @click="showDialog = true">新增患者</el-button>
        </div>
      </template>

      <!-- 搜索 -->
      <el-form :inline="true" style="margin-bottom: 16px;">
        <el-form-item>
          <el-input v-model="keyword" placeholder="姓名/手机号/身份证" clearable @clear="fetchData" @keyup.enter="fetchData" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="fetchData">查询</el-button>
        </el-form-item>
      </el-form>

      <!-- 表格 -->
      <el-table :data="list" v-loading="loading" stripe style="width: 100%">
        <el-table-column prop="id" label="ID" width="60" />
        <el-table-column prop="name" label="姓名" width="100" />
        <el-table-column prop="gender" label="性别" width="60" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="idCard" label="身份证号" width="180" />
        <el-table-column prop="dateOfBirth" label="出生日期" width="120">
          <template #default="{ row }">
            {{ row.dateOfBirth ? dayjs(row.dateOfBirth).format('YYYY-MM-DD') : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="allergies" label="过敏史" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small">编辑</el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div style="display: flex; justify-content: flex-end; margin-top: 16px;">
        <el-pagination
          v-model:current-page="page"
          :page-size="pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="fetchData"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/api'
import dayjs from 'dayjs'

const loading = ref(false)
const list = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const keyword = ref('')
const showDialog = ref(false)

async function fetchData() {
  loading.value = true
  try {
    const res = await api.get('/patients', {
      params: { page: page.value, pageSize: pageSize.value, keyword: keyword.value }
    })
    list.value = res.data.items
    total.value = res.data.total
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>
