import sys
import requests
from datetime import datetime, timedelta

from powerlaw.settings import settings
from powerlaw.db import get_connection

# ===== 設定 =====
MAX_YEARS = 5  # 最大で保存する年数


def get_symbol_id(conn, symbol: str, market: str = "US") -> int:
    """
    symbols テーブルに銘柄を登録し、IDを取得
    """
    with conn.cursor() as cur:
        cur.execute(
            """
            INSERT INTO symbols (symbol, market)
            VALUES (%s, %s)
            ON CONFLICT (symbol, market) DO NOTHING
            """,
            (symbol, market),
        )
        conn.commit()

        cur.execute(
            "SELECT id FROM symbols WHERE symbol = %s AND market = %s",
            (symbol, market),
        )
        return cur.fetchone()[0]


def fetch_daily_prices(symbol: str) -> dict:
    """
    Alpha Vantage から株価を取得
    full で取得するが、保存時に期間を制限する
    """
    params = {
        "function": "TIME_SERIES_DAILY_ADJUSTED",
        "symbol": symbol,
        "apikey": settings.ALPHA_VANTAGE_API_KEY,
        "outputsize": "full",
    }

    r = requests.get(settings.ALPHA_VANTAGE_BASE_URL, params=params, timeout=30)
    r.raise_for_status()
    return r.json()


def upsert_prices(conn, symbol_id: int, data: dict):
    """
    直近 MAX_YEARS 年分だけ price_daily に保存
    """
    series = data.get("Time Series (Daily)", {})

    today = datetime.now().date()
    cutoff_date = today - timedelta(days=365 * MAX_YEARS)

    with conn.cursor() as cur:
        for date_str, values in series.items():
            trade_date = datetime.strptime(date_str, "%Y-%m-%d").date()

            # ===== 5年より古いデータは保存しない =====
            if trade_date < cutoff_date:
                continue

            close_price = float(values["5. adjusted close"])

            cur.execute(
                """
                INSERT INTO price_daily (symbol_id, trade_date, close_price)
                VALUES (%s, %s, %s)
                ON CONFLICT (symbol_id, trade_date)
                DO UPDATE SET close_price = EXCLUDED.close_price
                """,
                (symbol_id, trade_date, close_price),
            )

        conn.commit()


def main():
    if len(sys.argv) < 2:
        print("Usage: python import_price_daily.py <SYMBOL>")
        return

    symbol = sys.argv[1].upper()

    conn = get_connection()
    try:
        symbol_id = get_symbol_id(conn, symbol)
        data = fetch_daily_prices(symbol)
        upsert_prices(conn, symbol_id, data)

        print(f"[OK] price_daily imported (max {MAX_YEARS} years): {symbol}")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
