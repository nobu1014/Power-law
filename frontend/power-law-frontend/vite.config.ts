// vite.config.ts
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7216', // ← APIのURL
        changeOrigin: true,
        secure: false, // https自己署名対策
      },
    },
  },
})
