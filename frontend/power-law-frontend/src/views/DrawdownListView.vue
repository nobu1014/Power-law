<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { fetchDrawdownList, refreshDrawdownData } from '../api/drawdown'
  import type { DrawdownListItem } from '../types/drawdown'
  import { useRouter } from 'vue-router'

  const router = useRouter()

  const periodMonths = ref(3)
  const loadingList = ref(false) // 下落チェック取得用
  const loadingRefresh = ref(false) // 最新データ取得用
  const items = ref<DrawdownListItem[]>([])

  const periodOptions = [
    { title: '直近1か月', value: 1 },
    { title: '直近3か月', value: 3 },
    { title: '直近6か月', value: 6 },
    { title: '直近1年', value: 12 },
  ]

  const headers = [
    { title: '銘柄', key: 'symbol', align: 'center' },
    { title: '最高値', key: 'peakPrice', align: 'center' },
    { title: '現在値', key: 'currentPrice', align: 'center' },
    { title: '下落率', key: 'drawdownRate', align: 'center' },
  ] as const

  async function loadList() {
    loadingList.value = true
    try {
      items.value = await fetchDrawdownList(periodMonths.value)
    } finally {
      loadingList.value = false
    }
  }

  async function refreshLatestData() {
    loadingRefresh.value = true
    try {
      await refreshDrawdownData()
      // 取得完了後、最新DBで一覧を再取得
      await loadList()
    } finally {
      loadingRefresh.value = false
    }
  }

  function goDetail(symbol: string) {
    router.push({ name: 'analysis', query: { symbol } })
  }

  function handleRowClick(_: unknown, row: DrawdownListItem) {
    goDetail(row.symbol)
  }

  function drawdownColor(rate: number) {
    if (rate <= -20) return 'red-darken-3'
    if (rate <= -10) return 'red-darken-1'
    if (rate < 0) return 'orange-darken-1'
    return 'grey'
  }

  onMounted(loadList)
</script>

<template>
  <v-container fluid class="pa-0 pa-md-4">
    <v-card class="mx-auto mt-2 mt-md-6 pa-2 pa-md-6 drawdown-root" max-width="1000">
      <v-card-title class="text-h6">下落チェック</v-card-title>

      <v-card-text class="pa-0 pa-md-4">
        <v-row class="mt-1" dense>
          <v-col cols="12" md="3">
            <v-select
              v-model="periodMonths"
              :items="periodOptions"
              density="compact"
              hide-details
              variant="outlined"
              label="対象期間"
              class="w-100"
            />
          </v-col>

          <!-- 📉 下落チェック実行 -->
          <v-col cols="12" md="3">
            <v-btn
              color="primary"
              block
              :loading="loadingList"
              :disabled="loadingList || loadingRefresh"
              @click="loadList"
            >
              下落チェック実行
            </v-btn>
          </v-col>

          <!-- 🔄 最新データ取得 -->
          <v-col cols="12" md="3">
            <v-btn
              color="secondary"
              block
              :loading="loadingRefresh"
              :disabled="loadingRefresh || loadingList"
              @click="refreshLatestData"
            >
              最新データ取得
            </v-btn>
          </v-col>
        </v-row>

        <v-data-table
          :headers="headers"
          :items="items"
          :items-per-page="-1"
          hide-default-footer
          height="300"
          fixed-header
          class="drawdown-table mt-2"
          density="compact"
          :mobile-breakpoint="0"
          @click:row="handleRowClick"
        >
          <template #item.symbol="{ value }">
            <span class="font-weight-medium">{{ value }}</span>
          </template>

          <template #item.peakPrice="{ value }">
            {{ value.toFixed(2) }}
          </template>

          <template #item.currentPrice="{ value }">
            <strong>{{ value.toFixed(2) }}</strong>
          </template>

          <template #item.drawdownRate="{ value }">
            <v-chip
              size="x-small"
              :color="drawdownColor(value)"
              variant="flat"
              class="font-weight-medium"
            >
              {{ value.toFixed(2) }}%
            </v-chip>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </v-container>
</template>

<style lang="css">
  /* スマホはカード感を消して幅を最大化 */
  @media (max-width: 600px) {
    .drawdown-root {
      border-radius: 0;
      box-shadow: none;
    }
  }
</style>
