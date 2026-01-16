# import_price_daily_api.py
# → 株価（日次）データを外部APIから取得し、JSONで返すためのスクリプト
# → full / compact の判断は C# 側が行い、Pythonはその指示に従うだけ

import argparse        # コマンドライン引数（--symbol, --from, --to）を扱うための標準ライブラリ
import json            # Pythonの辞書をJSON文字列に変換するためのライブラリ
import sys             # プログラムの終了コード（exit）などを制御するためのライブラリ
import requests        # HTTP通信（API呼び出し）を簡単に行うための外部ライブラリ
from datetime import datetime
from powerlaw.settings import settings  # APIキーやベースURLなどの設定を読み込む


def fetch_daily_prices(symbol: str, from_date: str, to_date: str, outputsize: str) -> list[dict]:
    """
    指定された銘柄(symbol)・期間(from_date〜to_date)について、
    Alpha Vantage APIから日次株価を取得し、
    C#が扱いやすい形式のリスト(dict配列)に変換して返す

    注意:
    - from/to は Alpha Vantage API のパラメータではない
    - APIが返した結果を Python 側でフィルタしているだけ
    """

    # Alpha Vantage の日次株価（調整後）APIに渡すパラメータを定義する
    params = {
        "function": "TIME_SERIES_DAILY_ADJUSTED",  # 調整後株価（日次）を取得するAPI
        "symbol": symbol,                          # 対象の銘柄コード（例: MSFT）
        "apikey": settings.ALPHA_VANTAGE_API_KEY,  # 環境設定から読み込んだAPIキー
        "outputsize": outputsize,                  # C#の判断をそのまま反映（compact / full）
    }

    # Alpha Vantage API に HTTP GET リクエストを送信する
    r = requests.get(
        settings.ALPHA_VANTAGE_BASE_URL,
        params=params,
        timeout=30
    )

    # HTTPステータスコードが 200系 でなければ例外を発生させる
    r.raise_for_status()

    # APIレスポンス（JSON文字列）をPythonの辞書型に変換する
    data = r.json()

    # API制限・エラーが含まれていた場合は異常終了とする
    if "Note" in data or "Information" in data or "Error Message" in data:
        raise RuntimeError(str(data))

    # 株価データ本体（日付ごとのデータ）を取得する
    series = data.get("Time Series (Daily)")

    # データが取得できなかった場合は異常として扱う
    if not series:
        raise RuntimeError("Price data is empty")

    # 期間指定の文字列を date 型に変換する
    from_d = datetime.strptime(from_date, "%Y-%m-%d").date()
    to_d = datetime.strptime(to_date, "%Y-%m-%d").date()

    result = []

    # APIが返した日付分の株価を1日ずつ処理する
    for date_str, values in series.items():
        trade_date = datetime.strptime(date_str, "%Y-%m-%d").date()

        # 指定期間外はスキップ
        if trade_date < from_d or trade_date > to_d:
            continue

        close_price = float(values["5. adjusted close"])

        result.append({
            "date": date_str,
            "close": close_price
        })

    # 日付昇順に並び替える（DB保存・分析しやすくするため）
    result.sort(key=lambda x: x["date"])

    return result


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("--symbol", required=True)
    parser.add_argument("--from", dest="from_date", required=True)
    parser.add_argument("--to", dest="to_date", required=True)

    # full / compact は C# 側が判断し、Pythonはそのまま従う
    parser.add_argument(
        "--outputsize",
        choices=["compact", "full"],
        default="compact",
        help="Alpha Vantage outputsize (compact=~100days, full=all)"
    )

    args = parser.parse_args()
    symbol = args.symbol.upper()

    try:
        prices = fetch_daily_prices(
            symbol,
            args.from_date,
            args.to_date,
            args.outputsize
        )

        print(json.dumps({
            "ok": True,
            "symbol": symbol,
            "from": args.from_date,
            "to": args.to_date,
            "outputsize": args.outputsize,
            "prices": prices
        }, ensure_ascii=False))

    except Exception as e:
        print(json.dumps({
            "ok": False,
            "error": str(e)
        }, ensure_ascii=False))
        sys.exit(1)


if __name__ == "__main__":
    main()
