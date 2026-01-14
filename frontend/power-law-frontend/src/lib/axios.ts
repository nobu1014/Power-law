import Axios from 'axios'

const axios = Axios.create({
  baseURL: 'https://power-law.site/api', // ← backend の URL
  withCredentials: false,
  headers: {
    'Content-Type': 'application/json',
  },
})

export default axios
