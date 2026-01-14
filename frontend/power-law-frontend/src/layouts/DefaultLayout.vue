<script setup lang="ts">
  import { useRouter } from 'vue-router'
  import { useAuthStore } from '../stores/auth'

  const router = useRouter()
  const authStore = useAuthStore()

  const onLogout = () => {
    authStore.logout()
    router.push('/login')
  }
</script>

<template>
  <v-app>
    <!-- ヘッダー -->
    <v-app-bar color="primary" dark>
      <v-app-bar-title class="mr-6"> POWER-LAW </v-app-bar-title>

      <!-- ★ ナビゲーション -->
      <v-btn variant="text" to="/symbols"> 銘柄登録 </v-btn>

      <v-btn variant="text" to="/watchlist"> ウォッチリスト </v-btn>

      <v-btn variant="text" to="/analysis"> 株価分析 </v-btn>

      <v-btn variant="text" to="/drawdown"> 下落チェック </v-btn>

      <v-spacer />

      <v-btn variant="text" @click="onLogout">
        <v-icon start>mdi-logout</v-icon>
        ログアウト
      </v-btn>
    </v-app-bar>

    <!-- メイン -->
    <v-main>
      <v-container fluid>
        <router-view />
      </v-container>
    </v-main>
  </v-app>
</template>
