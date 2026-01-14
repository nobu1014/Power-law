import sys
import requests
from datetime import datetime

from powerlaw.settings import settings
from powerlaw.db import get_connection

MAX_QUARTERS = 16  # 最大16期（4年分）

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


def fetch_eps(symbol: str) -> list[dict]:
    print("API KEY =", settings.ALPHA_VANTAGE_API_KEY)

    params = {
        "function": "EARNINGS",
        "symbol": symbol,
        "apikey": settings.ALPHA_VANTAGE_API_KEY,
    }

    r = requests.get(settings.ALPHA_VANTAGE_BASE_URL, params=params, timeout=20)
    print("STATUS =", r.status_code)
    print("RAW RESPONSE =", r.text)   # ← 超重要

    r.raise_for_status()
    data = r.json()

    if "Note" in data:
        raise RuntimeError(data["Note"])
    if "Error Message" in data:
        raise RuntimeError(data["Error Message"])

    return data.get("quarterlyEarnings", [])



def upsert_eps(conn, symbol_id: int, rows: list[dict]):
    """
    EPS（四半期）を直近16期まで保存
    """
    sql = """
    INSERT INTO eps_quarterly (
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

    # ===== 直近16期だけに制限 =====
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
    symbol = sys.argv[1].upper() if len(sys.argv) >= 2 else "AAPL"

    conn = get_connection()
    try:
        symbol_id = get_symbol_id(conn, symbol)
        rows = fetch_eps(symbol)
        upsert_eps(conn, symbol_id, rows)

        print(f"[OK] EPS imported: {symbol} ({len(rows)} records)")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
