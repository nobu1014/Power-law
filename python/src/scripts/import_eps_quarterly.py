# import_eps_quarterly_api.py
# → EPS（四半期）データを外部APIから取得し、JSONで返すためのスクリプト

import argparse        # コマンドライン引数（--symbol など）を扱うための標準ライブラリ
import json            # Pythonの辞書をJSON文字列に変換するためのライブラリ
import sys             # プログラムの終了コード（exit）などを制御するためのライブラリ
import requests        # HTTP通信（API呼び出し）を簡単に行うための外部ライブラリ
from datetime import datetime  # 日付文字列を日付型に変換するためのクラス
from powerlaw.settings import settings  # APIキーやベースURLなどの設定を読み込む


def fetch_eps(symbol: str) -> list[dict]:
    """
    指定された銘柄(symbol)について、Alpha Vantage APIから四半期EPSを取得し、
    APIサーバー(C#)が扱いやすい形式のリスト(dict配列)に変換して返す
    """

    # Alpha Vantage の EARNINGS API に渡すパラメータを定義する
    params = {
        "function": "EARNINGS",                     # EPS（決算情報）を取得するAPI種別
        "symbol": symbol,                           # 対象の銘柄コード（例: MSFT）
        "apikey": settings.ALPHA_VANTAGE_API_KEY,   # 環境設定から読み込んだAPIキー
    }

    # Alpha Vantage の API に HTTP GET リクエストを送信する
    r = requests.get(
        settings.ALPHA_VANTAGE_BASE_URL,  # APIのベースURL
        params=params,                    # 上で定義したクエリパラメータ
        timeout=20                        # 20秒応答がなければ失敗とする
    )

    # HTTPステータスコードが 200系 でなければ例外を発生させる
    r.raise_for_status()

    # APIレスポンス（JSON文字列）をPythonの辞書型に変換する
    data = r.json()

    # API制限・エラー・情報メッセージが含まれていた場合は異常終了とする
    if "Note" in data or "Information" in data or "Error Message" in data:
        raise RuntimeError(str(data))

    # C#に渡すためのEPSデータを格納するリストを初期化する
    result = []

    # 四半期ごとのEPSデータを1件ずつ処理する
    for row in data.get("quarterlyEarnings", []):

        # 決算日の文字列（YYYY-MM-DD）を date 型に変換する
        report_date = datetime.strptime(
            row["fiscalDateEnding"], "%Y-%m-%d"
        ).date()

        # 決算日から会計年度（年）を取得する
        fiscal_year = report_date.year

        # 決算日から四半期（1〜4）を計算する
        fiscal_quarter = (report_date.month - 1) // 3 + 1

        # C#側がそのままDB保存や判定に使える形に整形して追加する
        result.append({
            "fiscalYear": fiscal_year,                             # 会計年度（例: 2025）
            "fiscalQuarter": fiscal_quarter,                       # 四半期（1〜4）
            "fiscalPeriod": f"{fiscal_year}Q{fiscal_quarter}",     # 表示・識別用（例: 2025Q4）
            "reportDate": row["fiscalDateEnding"],                 # 決算日（文字列）
            "eps": float(row["reportedEPS"]),                      # EPS（数値）
        })

    # 整形済みのEPSリストを呼び出し元に返す
    return result


def main():
    # コマンドライン引数を定義するためのパーサーを作成する
    parser = argparse.ArgumentParser()

    # --symbol 引数（必須）を定義する
    parser.add_argument("--symbol", required=True)

    # コマンドライン引数を解析して args に格納する
    args = parser.parse_args()

    # 銘柄コードを大文字に統一する（MSFT / AAPL など）
    symbol = args.symbol.upper()

    try:
        # 指定された銘柄のEPSを取得する
        eps = fetch_eps(symbol)

        # 成功時は C# が解析しやすい JSON 形式で標準出力に出力する
        print(json.dumps({
            "ok": True,        # 正常終了を示すフラグ
            "symbol": symbol,  # 対象銘柄
            "eps": eps         # 取得したEPSデータ
        }, ensure_ascii=False))

    except Exception as e:
        # 何らかのエラーが発生した場合も JSON 形式で標準出力に返す
        print(json.dumps({
            "ok": False,       # 異常終了を示すフラグ
            "error": str(e)    # エラー内容（C#側でログ出力などに使う）
        }, ensure_ascii=False))

        # プロセスを「失敗」として終了させる（C#側で判定可能）
        sys.exit(1)


# このファイルが直接実行された場合のみ main() を呼び出す
if __name__ == "__main__":
    main()
