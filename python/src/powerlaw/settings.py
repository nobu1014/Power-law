import os
from dotenv import load_dotenv

# .env を一度だけロード
load_dotenv()


class Settings:
    """
    アプリ全体で使う設定値
    """

    # ===== Alpha Vantage =====
    ALPHA_VANTAGE_API_KEY: str = os.getenv("ALPHA_VANTAGE_API_KEY")
    ALPHA_VANTAGE_BASE_URL: str = "https://www.alphavantage.co/query"

    # ===== PostgreSQL =====
    DB_HOST: str = os.getenv("DB_HOST", "localhost")
    DB_PORT: int = int(os.getenv("DB_PORT", "5432"))
    DB_NAME: str = os.getenv("DB_NAME")
    DB_USER: str = os.getenv("DB_USER")
    DB_PASSWORD: str = os.getenv("DB_PASSWORD")
    DB_SCHEMA: str = os.getenv("DB_SCHEMA", "power_test")


# シングルトンとして利用
settings = Settings()
