<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { fetchDrawdownList } from '../api/drawdown'
  import type { DrawdownListItem } from '../types/drawdown'
  import { useRouter } from 'vue-router'

  const router = useRouter()

  const periodMonths = ref(3)
  const loading = ref(false)
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

  async function load() {
    loading.value = true
    try {
      items.value = await fetchDrawdownList(periodMonths.value)
    } finally {
      loading.value = false
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

  onMounted(load)
</script>

<template>
  <v-container>
    <v-card class="mx-auto mt-6 pa-6" max-width="1000">
      <v-card-title class="text-h6">株価分析</v-card-title>
      <v-card-text>
        <v-row>
          <v-col cols="3">
            <v-select
              v-model="periodMonths"
              :items="periodOptions"
              density="compact"
              hide-details
              variant="outlined"
              label="対象期間"
              style="max-width: 180px"
              class="mr-3"
            />
          </v-col>
          <v-col cols="2">
            <v-btn color="primary" block :loading="loading" :disabled="loading" @click="load">
              実行
            </v-btn>
          </v-col>
        </v-row>

        <!-- ===== テーブル ===== -->
        <v-data-table
          :headers="headers"
          :items="items"
          :items-per-page="-1"
          hide-default-footer
          height="300"
          fixed-header
          class="drawdown-table"
          @click:row="handleRowClick"
        >
          <!-- ★ 列幅制御 -->
          <template #colgroup>
            <col style="width: 25%" />
            <col style="width: 25%" />
            <col style="width: 25%" />
            <col style="width: 25%" />
          </template>

          <template #headers>
            <tr>
              <th class="text-center">銘柄</th>
              <th class="text-center">最高値</th>
              <th class="text-center">現在値</th>
              <th class="text-center">下落率</th>
            </tr>
          </template>

          <template #item.symbol="{ value }">
            <span class="font-weight-medium">
              {{ value }}
            </span>
          </template>

          <template #item.peakPrice="{ value }">
            {{ value.toFixed(2) }}
          </template>

          <template #item.currentPrice="{ value }">
            <strong>{{ value.toFixed(2) }}</strong>
          </template>

          <template #item.drawdownRate="{ value }">
            <v-chip
              size="small"
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
