import { defineStore } from 'pinia'

/**
 * 認証状態を管理するストア
 * ・ログイン状態
 * ・管理者判定
 */
const STORAGE_KEY = 'power-law-auth'
const ADMIN_LOGIN_ID = 'nobu1014b.b'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    isLoggedIn: localStorage.getItem(STORAGE_KEY) === '1',

    // ★ ログインIDを保持（管理者判定用）
    loginId: localStorage.getItem('power-law-login-id') as string | null,
  }),

  getters: {
    /**
     * 管理者かどうかを判定する
     */
    isAdmin: state => {
      return state.loginId === ADMIN_LOGIN_ID
    },
  },

  actions: {
    /**
     * ログイン成功時に呼ばれる
     */
    loginSuccess(loginId: string) {
      this.isLoggedIn = true
      this.loginId = loginId

      localStorage.setItem(STORAGE_KEY, '1')
      localStorage.setItem('power-law-login-id', loginId)
    },

    /**
     * ログアウト処理
     */
    logout() {
      this.isLoggedIn = false
      this.loginId = null

      localStorage.removeItem(STORAGE_KEY)
      localStorage.removeItem('power-law-login-id')
    },
  },
})
