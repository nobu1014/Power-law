import sys
import requests
from datetime import datetime, timedelta

from powerlaw.settings import settings
from powerlaw.db import get_connection

# ===== 最大保存年数（分析画面の最大）=====
MAX_YEARS = 5


def get_symbol_id(conn, symbol: str, market: str = "US") -> int:
    with conn.cursor() as cur:
        cur.execute(
            """
            INSERT INTO power_test.symbols (symbol, market)
            VALUES (%s, %s)
            ON CONFLICT (symbol, market) DO NOTHING
            """,
            (symbol, market),
        )
        conn.commit()

        cur.execute(
            """
            SELECT id
            FROM power_test.symbols
            WHERE symbol = %s AND market = %s
            """,
            (symbol, market),
        )
        return cur.fetchone()[0]


def fetch_daily_prices(symbol: str) -> dict:
    params = {
        "function": "TIME_SERIES_DAILY_ADJUSTED",
        "symbol": symbol,
        "apikey": settings.ALPHA_VANTAGE_API_KEY,
        "outputsize": "full",
    }

    r = requests.get(settings.ALPHA_VANTAGE_BASE_URL, params=params, timeout=30)
    r.raise_for_status()
    data = r.json()

    if "Note" in data or "Error Message" in data:
        raise RuntimeError(str(data))

    return data


def upsert_prices(conn, symbol_id: int, data: dict):
    series = data.get("Time Series (Daily)", {})

    today = datetime.utcnow().date()
    cutoff_date = today - timedelta(days=365 * MAX_YEARS)

    sql = """
    INSERT INTO power_test.price_daily (
        symbol_id,
        trade_date,
        close_price
    )
    VALUES (%s, %s, %s)
    ON CONFLICT (symbol_id, trade_date)
    DO UPDATE SET
        close_price = EXCLUDED.close_price
    """

    with conn.cursor() as cur:
        for date_str, values in series.items():
            trade_date = datetime.strptime(date_str, "%Y-%m-%d").date()

            if trade_date < cutoff_date:
                continue

            close_price = float(values["5. adjusted close"])

            cur.execute(sql, (symbol_id, trade_date, close_price))

        conn.commit()


def main():
    if len(sys.argv) < 2:
        print("Usage: python import_price_daily.py <SYMBOL>")
        sys.exit(1)

    symbol = sys.argv[1].upper()

    conn = get_connection()
    try:
        symbol_id = get_symbol_id(conn, symbol)
        data = fetch_daily_prices(symbol)
        upsert_prices(conn, symbol_id, data)

        print(f"[OK] price_daily imported ({MAX_YEARS}Y): {symbol}")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
