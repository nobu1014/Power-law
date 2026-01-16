import axios from 'axios'

export const api = axios.create({
  baseURL: 'http://localhost:5287/api',
  withCredentials: true, // Cookie 認証を使っているため必須
})
