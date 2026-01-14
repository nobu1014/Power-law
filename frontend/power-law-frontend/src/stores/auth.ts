import { defineStore } from "pinia"

/**
 * 認証状態を管理するストア
 * POWER-LAW では「ログイン済みかどうか」だけを管理
 */
const STORAGE_KEY = "power-law-auth"

export const useAuthStore = defineStore("auth", {
  state: () => ({
    isLoggedIn: localStorage.getItem(STORAGE_KEY) === "1",
  }),

  actions: {
    loginSuccess() {
      this.isLoggedIn = true
      localStorage.setItem(STORAGE_KEY, "1")
    },
    logout() {
      this.isLoggedIn = false
      localStorage.removeItem(STORAGE_KEY)
    },
  },
})
