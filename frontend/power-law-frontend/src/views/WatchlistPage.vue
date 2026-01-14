<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { api } from '../lib/api'

  type WatchlistItem = {
    id: number
    symbol: string
    market: string
    createdAt: string
  }

  type SymbolItem = {
    symbolCode: string
    market: string
  }

  const list = ref<WatchlistItem[]>([])
  const symbols = ref<string[]>([]) // ← 文字列配列にする
  const selectedSymbol = ref<string | null>(null)

  const loading = ref(false)
  const errorMessage = ref('')
  const snackbar = ref(false)

  /**
   * ウォッチリスト一覧取得
   */
  const load = async () => {
    const res = await api.get<WatchlistItem[]>('/watchlist')
    list.value = res.data
  }

  /**
   * symbols 一覧取得
   * → symbol だけ抜き出す
   */
  const loadSymbols = async () => {
    const res = await api.get<SymbolItem[]>('/symbols')
    symbols.value = res.data.map(s => s.symbolCode)
  }

  /**
   * 追加
   */
  const add = async () => {
    if (!selectedSymbol.value) return

    loading.value = true
    errorMessage.value = ''

    try {
      await api.post('/watchlist/add', {
        symbol: selectedSymbol.value,
        market: 'US',
      })

      selectedSymbol.value = null
      snackbar.value = true
      await load()
    } catch (e) {
      console.error(e)
      errorMessage.value = 'ウォッチリストの追加に失敗しました'
    } finally {
      loading.value = false
    }
  }

  /**
   * 削除
   */
  const remove = async (item: WatchlistItem) => {
    await api.post('/watchlist/remove', {
      symbol: item.symbol,
      market: item.market,
    })
    await load()
  }

  onMounted(async () => {
    await loadSymbols()
    await load()
  })
</script>
<template>
  <v-container>
    <v-card max-width="800" class="mx-auto mt-6 pa-6">
      <v-card-title class="text-h6">ウォッチリスト</v-card-title>

      <v-card-text>
        <!-- 銘柄選択（string[]） -->
        <v-select
          v-model="selectedSymbol"
          :items="symbols"
          label="銘柄を選択"
          clearable
          variant="outlined"
        />

        <v-btn
          color="primary"
          class="mt-2"
          :disabled="!selectedSymbol"
          :loading="loading"
          @click="add"
        >
          追加
        </v-btn>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-4">
          {{ errorMessage }}
        </v-alert>

        <v-divider class="my-6" />

        <v-table>
          <thead>
            <tr>
              <th>銘柄</th>
              <th>市場</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in list" :key="item.id">
              <td>{{ item.symbol }}</td>
              <td>{{ item.market }}</td>
              <td class="text-right">
                <v-btn icon color="error" variant="text" @click="remove(item)">
                  <v-icon>mdi-delete</v-icon>
                </v-btn>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
    </v-card>

    <v-snackbar v-model="snackbar" color="success"> ウォッチリストを更新しました </v-snackbar>
  </v-container>
</template>
