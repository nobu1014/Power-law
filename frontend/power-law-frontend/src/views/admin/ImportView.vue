<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { useRouter } from 'vue-router'
  import { api } from '../../lib/api'
  import { useAuthStore } from '../../stores/auth'

  /**
   * =====================================================
   * 管理者Import画面
   * ・Import 実行
   * ・結果サマリ表示
   * =====================================================
   */

  const router = useRouter()
  const authStore = useAuthStore()

  /**
   * =====================================================
   * 初期チェック
   * ・未ログインならログイン画面へ
   * =====================================================
   */
  onMounted(() => {
    if (!authStore.isLoggedIn) {
      router.push('/login')
    }
  })

  /**
   * =====================================================
   * 画面状態
   * =====================================================
   */
  const loading = ref(false)
  const symbol = ref('')
  const results = ref<any[]>([])
  const error = ref<string | null>(null)

  /**
   * =====================================================
   * API呼び出し
   * =====================================================
   */

  /**
   * 全銘柄 Import を実行する
   */
  async function importAll() {
    loading.value = true
    error.value = null

    try {
      const res = await api.post('/admin/import/all')
      results.value = res.data
    } catch (e: any) {
      error.value = e?.message ?? 'Import failed'
    } finally {
      loading.value = false
    }
  }

  /**
   * 指定銘柄 Import を実行する
   */
  async function importBySymbol() {
    if (!symbol.value) return

    loading.value = true
    error.value = null

    try {
      const res = await api.post(`/admin/import/symbol/${symbol.value}`)
      results.value = [res.data] // 単一結果なので配列に包む
    } catch (e: any) {
      error.value = e?.message ?? 'Import failed'
    } finally {
      loading.value = false
    }
  }
</script>

<template>
  <div class="import-view">
    <h1>Import 管理画面</h1>

    <!-- 操作エリア -->
    <section class="actions">
      <button @click="importAll" :disabled="loading">全銘柄 Import 実行</button>

      <div class="symbol-import">
        <input v-model="symbol" placeholder="Symbol (例: AAPL)" :disabled="loading" />
        <button @click="importBySymbol" :disabled="loading">銘柄 Import</button>
      </div>
    </section>

    <!-- エラー表示 -->
    <p v-if="error" class="error">
      {{ error }}
    </p>

    <!-- 結果表示 -->
    <table v-if="results.length" class="result-table">
      <thead>
        <tr>
          <th>Symbol</th>
          <th>Price Inserted</th>
          <th>Price Skipped</th>
          <th>Price Filled</th>
          <th>Price Deleted</th>
          <th>EPS Inserted</th>
          <th>EPS Skipped</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in results" :key="r.symbol">
          <td>{{ r.symbol }}</td>
          <td>{{ r.price.inserted }}</td>
          <td>{{ r.price.skipped }}</td>
          <td>{{ r.price.filled }}</td>
          <td>{{ r.price.deleted }}</td>
          <td>{{ r.eps.inserted }}</td>
          <td>{{ r.eps.skipped }}</td>
        </tr>
      </tbody>
    </table>

    <p v-if="loading">Import 実行中...</p>
  </div>
</template>

<style scoped>
  .import-view {
    padding: 24px;
  }

  .actions {
    margin-bottom: 16px;
  }

  .symbol-import {
    margin-top: 8px;
  }

  .result-table {
    margin-top: 24px;
    border-collapse: collapse;
  }

  .result-table th,
  .result-table td {
    border: 1px solid #ccc;
    padding: 6px 10px;
    text-align: right;
  }

  .result-table th:first-child,
  .result-table td:first-child {
    text-align: left;
  }

  .error {
    color: red;
  }
</style>
