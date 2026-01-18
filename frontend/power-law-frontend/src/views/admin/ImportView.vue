<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { api } from '../../lib/api'
  import { useAuthStore } from '../../stores/auth'

  /**
   * =====================================================
   * 管理者 Import 画面（Vuetify）
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
  const results = ref<any[]>([])

  /* Snackbar */
  const snackbar = ref(false)
  const snackbarText = ref('')
  const snackbarColor = ref<'success' | 'error' | 'info'>('info')

  function showMessage(text: string, color: 'success' | 'error' | 'info') {
    snackbarText.value = text
    snackbarColor.value = color
    snackbar.value = true
  }

  /* ---------------------------
   * API
   * --------------------------- */
  async function importAll() {
    loading.value = true
    results.value = []

    showMessage('Import を実行中です…', 'info')

    try {
      const res = await api.post('/admin/import/all')
      results.value = res.data
      showMessage('全銘柄 Import が完了しました', 'success')
    } catch (e: any) {
      handleError(e)
    } finally {
      loading.value = false
    }
  }

  async function importBySymbol() {
    if (!symbol.value) return

    loading.value = true
    results.value = []

    showMessage('Import を実行中です…', 'info')

    try {
      const res = await api.post(`/admin/import/symbol/${symbol.value}`)
      results.value = [res.data]
      showMessage(`${symbol.value} の Import が完了しました`, 'success')
    } catch (e: any) {
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

        <!-- 結果テーブル -->
        <v-data-table v-if="results.length" :items="results" item-key="symbol">
          <template #headers>
            <tr>
              <th>Symbol</th>
              <th>Price Inserted</th>
              <th>Price Skipped</th>
              <th>Price Filled</th>
              <th>Price Deleted</th>
              <th>EPS Inserted</th>
              <th>EPS Skipped</th>
            </tr>
          </template>

          <template #item="{ item }">
            <tr>
              <td>{{ item.symbol }}</td>
              <td>{{ item.price.inserted }}</td>
              <td>{{ item.price.skipped }}</td>
              <td>{{ item.price.filled }}</td>
              <td>{{ item.price.deleted }}</td>
              <td>{{ item.eps.inserted }}</td>
              <td>{{ item.eps.skipped }}</td>
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
