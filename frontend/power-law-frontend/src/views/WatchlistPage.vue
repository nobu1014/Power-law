<script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import { api } from '../lib/api'

  type WatchlistItem = {
    id: number
    symbol: string
    market: string
    createdAt: string
  }

  const list = ref<WatchlistItem[]>([])
  const input = ref('')
  const loading = ref(false)
  const errorMessage = ref('')
  const snackbar = ref(false)

  /**
   * 一覧取得
   */
  const load = async () => {
    const res = await api.get<WatchlistItem[]>('/watchlist')
    list.value = res.data
  }

  /**
   * 追加（カンマ区切り）
   */
  const add = async () => {
    if (!input.value) return

    loading.value = true
    errorMessage.value = ''

    const symbols = input.value
      .split(',')
      .map(s => s.trim())
      .filter(Boolean)

    try {
      for (const symbol of symbols) {
        await api.post('/watchlist/add', {
          symbol,
          market: 'US',
        })
      }

      input.value = ''
      snackbar.value = true
      await load()
    } catch {
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

  onMounted(load)
</script>

<template>
  <v-container>
    <v-card max-width="800" class="mx-auto mt-6 pa-6">
      <v-card-title class="text-h6"> ウォッチリスト </v-card-title>

      <v-card-text>
        <!-- 追加 -->
        <v-text-field
          v-model="input"
          label="銘柄追加（カンマ区切り）"
          placeholder="AAPL,TSLA,NVDA"
          variant="outlined"
        />

        <v-btn
          color="primary"
          variant="elevated"
          width="150"
          :loading="loading"
          :disabled="!input"
          @click="add"
        >
          追加
        </v-btn>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-4">
          {{ errorMessage }}
        </v-alert>

        <v-divider class="my-6" />

        <!-- 一覧 -->
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
