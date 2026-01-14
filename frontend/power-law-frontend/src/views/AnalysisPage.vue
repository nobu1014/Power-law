<script setup lang="ts">
  import { ref, onMounted, computed } from 'vue'
  import { api } from '../lib/api'

  import type { ChartOptions } from 'chart.js'

  /* ===== Chart.js ===== */
  import { Line } from 'vue-chartjs'
  import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Tooltip,
    Legend,
  } from 'chart.js'

  ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Legend)

  /* =============================
   * 型定義
   * ============================= */
  type WatchlistItem = {
    symbol: string
    market: string
  }

  type EpsTableRowEx = {
    period: string
    value: number | null
    change: number | null
    changeRate: number | null
    isAverage?: boolean
  }

  type PerTableRowEx = {
    period: string
    value: number | null
    change: number | null
    changeRate: number | null
  }

  /* =============================
   * 状態
   * ============================= */
  const watchlist = ref<WatchlistItem[]>([])
  const selectedSymbol = ref('')

  const displayMode = ref<'price' | 'eps' | 'per'>('price')
  const loading = ref(false)
  const result = ref<any>(null)

  /* ---- 条件 ---- */
  const baseYears = ref<number | null>(1) //デフォルト
  const epsRange = ref(4) // デフォルト

  /* =============================
   * 初期ロード
   * ============================= */
  onMounted(async () => {
    const res = await api.get<WatchlistItem[]>('/analysis/watchlist')
    watchlist.value = res.data
    if (watchlist.value.length > 0) {
      selectedSymbol.value = watchlist.value[0]!.symbol
    }
  })

  /* =============================
   * 分析実行
   * ============================= */
  const execute = async () => {
    // 入力が空・空白のみなら実行しない
    if (!selectedSymbol.value || !selectedSymbol.value.trim()) return

    // 旧結果をクリア（表示のブレ防止）
    result.value = null

    loading.value = true

    // 銘柄はここで正規化（backend と完全一致させる）
    const symbol = selectedSymbol.value.trim().toUpperCase()

    const res = await api.post('/analysis/execute', {
      symbol,
      market: 'US',
      baseYears: baseYears.value ?? undefined,
      epsRange: epsRange.value ?? undefined,
    })

    result.value = res.data
    loading.value = false
  }

  /* =============================
   * EPS / PER 表
   * ============================= */
  const epsTable = computed<EpsTableRowEx[]>(() => {
    if (!result.value) return []

    const rows = [...(result.value.eps.table ?? [])] // 新 → 旧

    const decisionRows = rows.map((r: any, i: number) => {
      const prev = rows[i + 1]

      const cur = r.value != null ? Number(r.value) : null
      const prevVal = prev?.value != null ? Number(prev.value) : null

      const change = cur != null && prevVal != null ? cur - prevVal : null

      let changeRate: number | null = null

      if (change !== null && prevVal !== null && prevVal !== 0) {
        changeRate = change / Math.abs(prevVal)
      }

      return {
        period: r.period,
        value: cur,
        change,
        changeRate, // ← TS完全OK
        isAverage: false,
      }
    })

    /* ===== 平均 ===== */
    const values = decisionRows.map(r => r.value).filter(v => v != null) as number[]

    const avgRows: EpsTableRowEx[] = []

    for (let n = 2; n <= Math.min(epsRange.value, values.length); n++) {
      const avg = values.slice(0, n).reduce((a, b) => a + b, 0) / n

      const prevAvg = n > 2 ? values.slice(0, n - 1).reduce((a, b) => a + b, 0) / (n - 1) : null

      const change = prevAvg != null ? avg - prevAvg : null

      let changeRate: number | null = null
      if (change !== null && prevAvg !== null && prevAvg !== 0) {
        changeRate = change / Math.abs(prevAvg)
      }

      avgRows.push({
        period: `${n}AVG`,
        value: avg,
        change,
        changeRate,
        isAverage: true,
      })
    }

    return [...decisionRows, ...avgRows]
  })

  //EPS
  const perTable = computed<PerTableRowEx[]>(() => {
    if (!result.value) return []

    const rows = [...result.value.per.table]

    return rows.map((row: any, i: number) => {
      const prev = rows[i + 1]

      const cur = row.value != null ? Number(row.value) : null
      const prevVal = prev?.value != null ? Number(prev.value) : null

      const change = cur != null && prevVal != null ? cur - prevVal : null

      let changeRate: number | null = null
      if (change !== null && prevVal !== null && prevVal !== 0) {
        changeRate = change / Math.abs(prevVal)
      }

      return {
        period: row.period,
        value: cur,
        change,
        changeRate,
      }
    })
  })

  /* =============================
   * 株価 グラフ
   * ============================= */
  const priceChartData = computed(() => {
    if (!result.value) return null

    const prices = result.value.price.prices ?? []

    return {
      labels: prices.map((p: any) => new Date(p.date).toLocaleDateString()),
      datasets: [
        {
          label: '株価',
          data: prices.map((p: any) => p.value),

          // ===== 見た目 =====
          borderColor: '#1E88E5', // 少し深めのブルー
          backgroundColor: 'rgba(30,136,229,0.08)',

          borderWidth: 2.5,
          tension: 0.25, // なだらか（EPSより直線寄り）
          fill: true,

          // ===== ポイント =====
          pointRadius: 0, // 通常は非表示
          pointHoverRadius: 5,
          pointHoverBackgroundColor: '#1E88E5',
          pointHoverBorderWidth: 2,

          // ===== その他 =====
          spanGaps: true,
        },
      ],
    }
  })

  /* =============================
   * EPS グラフ
   * ============================= */

  const epsChartData = computed(() => {
    if (!result.value) return null

    const list = [...(result.value.eps.epsList ?? [])].reverse()

    const labels = list.map((x: any) => x.period)
    const values = list.map((x: any) => Number(x.value))

    const datasets: any[] = [
      {
        label: 'EPS',
        data: values,
        tension: 0.35,
        borderWidth: 3,
        borderColor: '#1976D2', // Vuetify primary
        backgroundColor: 'rgba(25,118,210,0.15)',
        fill: true,
        pointRadius: 2,
        pointHoverRadius: 6,
        pointHoverBorderWidth: 2,
      },
    ]

    // 平均線
    const n = Math.min(epsRange.value ?? 4, values.length)
    if (n >= 2) {
      const avg = values.slice(-n).reduce((a, b) => a + b, 0) / n

      datasets.push({
        label: `${n}期平均`,
        data: Array(values.length).fill(avg),
        borderColor: '#9E9E9E',
        borderDash: [6, 6],
        borderWidth: 2,
        pointRadius: 0,
      })
    }

    return { labels, datasets }
  })

  /* =============================
   * PER グラフ
   * ============================= */
  const perChartData = computed(() => {
    if (!result.value) return null

    const list = [...(result.value.per.perList ?? [])].reverse()

    const labels = list.map((x: any) => x.period)
    const values = list.map((x: any) => Number(x.value))

    const datasets: any[] = [
      {
        label: 'PER',
        data: values,
        tension: 0.35,
        borderWidth: 3,
        borderColor: '#FB8C00', // オレンジ系（EPSと差別化）
        backgroundColor: 'rgba(251,140,0,0.15)',
        fill: true,
        pointRadius: 2,
        pointHoverRadius: 6,
        pointHoverBorderWidth: 2,
      },
    ]

    const n = Math.min(epsRange.value ?? 4, values.length)
    if (n >= 2) {
      const avg = values.slice(-n).reduce((a, b) => a + b, 0) / n

      datasets.push({
        label: `${n}期平均`,
        data: Array(values.length).fill(avg),
        borderColor: '#9E9E9E',
        borderDash: [6, 6],
        borderWidth: 2,
        pointRadius: 0,
      })
    }

    return { labels, datasets }
  })

  const chartData = computed(() => {
    switch (displayMode.value) {
      case 'price':
        return priceChartData.value
      case 'eps':
        return epsChartData.value
      case 'per':
        return perChartData.value
      default:
        return null
    }
  })

  const chartOptions = computed<ChartOptions<'line'>>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index',
      intersect: false,
    },
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          usePointStyle: true,
        },
      },
      tooltip: {
        callbacks: {
          label: ctx => `${ctx.dataset.label}: ${Number(ctx.parsed.y).toFixed(2)}`,
        },
      },
    },
    scales: {
      x: {
        type: 'category',
        grid: { display: false },
      },
      y: {
        type: 'linear',
        ticks: {
          callback: tickValue => {
            // tickValue: string | number
            if (typeof tickValue === 'number') return tickValue.toFixed(2)
            const n = Number(tickValue)
            return Number.isFinite(n) ? n.toFixed(2) : tickValue
          },
        },
      },
    },
  }))
