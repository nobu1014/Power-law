<script setup lang="ts">
  import { ref } from 'vue'
  import { api } from '../lib/api'

  const market = ref('US')
  const bulkInput = ref('')

  const loading = ref(false)
  const snackbar = ref(false)
  const errorMessage = ref('')

  // ★ 結果表示用
  const registeredCount = ref(0)
  const skippedCount = ref(0)
  const registeredSymbols = ref<string[]>([])

  /**
   * 登録
   */
  const registerBulk = async () => {
    if (!bulkInput.value) return

    loading.value = true
    errorMessage.value = ''

    try {
      const res = await api.post('/symbols/register', {
        symbols: bulkInput.value,
        market: market.value,
      })

      registeredCount.value = res.data.registered
      skippedCount.value = res.data.skipped ?? 0
      registeredSymbols.value = res.data.symbols ?? []

      snackbar.value = true
      bulkInput.value = ''
    } catch {
      errorMessage.value = '銘柄の登録に失敗しました'
    } finally {
      loading.value = false
    }
  }
</script>

<template>
  <v-container>
    <v-card max-width="700" class="mx-auto mt-6 pa-6">
      <v-card-title class="text-h6">銘柄登録</v-card-title>

      <v-card-text>
        <v-textarea
          v-model="bulkInput"
          label="銘柄入力（カンマ区切り）"
          placeholder="AAPL,MSFT,TSLA,NVDA"
          rows="5"
          variant="outlined"
        />

        <v-btn
          color="primary"
          variant="elevated"
          width="150"
          :disabled="!bulkInput"
          :loading="loading"
          @click="registerBulk"
        >
          登録
        </v-btn>

        <v-alert v-if="loading" type="info" variant="tonal" class="mt-4">
          株価・EPSデータを取得しています。<br />
          初回登録や銘柄数が多い場合、数十秒かかることがあります。
        </v-alert>

        <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-4">
          {{ errorMessage }}
        </v-alert>

        <!-- ★ 結果表示 -->
        <v-alert v-if="registeredCount || skippedCount" type="success" variant="tonal" class="mt-4">
          登録：{{ registeredCount }} 件　 スキップ：{{ skippedCount }} 件
          <div v-if="registeredSymbols.length" class="mt-2">
            <strong>登録銘柄：</strong>
            {{ registeredSymbols.join(', ') }}
          </div>
        </v-alert>
      </v-card-text>
    </v-card>

    <v-snackbar v-model="snackbar" color="success"> 銘柄登録が完了しました </v-snackbar>
  </v-container>
</template>
