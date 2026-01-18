<script setup lang="ts">
  /**
   * AnalysisPage.vue（Phase0 完成形）
   *
   * 【目的】
   * - 仕様を満たし、分析実行画面が正常に動作すること
   *
   * 【前提】
   * - backend の /api/analysis/execute は AnalysisResultDto を返す
   *   {
   *     symbol, range, priceSeries, epsSeries, metrics
   *   }
   *
   * 【重要】
   * - 旧形式（result.price.prices / result.eps.table / result.per...）は使わない
   * - 描画は v-if="chartData" や v-if="result" でガードしてクラッシュを防ぐ
   */

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
   * 型定義（DTO準拠）
   * ============================= */

  /** Watchlist */
  type WatchlistItem = {
    symbol: string
    market: string
  }

  /** Backend DTO: TimeSeriesPointDto に合わせる
   * - priceSeries.date: "yyyy-MM-dd"
   * - epsSeries.date  : "2024Q3" など（Period）
   */
  type TimeSeriesPointDto = {
    date: string
    value: number
  }

  /** Backend DTO: AnalysisMetricsDto（Phase0） */
  type AnalysisMetricsDto = {
    oneMonth: number
    threeMonths: number
    sixMonths: number
    oneYear: number
  }

  /** Backend DTO: AnalysisRangeDto */
  type AnalysisRangeDto = {
    from: string
    to: string
  }

  /** Backend DTO: AnalysisResultDto */
  type AnalysisResultDto = {
    symbol: string
    range: AnalysisRangeDto
    priceSeries: TimeSeriesPointDto[]
    epsSeries: TimeSeriesPointDto[]
    metrics: AnalysisMetricsDto
  }

  /** 表（EPS表示用） */
  type EpsTableRowEx = {
    period: string
    value: number | null
    change: number | null
    changeRate: number | null
    isAverage?: boolean
  }

  /** 表（PER表示用：フロントで計算） */
  type PerTableRowEx = {
    period: string
    value: number | null
    change: number | null
    changeRate: number | null
  }

  type PricePeriodDef = {
    key: string
    label: string
    months?: number
    years?: number
    ytd?: boolean
  }

  /* =============================
   * 状態
   * ============================= */
  const watchlist = ref<WatchlistItem[]>([])
  const selectedSymbol = ref('')

  const displayMode = ref<'price' | 'eps' | 'per'>('price')
  const loading = ref(false)
  const importing = ref(false)

  /**
   * API結果（AnalysisResultDto）
   * - null の間は描画しない
   */
  const result = ref<AnalysisResultDto | null>(null)

  /* ---- 条件 ---- */
  const baseYears = ref<number | null>(1)
  const epsRange = ref(4)

  /* ---- 日付指定（空なら最新） ---- */
  const selectedDate = ref<string>('') // "YYYY-MM-DD"

  /* ---- 変動率（画面表示用：フロント計算） ---- */
  const priceChange = ref<{
    baseDate: string
    basePrice: number | null
    m1: number | null
    m3: number | null
    m6: number | null
    y1: number | null
  }>({
    baseDate: '',
    basePrice: null,
    m1: null,
    m3: null,
    m6: null,
    y1: null,
  })

  /* =============================
   * 初期ロード（watchlist）
   * ============================= */
  onMounted(async () => {
    const res = await api.get<WatchlistItem[]>('/analysis/watchlist')
    watchlist.value = res.data
    if (watchlist.value.length > 0) {
      selectedSymbol.value = watchlist.value[0]!.symbol
    }
  })

  /* =============================
   * ユーティリティ
   * ============================= */

  /**
   * 価格系列（DTOから）
   * - date: "yyyy-MM-dd" を Date に変換
   * - value: number に変換
   * - 日付昇順に整列
   */
  const getPriceSeries = () => {
    const prices = result.value?.priceSeries ?? []
    return prices
      .map(p => ({ date: new Date(p.date), value: Number(p.value) }))
      .filter(p => Number.isFinite(p.value) && !Number.isNaN(p.date.getTime()))
      .sort((a, b) => a.date.getTime() - b.date.getTime())
  }

  /**
   * EPS 系列（DTOから）
   * - date は "2024Q3" のような period（Date変換しない）
   * - value は number
   * - APIの並びがどう来ても良いように「そのまま扱う」
   */
  const getEpsSeries = () => {
    const eps = result.value?.epsSeries ?? []
    return eps.map(e => ({
      period: String(e.date),
      value: e.value != null ? Number(e.value) : null,
    }))
  }

  /**
   * 指定日（selectedDate）に最も近い「その日以前」の取引日を探す
   */
  const findClosestOnOrBefore = (series: { date: Date; value: number }[], target: Date) => {
    if (series.length === 0) return null
    for (let i = series.length - 1; i >= 0; i--) {
      if (series[i]!.date.getTime() <= target.getTime()) return series[i]!
    }
    return null
  }

  /**
   * targetDate より過去の「その日以前」で最も近いデータを返す（期間比較用）
   */
  const findValueBefore = (series: { date: Date; value: number }[], targetDate: Date) => {
    const hit = findClosestOnOrBefore(series, targetDate)
    return hit?.value ?? null
  }

  /**
   * 変動率 (base - past) / past * 100
   */
  const calcChangeRatePct = (base: number | null, past: number | null) => {
    if (base == null || past == null || past === 0) return null
    return ((base - past) / Math.abs(past)) * 100
  }

  /**
   * EPSの period を「新しい順」っぽく並べたい場合の簡易ソート
   * - "2024Q3" のような形式に対応
   * - 形式が違う場合は元の順序優先でもOK
   */
  const sortPeriodDesc = (a: string, b: string) => {
    const parse = (p: string) => {
      // 例: 2024Q3
      const m = /^(\d{4})Q(\d)$/.exec(p)
      if (!m) return { y: -1, q: -1 }
      return { y: Number(m[1]), q: Number(m[2]) }
    }
    const pa = parse(a)
    const pb = parse(b)
    if (pa.y !== pb.y) return pb.y - pa.y
    return pb.q - pa.q
  }

  /* =============================
   * 分析実行（Import 組み込み）
   * ============================= */
  const execute = async () => {
    if (!selectedSymbol.value || !selectedSymbol.value.trim()) return

    // 初期化（描画クラッシュ防止）
    result.value = null
    priceChange.value = { baseDate: '', basePrice: null, m1: null, m3: null, m6: null, y1: null }
    loading.value = true
    importing.value = true

    const symbol = selectedSymbol.value.trim().toUpperCase()

    try {
      const res = await api.post<AnalysisResultDto>('/analysis/execute', {
        symbol,
        market: 'US',
        baseYears: baseYears.value ?? undefined,
        epsRange: epsRange.value ?? undefined,
        // backendで日付指定を使う場合はここで渡す（今回はフロントで完結）
        // asOfDate: selectedDate.value || undefined,
      })

      // 非同期後にまとめて代入
      result.value = res.data

      // 変動率計算（フロント責務）
      const series = getPriceSeries()
      if (series.length > 0) {
        const latest = series[series.length - 1]!

        const baseTarget = selectedDate.value ? new Date(selectedDate.value) : latest.date
        const baseHit = findClosestOnOrBefore(series, baseTarget) ?? latest

        const basePrice = baseHit.value
        const baseDateStr = baseHit.date.toLocaleDateString()

        const d1m = new Date(baseHit.date)
        d1m.setMonth(d1m.getMonth() - 1)
        const d3m = new Date(baseHit.date)
        d3m.setMonth(d3m.getMonth() - 3)
        const d6m = new Date(baseHit.date)
        d6m.setMonth(d6m.getMonth() - 6)
        const d1y = new Date(baseHit.date)
        d1y.setFullYear(d1y.getFullYear() - 1)

        const p1m = findValueBefore(series, d1m)
        const p3m = findValueBefore(series, d3m)
        const p6m = findValueBefore(series, d6m)
        const p1y = findValueBefore(series, d1y)

        priceChange.value = {
          baseDate: baseDateStr,
          basePrice,
          m1: calcChangeRatePct(basePrice, p1m),
          m3: calcChangeRatePct(basePrice, p3m),
          m6: calcChangeRatePct(basePrice, p6m),
          y1: calcChangeRatePct(basePrice, p1y),
        }
      }
    } catch (e) {
      console.error(e)
    } finally {
      importing.value = false
      loading.value = false
    }
  }

  /* =============================
   * EPS / PER 表（DTOから生成）
   * ============================= */

  /**
   * EPS 表（DTO epsSeries → 表形式に変換）
   * - change / changeRate は「直前の期」との比較
   * - avg 行も追加（epsRange に応じる）
   */
  const epsTable = computed<EpsTableRowEx[]>(() => {
    if (!result.value) return []

    // 新しい期 → 古い期 の順に揃える（見やすさ）
    const series = getEpsSeries()
      .slice()
      .sort((a, b) => sortPeriodDesc(a.period, b.period))

    // 期ごとの増減計算
    const decisionRows = series.map((r, i) => {
      const prev = series[i + 1]
      const cur = r.value != null ? Number(r.value) : null
      const prevVal = prev?.value != null ? Number(prev.value) : null

      const change = cur != null && prevVal != null ? cur - prevVal : null
      const changeRate =
        change !== null && prevVal !== null && prevVal !== 0
          ? (change / Math.abs(prevVal)) * 100
          : null

      return {
        period: r.period,
        value: cur,
        change,
        changeRate,
        isAverage: false,
      }
    })

    // 平均行（2AVG,3AVG...）
    const values = decisionRows.map(r => r.value).filter(v => v != null) as number[]
    const avgRows: EpsTableRowEx[] = []

    for (let n = 2; n <= Math.min(epsRange.value, values.length); n++) {
      const avg = values.slice(0, n).reduce((a, b) => a + b, 0) / n
      const prevAvg = n > 2 ? values.slice(0, n - 1).reduce((a, b) => a + b, 0) / (n - 1) : null

      const change = prevAvg != null ? avg - prevAvg : null
      const changeRate =
        change !== null && prevAvg !== null && prevAvg !== 0
          ? (change / Math.abs(prevAvg)) * 100
          : null

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

  /**
   * PER 表（フロントで計算）
   * - 最新株価（指定日 or 最新）を基準に、PER = price / EPS
   * - EPSが0/nullならPERはnull
   */
  const perTable = computed<PerTableRowEx[]>(() => {
    if (!result.value) return []

    const priceSeries = getPriceSeries()
    if (priceSeries.length === 0) return []

    const latest = priceSeries[priceSeries.length - 1]!
    const baseTarget = selectedDate.value ? new Date(selectedDate.value) : latest.date
    const baseHit = findClosestOnOrBefore(priceSeries, baseTarget) ?? latest
    const basePrice = baseHit.value

    const series = getEpsSeries()
      .slice()
      .sort((a, b) => sortPeriodDesc(a.period, b.period))

    const rows = series.map(x => {
      const epsVal = x.value != null ? Number(x.value) : null
      const per = epsVal != null && epsVal !== 0 ? basePrice / epsVal : null
      return { period: x.period, value: per }
    })

    return rows.map((row, i) => {
      const prev = rows[i + 1]
      const cur = row.value != null ? Number(row.value) : null
      const prevVal = prev?.value != null ? Number(prev.value) : null

      const change = cur != null && prevVal != null ? cur - prevVal : null
      const changeRate =
        change !== null && prevVal !== null && prevVal !== 0
          ? (change / Math.abs(prevVal)) * 100
          : null

      return {
        period: row.period,
        value: cur,
        change,
        changeRate,
      }
    })
  })

  /* =============================
   * グラフ（DTOから生成）
   * ============================= */

  /** 株価グラフ */
  const priceChartData = computed(() => {
    if (!result.value) return null

    const prices = result.value.priceSeries ?? []
    if (prices.length === 0) return null

    return {
      labels: prices.map(p => new Date(p.date).toLocaleDateString()),
      datasets: [
        {
          label: '株価',
          data: prices.map(p => Number(p.value)),
          borderColor: '#1E88E5',
          backgroundColor: 'rgba(30,136,229,0.08)',
          borderWidth: 2.5,
          tension: 0.25,
          fill: true,
          pointRadius: 0,
          pointHoverRadius: 5,
          pointHoverBackgroundColor: '#1E88E5',
          pointHoverBorderWidth: 2,
          spanGaps: true,
        },
      ],
    }
  })

  /** EPSグラフ（periodをそのままラベルに使用） */
  const epsChartData = computed(() => {
    if (!result.value) return null

    const list = (result.value.epsSeries ?? []).slice()
    if (list.length === 0) return null

    // 「古い→新しい」にして右肩上がり感を見やすく（必要なら変更OK）
    // periodソートの精度は "YYYYQn" 前提
    const sorted = list
      .map(x => ({ period: String(x.date), value: Number(x.value) }))
      .filter(x => Number.isFinite(x.value))
      .sort((a, b) => -sortPeriodDesc(a.period, b.period)) // ascにしたいので反転

    const labels = sorted.map(x => x.period)
    const values = sorted.map(x => x.value)

    const datasets: any[] = [
      {
        label: 'EPS',
        data: values,
        tension: 0.35,
        borderWidth: 3,
        borderColor: '#1976D2',
        backgroundColor: 'rgba(25,118,210,0.15)',
        fill: true,
        pointRadius: 2,
        pointHoverRadius: 6,
        pointHoverBorderWidth: 2,
      },
    ]

    // 平均線（選択レンジ）
    // ===== EPS 平均線（移動平均）=====
    // ===== EPS 平均線（修正版・必ず表示される）=====
    const n = Math.min(epsRange.value ?? 4, values.length)

    if (n >= 2) {
      const avgSeries = values.map((_, i) => {
        const slice = values.slice(Math.max(0, i - (n - 1)), i + 1)

        // EPS は必ず number なので null チェック不要
        return slice.reduce((a, b) => a + b, 0) / slice.length
      })

      datasets.push({
        label: `${n}期移動平均`,
        data: avgSeries,
        borderColor: '#9E9E9E',
        borderDash: [6, 6],
        borderWidth: 2,
        pointRadius: 0,
        spanGaps: false, // ← null が無いので false
      })
    }

    return { labels, datasets }
  })

  /** PERグラフ（フロントで計算） */
  /** PERグラフ（フロントで計算） */
  const perChartData = computed(() => {
    if (!result.value) return null

    const priceSeries = getPriceSeries()
    if (priceSeries.length === 0) return null

    const latest = priceSeries[priceSeries.length - 1]!
    const baseTarget = selectedDate.value ? new Date(selectedDate.value) : latest.date
    const baseHit = findClosestOnOrBefore(priceSeries, baseTarget) ?? latest
    const basePrice = baseHit.value

    const eps = getEpsSeries()
      .slice()
      .sort((a, b) => -sortPeriodDesc(a.period, b.period))
    if (eps.length === 0) return null

    const labels = eps.map(x => x.period)
    const values = eps.map(x => {
      const epsVal = x.value != null ? Number(x.value) : null
      return epsVal != null && epsVal !== 0 ? basePrice / epsVal : null
    })

    const datasets: any[] = [
      {
        label: 'PER',
        data: values,
        tension: 0.35,
        borderWidth: 3,
        borderColor: '#FB8C00',
        backgroundColor: 'rgba(251,140,0,0.15)',
        fill: true,
        pointRadius: 2,
        pointHoverRadius: 6,
        pointHoverBorderWidth: 2,
        spanGaps: true,
      },
    ]

    // ===== PER 移動平均 =====
    const numeric = values.map(v => (typeof v === 'number' && Number.isFinite(v) ? v : null))

    const n = Math.min(epsRange.value ?? 4, numeric.filter(v => v != null).length)

    if (n >= 2) {
      const avgSeries = numeric.map((_, i) => {
        const slice = numeric
          .slice(Math.max(0, i - (n - 1)), i + 1)
          .filter(v => v != null) as number[]

        if (slice.length === 0) return null
        return slice.reduce((a, b) => a + b, 0) / slice.length
      })

      datasets.push({
        label: `${n}期移動平均`,
        data: avgSeries,
        borderColor: '#9E9E9E',
        borderDash: [6, 6],
        borderWidth: 2,
        pointRadius: 0,
        spanGaps: true,
      })
    }

    return { labels, datasets }
  })

  /** 表示モードに応じて切り替え */
  /** 表示モードに応じてグラフ切替 */
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

  /** Chart.js options */
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
          label: ctx => {
            const y = (ctx.parsed as any)?.y
            return y != null ? `${ctx.dataset.label}: ${Number(y).toFixed(2)}` : ''
          },
        },
      },
    },
    scales: {
      y: {
        type: 'linear',
        beginAtZero: false,
        grace: '10%', // ← これ重要
        ticks: {
          callback: v => Number(v).toFixed(2),
        },
      },
    },
  }))

  const pricePeriodDefs = computed<PricePeriodDef[]>(() => {
    const defs: PricePeriodDef[] = [
      { key: '1m', label: 'M1', months: 1 },
      { key: '2m', label: 'M2', months: 2 },
      { key: '6m', label: 'M6', months: 6 },
      { key: '1y', label: 'Y1', years: 1 },
      { key: 'ytd', label: 'YTD', ytd: true },
    ]

    // 直近◯年平均（2〜baseYears）
    for (let y = 2; y <= (baseYears.value ?? 1); y++) {
      defs.push({ key: `${y}y`, label: `Y${y}`, years: y })
    }

    return defs
  })

  const calcAveragePrice = (
    series: { date: Date; value: number }[],
    baseDate: Date,
    def: PricePeriodDef
  ) => {
    let from: Date

    if (def.ytd) {
      from = new Date(baseDate.getFullYear(), 0, 1)
    } else if (def.months != null) {
      from = new Date(baseDate)
      from.setMonth(from.getMonth() - def.months)
    } else if (def.years != null) {
      from = new Date(baseDate)
      from.setFullYear(from.getFullYear() - def.years)
    } else {
      return null
    }

    const targets = series.filter(p => p.date >= from && p.date <= baseDate)
    if (targets.length === 0) return null

    return targets.reduce((a, b) => a + b.value, 0) / targets.length
  }

  const priceSummary = computed(() => {
    if (!result.value) return []

    const series = getPriceSeries()
    if (series.length === 0) return []

    const latest = series[series.length - 1]!
    const baseTarget = selectedDate.value ? new Date(selectedDate.value) : latest.date

    const baseHit = findClosestOnOrBefore(series, baseTarget) ?? latest
    const basePrice = baseHit.value

    return pricePeriodDefs.value.map(def => {
      const avg = calcAveragePrice(series, baseHit.date, def)
      return {
        key: def.key,
        label: def.label,
        average: avg,
        changeRate: avg != null ? calcChangeRatePct(basePrice, avg) : null,
      }
    })
  })
