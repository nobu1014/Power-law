<script setup lang="ts">
  import { ref } from 'vue'
  import { useRouter } from 'vue-router'
  import { api } from '../lib/api'
  import { useAuthStore } from '../stores/auth'

  // 入力値
  const loginId = ref('')
  const password = ref('')

  // 状態管理
  const loading = ref(false)
  const errorMessage = ref('')

  const router = useRouter()
  const authStore = useAuthStore()

  /**
   * ログイン処理
   */
  const onLogin = async () => {
    errorMessage.value = ''
    loading.value = true

    try {
      // AuthController の API を呼び出す
      const response = await api.post<boolean>('/auth/login', {
        loginId: loginId.value,
        password: password.value,
      })

      // true が返ってきたらログイン成功
      if (response.data === true) {
        authStore.loginSuccess()
        router.push('/analysis')
      } else {
        errorMessage.value = 'ログインに失敗しました'
      }
    } catch (error) {
      errorMessage.value = 'サーバーエラーが発生しました'
    } finally {
      loading.value = false
    }
  }
</script>

<template>
  <v-container class="fill-height">
    <v-row justify="center" align="center">
      <v-col cols="12" sm="8" md="5" lg="4">
        <v-card class="pa-6" rounded="xl">
          <v-card-title class="text-h6 text-center"> POWER-LAW ログイン </v-card-title>

          <v-card-text>
            <!-- ログインID -->
            <v-text-field
              v-model="loginId"
              label="Login ID"
              prepend-inner-icon="mdi-account"
              variant="outlined"
              density="comfortable"
            />

            <!-- パスワード -->
            <v-text-field
              v-model="password"
              label="Password"
              type="password"
              prepend-inner-icon="mdi-lock"
              variant="outlined"
              density="comfortable"
              @keyup.enter="onLogin"
            />

            <!-- エラーメッセージ -->
            <v-alert v-if="errorMessage" type="error" variant="tonal" class="mt-3">
              {{ errorMessage }}
            </v-alert>
          </v-card-text>

          <v-card-actions class="justify-center">
            <v-btn
              color="primary"
              variant="elevated"
              size="large"
              width="220"
              rounded="xl"
              :loading="loading"
              @click="onLogin"
            >
              <v-icon start>mdi-login</v-icon>
              ログイン
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
