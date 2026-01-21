<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { api } from '../../lib/api'
  import { useAuthStore } from '../../stores/auth'

  /**
   * =====================================================
   * 管理者 Import 画面（Vuetify）
   *
   * ・全銘柄 Import
   * ・指定銘柄 Import
   * ・Import 結果表示
   * ・SSE による進捗表示（最後の状態を保持）
   * =====================================================
   */

  const router = useRouter()
  const authStore = useAuthStore()

  /* ---------------------------
   * 認証チェック
   * --------------------------- */
  onMounted(() => {
    if (!authStore.isLoggedIn) {
      router.push('/login')
    }
  })

  /* ---------------------------
   * State
   * --------------------------- */
  const loading = ref(false)
  const symbol = ref('')

  /**
   * ImportSummary の配列
   * ・成功行：error === null
   * ・失敗行：error にメッセージが入る
   */
  const results = ref<any[]>([])

  /**
   * SSE 進捗表示用（★ここが重要）
   * ・最後に受信した状態を常に保持する
   */
  const currentSymbol = ref<string | null>(null)
  const currentStatus = ref<'start' | 'success' | 'failed' | null>(null)
  const currentError = ref<string | null>(null)

  /**
   * EventSource は 1 つだけ保持
   */
  let eventSource: EventSource | null = null

  /* ---------------------------
   * Snackbar
   * --------------------------- */
  const snackbar = ref(false)
  const snackbarText = ref('')
  const snackbarColor = ref<'success' | 'error' | 'info'>('info')

  function showMessage(text: string, color: 'success' | 'error' | 'info') {
    snackbarText.value = text
    snackbarColor.value = color
    snackbar.value = true
  }

  /* =====================================================
   * SSE（進捗監視）
   * ===================================================== */

  /**
   * 全件 Import 用の進捗リスナー開始
   */
  function startProgressListener() {
    // 多重接続防止
    stopProgressListener()

    eventSource = new EventSource('/api/admin/import/progress')

    eventSource.onmessage = e => {
      const data = JSON.parse(e.data)

      // ★ 常に最後の状態を保持する
      currentSymbol.value = data.Symbol
      currentStatus.value = data.Status
      currentError.value = data.Error ?? null
    }

    eventSource.onerror = () => {
      stopProgressListener()
    }
  }

  /**
   * SSE 停止
   */
  function stopProgressListener() {
    if (eventSource) {
      eventSource.close()
      eventSource = null
    }
  }

  /* =====================================================
   * API 呼び出し
   * ===================================================== */

  /**
   * 全銘柄 Import
   */
  async function importAll() {
    loading.value = true
    results.value = []

    // 進捗初期化
    currentSymbol.value = null
    currentStatus.value = null
    currentError.value = null

    startProgressListener()

    // SSE 接続確立待ち
    await new Promise(resolve => setTimeout(resolve, 300))

    showMessage('全銘柄 Import を実行中です…', 'info')

    try {
      const res = await api.post('/admin/import/all')
      results.value = res.data
      showMessage('全銘柄 Import が完了しました', 'success')
    } catch (e: any) {
      handleError(e)
    } finally {
      loading.value = false
      stopProgressListener()
    }
  }

  /**
   * 指定銘柄 Import
   */
  async function importBySymbol() {
    if (!symbol.value) return

    loading.value = true
    results.value = []

    // SSE は全件用なので止める
    stopProgressListener()

    // 状態表示用
    currentSymbol.value = symbol.value.toUpperCase()
    currentStatus.value = 'start'
    currentError.value = null

    showMessage(`${symbol.value} の Import を実行中です…`, 'info')

    try {
      const res = await api.post(`/admin/import/symbol/${symbol.value}`)
      results.value = [res.data]

      if (res.data.error) {
        currentStatus.value = 'failed'
        currentError.value = res.data.error
      } else {
        currentStatus.value = 'success'
      }

      showMessage(`${symbol.value} の Import が完了しました`, 'success')
    } catch (e: any) {
      currentStatus.value = 'failed'
      currentError.value = 'API 実行エラー'
      handleError(e)
    } finally {
      loading.value = false
    }
  }

  /* ---------------------------
   * エラーハンドリング
   * --------------------------- */
  function handleError(e: any) {
    if (e?.response?.status === 403) {
      showMessage('管理者権限がありません', 'error')
    } else {
      showMessage('Import に失敗しました', 'error')
    }
  }
</script>

<template>
  <v-container>
    <v-card>
      <v-card-title>Import 管理画面</v-card-title>

      <v-card-text>
        <!-- 操作エリア -->
        <v-row class="mb-4">
          <v-col cols="12">
            <v-btn color="primary" :loading="loading" @click="importAll">
              全銘柄 Import 実行
            </v-btn>
          </v-col>

          <v-col cols="8">
            <v-text-field v-model="symbol" label="Symbol（例: AAPL）" :disabled="loading" />
          </v-col>
          <v-col cols="4">
            <v-btn color="secondary" :loading="loading" @click="importBySymbol">
              銘柄 Import
            </v-btn>
          </v-col>
        </v-row>

        <!-- ===== 進捗表示（常に残る） ===== -->
        <v-alert v-if="currentStatus === 'start'" type="info" class="mb-4">
          現在処理中の銘柄：{{ currentSymbol }}
        </v-alert>

        <v-alert v-else-if="currentStatus === 'success'" type="success" class="mb-4">
          処理完了：{{ currentSymbol }}
        </v-alert>

        <v-alert v-else-if="currentStatus === 'failed'" type="error" class="mb-4">
          処理失敗：{{ currentSymbol }}<br />
          {{ currentError }}
        </v-alert>

        <!-- 結果テーブル -->
        <v-data-table v-if="results.length" :items="results" item-key="symbol">
          <template #headers>
            <tr>
              <th>Symbol</th>
              <th>Status</th>
              <th>Price Inserted</th>
              <th>Price Skipped</th>
              <th>Price Filled</th>
              <th>Price Deleted</th>
              <th>EPS Inserted</th>
              <th>EPS Skipped</th>
              <th>Error</th>
            </tr>
          </template>

          <template #item="{ item }">
            <tr :class="{ 'error-row': item.error }">
              <td>{{ item.symbol }}</td>

              <td>
                <span v-if="!item.error" class="text-success">SUCCESS</span>
                <span v-else class="text-error">FAILED</span>
              </td>

              <td>{{ item.error ? '-' : item.price.inserted }}</td>
              <td>{{ item.error ? '-' : item.price.skipped }}</td>
              <td>{{ item.error ? '-' : item.price.filled }}</td>
              <td>{{ item.error ? '-' : item.price.deleted }}</td>
              <td>{{ item.error ? '-' : item.eps.inserted }}</td>
              <td>{{ item.error ? '-' : item.eps.skipped }}</td>

              <td class="error-text">
                {{ item.error ?? '' }}
              </td>
            </tr>
          </template>
        </v-data-table>
      </v-card-text>
    </v-card>

    <!-- Snackbar -->
    <v-snackbar v-model="snackbar" :color="snackbarColor">
      {{ snackbarText }}
    </v-snackbar>
  </v-container>
</template>

<style scoped>
  .error-row {
    background-color: #ffecec;
  }

  .error-text {
    color: #c62828;
    font-size: 0.85rem;
    white-space: pre-wrap;
  }
</style>