</script>

<template>
  <v-container fluid class="pa-0 pa-md-4">
    <v-card class="analysis-root pa-2 pa-md-6">
      <v-card-title class="text-h6">株価分析</v-card-title>

      <v-card-text class="pa-0 pa-md-4">
        <!-- =============================
             銘柄選択・実行
        ============================== -->
        <v-row>
          <v-col cols="12" md="4">
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

          <v-col cols="12" md="3">
            <v-text-field
              v-model="selectedDate"
              label="表示基準日（任意）"
              type="date"
              density="compact"
              variant="outlined"
              hide-details
              clearable
            />
          </v-col>

          <v-col cols="12" md="2">
            <v-btn color="primary" block :loading="loading" :disabled="loading" @click="execute">
              分析実行
            </v-btn>
          </v-col>

          <v-col cols="12" md="2">
            <v-btn block disabled>スプシ出力</v-btn>
          </v-col>
        </v-row>

        <!-- Import中 -->
        <v-alert v-if="importing" type="info" variant="tonal" class="mt-3">
          データを取得・同期中です（初回は少し時間がかかります）
        </v-alert>

        <!-- =============================
             条件指定
        ============================== -->
        <v-row class="mt-2">
          <v-col cols="12" md="5">
            <div class="option-block">
              <div class="option-title">📈 株価分析・基準期間</div>
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
                  size="small"
                >
                  直近 {{ year }} 年
                </v-chip>
              </v-chip-group>
            </div>
          </v-col>

          <v-col cols="12" md="7">
            <div class="option-block">
              <div class="option-title">📊 EPS / PER 分析レンジ</div>
              <v-chip-group v-model="epsRange" mandatory selected-class="bg-primary text-white">
                <v-chip size="small" :value="4">4期</v-chip>
                <v-chip size="small" :value="8">8期</v-chip>
                <v-chip size="small" :value="12">12期</v-chip>
                <v-chip size="small" :value="16">16期</v-chip>
              </v-chip-group>
            </div>
          </v-col>
        </v-row>

        <!-- =============================
             表示切替
        ============================== -->
        <v-btn-toggle v-model="displayMode" class="my-4 toggle-compact" mandatory divided>
          <v-btn value="price">株価</v-btn>
          <v-btn value="eps">EPS</v-btn>
          <v-btn value="per">PER</v-btn>
        </v-btn-toggle>

        <!-- =============================
             グラフ
        ============================== -->
        <v-card v-if="chartData" class="pa-4 mb-4" variant="outlined">
          <div class="chart-wrapper chart-height">
            <Line :data="chartData" :options="chartOptions" />
          </div>
        </v-card>

        <!-- =============================
             株価テキスト（平均＋変動率）
        ============================== -->
        <v-card v-if="displayMode === 'price' && result" class="pa-4 mb-4" variant="outlined">
          <div class="text-caption mb-2">平均株価（基準日ベース）</div>

          <v-row dense>
            <v-col v-for="item in priceSummary" :key="item.key" cols="6" md="4">
              <v-chip size="small" variant="tonal" color="info" class="ma-1">
                {{ item.label }}：
                {{ item.average != null ? item.average.toFixed(2) : '—' }}
                <span v-if="item.changeRate != null"> （{{ item.changeRate.toFixed(1) }}%） </span>
              </v-chip>
            </v-col>
          </v-row>
        </v-card>

        <!-- =============================
             EPS 表
        ============================== -->
        <v-data-table
          v-if="displayMode === 'eps' && result"
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

        <!-- =============================
             PER 表
        ============================== -->
        <v-data-table
          v-if="displayMode === 'per' && result"
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
          <template #item.value="{ value }">
            <span v-if="value !== null">{{ value.toFixed(2) }}</span>
          </template>
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

  .chart-height {
    height: 320px;
  }

  /* ===== 任意条件ブロック ===== */
  .option-block {
    padding: 8px 4px;
  }

  .option-title {
    font-size: 13px;
    font-weight: 600;
    margin-bottom: 6px;
  }

  /* ===== 画面全体のルート ===== */
  .analysis-root {
    max-width: 100%;
  }

  /* ===== スマホではカード感を消す ===== */
  @media (max-width: 600px) {
    .analysis-root {
      border-radius: 0;
      box-shadow: none;
    }
  }
</style>