</script>

<template>
  <v-container>
    <v-card class="mx-auto mt-6 pa-6" max-width="1000">
      <v-card-title class="text-h6">株価分析</v-card-title>

      <v-card-text>
        <!-- 銘柄 & 実行 -->
        <v-row>
          <v-col cols="4">
            <v-combobox
              v-model="selectedSymbol"
              :items="watchlist.map(w => w.symbol)"
              label="銘柄"
              density="compact"
              variant="outlined"
              hide-details
              clearable
              placeholder="例：AAPL"
            />
          </v-col>

          <v-col cols="2">
            <v-btn color="primary" block :loading="loading" :disabled="loading" @click="execute">
              分析実行
            </v-btn>
          </v-col>

          <v-col cols="2">
            <v-btn block disabled>スプシ出力</v-btn>
          </v-col>
        </v-row>

        <!-- 任意条件 -->
        <v-row>
          <v-col cols="12" md="5">
            <v-card class="pa-4" variant="elevated">
              <div class="text-subtitle-2 font-weight-medium mb-3">📈 株価分析・基準期間</div>

              <v-chip-group
                v-model="baseYears"
                column
                mandatory
                selected-class="bg-primary text-white"
              >
                <v-chip
                  v-for="year in [1, 2, 3, 4, 5]"
                  :key="year"
                  :value="year"
                  variant="outlined"
                  class="ma-1"
                >
                  直近 {{ year }} 年
                </v-chip>
              </v-chip-group>

              <div class="text-caption text-medium-emphasis mt-2">
                ※ 短期（1M / 2M / 6M）・YTD は自動計算
              </div>
            </v-card>
          </v-col>
          <v-col cols="12" md="7">
            <!-- EPS / PER 分析レンジ -->
            <v-card class="pa-4 mb-4" variant="elevated">
              <div class="text-subtitle-2 font-weight-medium mb-3">📊 EPS / PER 分析レンジ</div>

              <v-chip-group v-model="epsRange" mandatory selected-class="bg-primary text-white">
                <v-chip :value="4" variant="outlined">直近 4 期</v-chip>
                <v-chip :value="8" variant="outlined">直近 8 期</v-chip>
                <v-chip :value="12" variant="outlined">直近 12 期</v-chip>
                <v-chip :value="16" variant="outlined">直近 16 期</v-chip>
              </v-chip-group>

              <div class="text-caption text-medium-emphasis mt-2">
                ※ EPS / PER の計算対象となる決算データの範囲
              </div>
            </v-card>
          </v-col>
        </v-row>

        <!-- 表示切替 -->
        <v-btn-toggle v-model="displayMode" class="my-4" mandatory divided>
          <v-btn value="price">株価</v-btn>
          <v-btn value="eps">EPS</v-btn>
          <v-btn value="per">PER</v-btn>
        </v-btn-toggle>

        <!-- グラフ -->
        <v-card v-if="chartData" class="pa-4 mb-4" variant="outlined">
          <!-- タイトル -->
          <div class="d-flex align-center justify-space-between mb-2">
            <div>
              <div class="text-subtitle-1 font-weight-medium">
                {{
                  displayMode === 'price'
                    ? '株価推移'
                    : displayMode === 'eps'
                      ? 'EPS 推移'
                      : 'PER 推移'
                }}
              </div>
              <div class="text-caption text-medium-emphasis">
                {{ displayMode === 'price' ? '日次ベース' : `直近 ${epsRange} 期（平均線付き）` }}
              </div>
            </div>
          </div>

          <!-- グラフ本体 -->
          <div class="chart-wrapper" style="height: 320px">
            <Line :data="chartData" :options="chartOptions" />
          </div>
        </v-card>

        <!-- 株価テキスト -->
        <v-card v-if="displayMode === 'price' && result" class="pa-4 mb-4" variant="outlined">
          <v-row align="center">
            <!-- 現在値 -->
            <v-col cols="12" md="4">
              <div class="text-caption text-medium-emphasis">現在値</div>
              <div class="text-h4 font-weight-bold text-primary">
                {{ result.price.currentPrice?.toFixed(2) }}
              </div>
            </v-col>

            <!-- 平均 -->
            <v-col cols="12" md="8">
              <div class="text-caption text-medium-emphasis mb-2">移動平均</div>
              <v-row dense>
                <v-col v-for="(v, k) in result.price.fixedAverages" :key="k" cols="6" md="4">
                  <v-chip size="small" variant="tonal" color="primary" class="ma-1">
                    {{ k }}：{{ v.toFixed(2) }}
                  </v-chip>
                </v-col>
              </v-row>
            </v-col>
          </v-row>
        </v-card>

        <!-- EPS 表 -->
        <v-data-table
          v-if="displayMode === 'eps'"
          :headers="[
            { title: '期間', key: 'period' },
            { title: 'EPS', key: 'value' },
            { title: '変動', key: 'change' },
            { title: '変動率', key: 'changeRate' },
          ]"
          :items="epsTable"
          :items-per-page="-1"
          hide-default-footer
          height="300"
          fixed-header
        >
          <template #item.value="{ value }">
            <span v-if="value !== null">{{ value.toFixed(2) }}</span>
          </template>

          <!-- PER change -->
          <template #item.change="{ value }">
            <span v-if="value !== null" :class="value >= 0 ? 'text-success' : 'text-error'">
              {{ value.toFixed(2) }}
            </span>
          </template>

          <!-- PER changeRate -->
          <template #item.changeRate="{ value }">
            <span v-if="value !== null" :class="value >= 0 ? 'text-success' : 'text-error'">
              {{ value.toFixed(1) }}%
            </span>
          </template>
        </v-data-table>

        <!-- PER 表 -->
        <v-data-table
          v-if="displayMode === 'per'"
          :headers="[
            { title: '期間', key: 'period' },
            { title: 'PER', key: 'value' },
            { title: '変動', key: 'change' },
            { title: '変動率', key: 'changeRate' },
          ]"
          :items="perTable"
          :items-per-page="-1"
          hide-default-footer
          height="300"
          fixed-header
        >
          <template #item.change="{ value }">
            <span v-if="value !== null" :class="value >= 0 ? 'text-success' : 'text-error'">
              {{ value.toFixed(2) }}
            </span>
          </template>

          <template #item.changeRate="{ value }">
            <span v-if="value !== null" :class="value >= 0 ? 'text-success' : 'text-error'">
              {{ value.toFixed(1) }}%
            </span>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>
  </v-container>
</template>

<style lang="css">
  .chart-wrapper {
    padding: 12px;
    border-radius: 8px;
    background: linear-gradient(to bottom, rgba(0, 0, 0, 0.02), rgba(0, 0, 0, 0));
  }
</style>
