import sys
import requests
from powerlaw.settings import get_settings

def fetch_earnings(symbol: str):
    s = get_settings()
    params = {
        "function": "EARNINGS",
        "symbol": symbol,
        "apikey": s.api_key,
    }

    r = requests.get(s.base_url, params=params, timeout=20)
    r.raise_for_status()
    data = r.json()

    if "Note" in data:
        raise RuntimeError(f"API Note: {data['Note']}")
    if "Information" in data:
        raise RuntimeError(f"API Information: {data['Information']}")
    if "Error Message" in data:
        raise RuntimeError(f"API Error: {data['Error Message']}")

    return data

def main():
    s = get_settings()
    symbol = sys.argv[1].upper() if len(sys.argv) >= 2 else s.default_symbol

    data = fetch_earnings(symbol)
    earnings = data.get("quarterlyEarnings", [])

    print(f"Symbol: {symbol}")
    print("=== quarterly EPS (latest 8) ===")
    for row in earnings[:8]:
        print(
            row.get("fiscalDateEnding"),
            "EPS:",
            row.get("reportedEPS"),
        )

if __name__ == "__main__":
    main()
