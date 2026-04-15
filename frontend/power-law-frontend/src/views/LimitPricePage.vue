<script setup lang="ts">
  import { ref, onMounted, watch, computed } from 'vue'
  import { useRoute } from 'vue-router'
  import { api } from '../lib/api'

  /* ===== パターンはルートパスから判定する ===== */
  const route = useRoute()
  const pattern = computed(() => (route.path.includes('pattern2') ? 2 : 1))

  /* ===== 型定義 ===== */
  type LimitPriceSettings = {
    pattern: number
    peakDropRate: number
    avgDropRate: number
  }

  type LimitPriceItem = {
    symbol: string
    market: string
    latestPrice: number | null
    peakA: number
    peakB: number
    avgA: number
    avgB: number
    avgC: number
    peakLimitPrice: number
    avgLimitPrice: number
    finalLimitPrice: number
  }

  type LimitPriceResult = {
    isSettingsRequired: boolean
    pattern: number
    peakDropRate: number
    avgDropRate: number
    items: LimitPriceItem[]
  }

  /* ===== State ===== */
  const loading = ref(false)
  const calcLoading = ref(false)
  const result = ref<LimitPriceResult | null>(null)
  const errorMessage = ref('')
  const snackbar = ref(false)
  const snackbarText = ref('')
  const snackbarColor = ref<'success' | 'error'>('success')

  // 設定フォーム
  const settingsDialog = ref(false)
  const settingsPeakDropRate = ref<number | null>(null)
  const settingsAvgDropRate = ref<number | null>(null)
  const settingsSaving = ref(false)

  /* ===== パターン別ラベル・項目名 ===== */
  const patternLabel = computed(() => (pattern.value === 1 ? 'パターン①' : 'パターン②'))

  const peakLabels = computed(() =>
    pattern.value === 1 ? ['高値1M', '高値3M'] : ['高値3M', '高値6M']
  )

  const avgLabels = computed(() =>
    pattern.value === 1 ? ['平均1M', '平均2M', '平均6M'] : ['平均2M', '平均4M', '平均6M']
  )

  /* ===== 計算式の掛け率（下落率から算出） ===== */
  const peakMultiplier = computed(() => {
    if (settingsPeakDropRate.value != null) return 100 - settingsPeakDropRate.value
    return pattern.value === 1 ? 80 : 77
  })

  const avgMultiplier = computed(() => {
    if (settingsAvgDropRate.value != null) return 100 - settingsAvgDropRate.value
    return pattern.value === 1 ? 90 : 88
  })

  /* ===== テーブルヘッダー ===== */
  const headers = computed(() => [
    { title: '銘柄', key: 'symbol', align: 'center' as const, fixed: true, width: '80px' },
    { title: '現在値', key: 'latestPrice', align: 'center' as const },
    { title: peakLabels.value[0], key: 'peakA', align: 'center' as const },
    { title: peakLabels.value[1], key: 'peakB', align: 'center' as const },
    { title: avgLabels.value[0], key: 'avgA', align: 'center' as const },
    { title: avgLabels.value[1], key: 'avgB', align: 'center' as const },
    { title: avgLabels.value[2], key: 'avgC', align: 'center' as const },
    { title: '高値指値', key: 'peakLimitPrice', align: 'center' as const },
    { title: '平均指値', key: 'avgLimitPrice', align: 'center' as const },
    { title: '最終指値', key: 'finalLimitPrice', align: 'center' as const },
  ])

  /* ===== 設定取得 ===== */
  const loadSettings = async () => {
    try {
      const res = await api.get<LimitPriceSettings>(`/limit-price/settings/${pattern.value}`)
      settingsPeakDropRate.value = res.data.peakDropRate
      settingsAvgDropRate.value = res.data.avgDropRate
    } catch {
      settingsPeakDropRate.value = null
      settingsAvgDropRate.value = null
    }
  }

  /* ===== 設定保存 ===== */
  const saveSettings = async () => {
    if (!settingsPeakDropRate.value || !settingsAvgDropRate.value) return

    settingsSaving.value = true
    try {
      await api.post('/limit-price/settings', {
        pattern: pattern.value,
        peakDropRate: settingsPeakDropRate.value,
        avgDropRate: settingsAvgDropRate.value,
      })
      settingsDialog.value = false
      snackbarText.value = '設定を保存しました'
      snackbarColor.value = 'success'
      snackbar.value = true
    } catch {
      snackbarText.value = '設定の保存に失敗しました'
      snackbarColor.value = 'error'
      snackbar.value = true
    } finally {
      settingsSaving.value = false
    }
  }

  /* ===== 指値計算実行 ===== */
  const calc = async () => {
    errorMessage.value = ''
    calcLoading.value = true
    result.value = null

    try {
      const res = await api.get<LimitPriceResult>(`/limit-price/calc/${pattern.value}`)
      result.value = res.data

      if (res.data.isSettingsRequired) {
        errorMessage.value = '下落率が未設定です。設定ボタンから登録してください。'
      }
    } catch {
      errorMessage.value = '指値計算に失敗しました'
    } finally {
      calcLoading.value = false
    }
  }

  /* ===== 数値フォーマット ===== */
  const fmt = (v: number | null) => (v != null ? v.toFixed(2) : '—')

  /* ===== 最終指値の色（現在値以下なら緑） ===== */
  const finalPriceColor = (item: LimitPriceItem) => {
    if (!item.latestPrice) return ''
    return item.latestPrice <= item.finalLimitPrice ? 'text-success' : ''
  }

  /* ===== 初期ロード ===== */
  onMounted(async () => {
    loading.value = true
    await loadSettings()
    loading.value = false
  })

  /* ===== パターン変更時に再ロード ===== */
  watch(pattern, async () => {
    result.value = null
    errorMessage.value = ''
    loading.value = true
    await loadSettings()
    loading.value = false
  })
