import psycopg2
from powerlaw.settings import settings


def get_connection():
    """
    PostgreSQL 接続を作成
    search_path をスキーマに固定
    """
    return psycopg2.connect(
        host=settings.DB_HOST,
        port=settings.DB_PORT,
        dbname=settings.DB_NAME,
        user=settings.DB_USER,
        password=settings.DB_PASSWORD,
        options=f"-c search_path={settings.DB_SCHEMA}",
    )
