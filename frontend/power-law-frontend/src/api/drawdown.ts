// src/api/drawdown.ts
import axios from 'axios'
import type { DrawdownListItem } from '../types/drawdown'

export async function fetchDrawdownList(
  periodMonths: number,
  sortOrder: 'asc' | 'desc' = 'desc'
): Promise<DrawdownListItem[]> {
  const res = await axios.get<DrawdownListItem[]>('/api/drawdown', {
    params: {
      periodMonths,
      sortOrder,
    },
  })

  return res.data
}

/**
 * 下落チェック用の最新データを取得する（重い）
 */
export async function refreshDrawdownData(): Promise<void> {
  await axios.post('/api/drawdown/refresh')
}
