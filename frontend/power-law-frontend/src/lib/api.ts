import axios from 'axios'

/**
 * Axios 共通クライアント
 *
 * ・POWER-LAW バックエンドとの通信はすべてこれを使う
 * ・baseURL をここで一元管理することで
 *   環境が変わっても修正箇所は1か所で済む
 */
export const api = axios.create({
  // Swagger が動いている URL の「/swagger」より前
  baseURL: 'http://localhost:5287',

  // JSON API 前提
  headers: {
    'Content-Type': 'application/json',
  },
})