</script>

<template>
  <v-container fluid class="pa-0 pa-md-4">
    <v-card class="pa-2 pa-md-6">
      <!-- タイトル・操作ボタン -->
      <v-card-title class="d-flex align-center flex-wrap gap-2">
        <span class="text-h6">指値計算 {{ patternLabel }}</span>
        <v-spacer />
        <v-btn
          variant="outlined"
          size="small"
          prepend-icon="mdi-cog"
          @click="settingsDialog = true"
        >
          下落率設定
        </v-btn>
        <v-btn
          color="primary"
          size="small"
          prepend-icon="mdi-calculator"
          :loading="calcLoading"
          @click="calc"
        >
          指値計算実行
        </v-btn>
      </v-card-title>

      <v-card-text class="pa-0 pa-md-4">
        <!-- ===== 計算式の説明 ===== -->
        <v-card variant="tonal" color="blue-grey" class="mb-4 pa-3">
          <div class="text-caption font-weight-bold mb-2">📐 計算方法（{{ patternLabel }}）</div>
          <!-- パターン① -->
          <template v-if="pattern === 1">
            <div class="text-caption">
              <div>
                📌 <strong>最高値軸指値</strong> ＝（1ヶ月最高値 ＋ 3ヶ月最高値）÷ 2 ×
                {{ peakMultiplier }}%
              </div>
              <div>
                📌 <strong>平均軸指値</strong> ＝（1ヶ月平均 ＋ 2ヶ月平均 ＋ 6ヶ月平均）÷ 3 ×
                {{ avgMultiplier }}%
              </div>
              <div>
                🎯 <strong>最終指値</strong>
                ＝（最高値軸指値 ＋ 平均軸指値）÷ 2
              </div>
            </div>
          </template>
          <!-- パターン② -->
          <template v-else>
            <div class="text-caption">
              <div>
                📌 <strong>最高値軸指値</strong> ＝（3ヶ月最高値 ＋ 6ヶ月最高値）÷ 2 ×
                {{ peakMultiplier }}%
              </div>
              <div>
                📌 <strong>平均軸指値</strong> ＝（2ヶ月平均 ＋ 4ヶ月平均 ＋ 6ヶ月平均）÷ 3 ×
                {{ avgMultiplier }}%
              </div>
              <div>
                🎯 <strong>最終指値</strong>
                ＝（最高値軸指値 ＋ 平均軸指値）÷ 2
              </div>
            </div>
          </template>
        </v-card>

        <!-- 現在の設定表示 -->
        <v-alert
          v-if="settingsPeakDropRate != null"
          type="info"
          variant="tonal"
          class="mb-4"
          density="compact"
        >
          現在の設定：最高値軸 {{ settingsPeakDropRate }}% 下落 ／ 平均軸 {{ settingsAvgDropRate }}%
          下落
        </v-alert>

        <!-- 設定未登録アラート -->
        <v-alert v-else type="warning" variant="tonal" class="mb-4" density="compact">
          下落率が未設定です。「下落率設定」ボタンから登録してください。
        </v-alert>

        <!-- エラーメッセージ -->
        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mb-4">
          {{ errorMessage }}
        </v-alert>

        <!-- 計算結果テーブル -->
        <v-data-table
          v-if="result && !result.isSettingsRequired && result.items.length > 0"
          :headers="headers"
          :items="result.items"
          :items-per-page="-1"
          hide-default-footer
          density="compact"
          fixed-header
          height="500"
          class="mt-2"
          :mobile-breakpoint="0"
        >
          <template #item.symbol="{ value }">
            <span class="font-weight-medium">{{ value }}</span>
          </template>

          <template #item.latestPrice="{ item }">
            {{ fmt(item.latestPrice) }}
          </template>

          <template #item.peakA="{ value }">{{ fmt(value) }}</template>
          <template #item.peakB="{ value }">{{ fmt(value) }}</template>
          <template #item.avgA="{ value }">{{ fmt(value) }}</template>
          <template #item.avgB="{ value }">{{ fmt(value) }}</template>
          <template #item.avgC="{ value }">{{ fmt(value) }}</template>

          <template #item.peakLimitPrice="{ value }">
            <span class="text-orange-darken-2 font-weight-medium">
              {{ fmt(value) }}
            </span>
          </template>

          <template #item.avgLimitPrice="{ value }">
            <span class="text-blue-darken-2 font-weight-medium">
              {{ fmt(value) }}
            </span>
          </template>

          <template #item.finalLimitPrice="{ item }">
            <span class="font-weight-bold" :class="finalPriceColor(item)">
              {{ fmt(item.finalLimitPrice) }}
            </span>
          </template>
        </v-data-table>

        <!-- データなし -->
        <v-alert
          v-else-if="result && result.items.length === 0"
          type="info"
          variant="tonal"
          class="mt-4"
        >
          ウォッチリストに銘柄が登録されていません。
        </v-alert>
      </v-card-text>
    </v-card>

    <!-- ===== 設定ダイアログ ===== -->
    <v-dialog v-model="settingsDialog" max-width="400">
      <v-card>
        <v-card-title>下落率設定 {{ patternLabel }}</v-card-title>
        <v-card-text>
          <v-text-field
            v-model.number="settingsPeakDropRate"
            label="最高値軸の下落率（%）"
            type="number"
            step="0.1"
            min="0.1"
            max="99.9"
            variant="outlined"
            density="compact"
            :hint="pattern === 1 ? 'デフォルト：20%' : 'デフォルト：23%'"
            persistent-hint
            class="mb-4"
          />
          <v-text-field
            v-model.number="settingsAvgDropRate"
            label="平均軸の下落率（%）"
            type="number"
            step="0.1"
            min="0.1"
            max="99.9"
            variant="outlined"
            density="compact"
            :hint="pattern === 1 ? 'デフォルト：10%' : 'デフォルト：12%'"
            persistent-hint
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="settingsDialog = false">キャンセル</v-btn>
          <v-btn
            color="primary"
            :loading="settingsSaving"
            :disabled="!settingsPeakDropRate || !settingsAvgDropRate"
            @click="saveSettings"
          >
            保存
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- スナックバー -->
    <v-snackbar v-model="snackbar" :color="snackbarColor">
      {{ snackbarText }}
    </v-snackbar>
  </v-container>
</template>
