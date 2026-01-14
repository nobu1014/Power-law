import sys
import requests
from datetime import datetime

from powerlaw.settings import settings
from powerlaw.db import get_connection

# ===== 最大保存期数（分析画面最大）=====
MAX_QUARTERS = 16


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


def fetch_eps(symbol: str) -> list[dict]:
    params = {
        "function": "EARNINGS",
        "symbol": symbol,
        "apikey": settings.ALPHA_VANTAGE_API_KEY,
    }

    r = requests.get(settings.ALPHA_VANTAGE_BASE_URL, params=params, timeout=20)
    r.raise_for_status()
    data = r.json()

    if "Note" in data or "Information" in data or "Error Message" in data:
        raise RuntimeError(str(data))

    return data.get("quarterlyEarnings", [])


def upsert_eps(conn, symbol_id: int, rows: list[dict]):
    sql = """
    INSERT INTO power_test.eps_quarterly (
        symbol_id,
        fiscal_year,
        fiscal_quarter,
        eps,
        report_date
    )
    VALUES (%s, %s, %s, %s, %s)
    ON CONFLICT (symbol_id, fiscal_year, fiscal_quarter)
    DO UPDATE SET
        eps = EXCLUDED.eps,
        report_date = EXCLUDED.report_date
    """

    limited_rows = rows[:MAX_QUARTERS]

    with conn.cursor() as cur:
        for r in limited_rows:
            report_date = datetime.strptime(
                r["fiscalDateEnding"], "%Y-%m-%d"
            ).date()

            fiscal_year = report_date.year
            fiscal_quarter = (report_date.month - 1) // 3 + 1
            eps = float(r["reportedEPS"])

            cur.execute(
                sql,
                (symbol_id, fiscal_year, fiscal_quarter, eps, report_date),
            )

        conn.commit()


def main():
    if len(sys.argv) < 2:
        print("Usage: python import_eps_quarterly.py <SYMBOL>")
        sys.exit(1)

    symbol = sys.argv[1].upper()

    conn = get_connection()
    try:
        symbol_id = get_symbol_id(conn, symbol)
        rows = fetch_eps(symbol)
        upsert_eps(conn, symbol_id, rows)

        print(f"[OK] eps_quarterly imported ({len(rows)} rows): {symbol}")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
