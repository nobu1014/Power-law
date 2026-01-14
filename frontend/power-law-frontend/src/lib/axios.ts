import Axios from 'axios'

const axios = Axios.create({
  baseURL: 'http://localhost:5287', // ← backend の URL
  withCredentials: false,
  headers: {
    'Content-Type': 'application/json',
  },
})

export default axios
