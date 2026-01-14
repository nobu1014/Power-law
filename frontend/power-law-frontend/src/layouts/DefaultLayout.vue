<script setup lang="ts">
  import { ref } from 'vue'
  import { useRouter } from 'vue-router'
  import { useDisplay } from 'vuetify'
  import { useAuthStore } from '../stores/auth'

  const router = useRouter()
  const authStore = useAuthStore()
  const { smAndDown } = useDisplay()

  const drawer = ref(false)

  const menus = [
    { title: '銘柄登録', to: '/symbols', icon: 'mdi-database' },
    { title: 'ウォッチリスト', to: '/watchlist', icon: 'mdi-eye' },
    { title: '株価分析', to: '/analysis', icon: 'mdi-chart-line' },
    { title: '下落チェック', to: '/drawdown', icon: 'mdi-trending-down' },
  ]

  const onLogout = () => {
    authStore.logout()
    router.push('/login')
  }
</script>

<template>
  <v-app>
    <!-- ===== App Bar ===== -->
    <v-app-bar color="primary" dark>
      <!-- スマホ：ハンバーガー -->
      <v-app-bar-nav-icon v-if="smAndDown" @click="drawer = !drawer" />

      <v-app-bar-title class="app-title">
        <span class="d-none d-sm-inline">POWER-LAW</span>
        <span class="d-sm-none">PL</span>
      </v-app-bar-title>

      <!-- PC：横並びナビ -->
      <template v-if="!smAndDown">
        <v-btn v-for="m in menus" :key="m.to" variant="text" :to="m.to">
          {{ m.title }}
        </v-btn>
      </template>

      <v-spacer />

      <v-btn variant="text" @click="onLogout">
        <v-icon start>mdi-logout</v-icon>
        ログアウト
      </v-btn>
    </v-app-bar>

    <!-- ===== スマホ用 Drawer ===== -->
    <v-navigation-drawer v-model="drawer" temporary location="left">
      <v-list>
        <v-list-item v-for="m in menus" :key="m.to" :to="m.to" @click="drawer = false">
          <template #prepend>
            <v-icon>{{ m.icon }}</v-icon>
          </template>
          <v-list-item-title>{{ m.title }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-navigation-drawer>

    <!-- ===== Main ===== -->
    <v-main>
      <v-container fluid>
        <router-view />
      </v-container>
    </v-main>
  </v-app>
</template>
