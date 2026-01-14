import { createRouter, createWebHistory } from 'vue-router'
import LoginPage from '../views/LoginPage.vue'
import DefaultLayout from '../layouts/DefaultLayout.vue'

import SymbolRegisterPage from '../views/SymbolRegisterPage.vue'
import WatchlistPage from '../views/WatchlistPage.vue'
import AnalysisPage from '../views/AnalysisPage.vue'
import DrawdownListView from '../views/DrawdownListView.vue'

import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/analysis' },

    { path: '/login', component: LoginPage },

    {
      path: '/',
      component: DefaultLayout,
      meta: { requiresAuth: true },
      children: [
        { path: 'symbols', component: SymbolRegisterPage },
        { path: 'watchlist', component: WatchlistPage },

        {
          path: 'analysis',
          name: 'analysis',
          component: AnalysisPage,
        },
        {
          path: 'drawdown',
          name: 'drawdown',
          component: DrawdownListView,
        },
      ],
    },
  ],
})

router.beforeEach(to => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isLoggedIn) {
    return '/login'
  }
})

export default router
