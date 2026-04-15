using Dapper;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Infrastructure;

namespace StockCheck.Api.Repositories;

/// <summary>
/// 下落チェック Repository
/// ログインユーザーのウォッチリスト銘柄を対象に下落率を計算する
/// </summary>
public sealed class DrawdownRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DrawdownRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// ログインユーザーのウォッチリスト銘柄ごとの下落率一覧を取得する
    /// </summary>
    public async Task<IReadOnlyList<DrawdownListItemDto>> GetDrawdownListAsync(
        int userId,
        int periodMonths,
        string sortOrder)
    {
        var schema = _connectionFactory.Schema;
        var orderDir = sortOrder == "asc" ? "ASC" : "DESC";

        // スキーマ名と user_id を動的に組み込む
        var sql = $@"
        WITH latest_price AS (
            SELECT DISTINCT ON (pd.symbol_id)
                pd.symbol_id,
                pd.close_price AS current_price
            FROM {schema}.price_daily pd
            ORDER BY pd.symbol_id, pd.trade_date DESC
        ),
        peak_price AS (
            SELECT
                pd.symbol_id,
                MAX(pd.close_price) AS peak_price
            FROM {schema}.price_daily pd
            WHERE pd.trade_date >= CURRENT_DATE - INTERVAL '{periodMonths} months'
            GROUP BY pd.symbol_id
        )
        SELECT
            s.symbol AS Symbol,
            p.peak_price AS PeakPrice,
            l.current_price AS CurrentPrice,
            ROUND(
                (l.current_price - p.peak_price) / p.peak_price * 100,
                2
            ) AS DrawdownRate
        FROM {schema}.watchlist w
        JOIN {schema}.symbols s
            ON s.symbol = w.symbol AND s.market = w.market
        JOIN peak_price p ON p.symbol_id = s.id
        JOIN latest_price l ON l.symbol_id = s.id
        WHERE w.user_id = @userId
        ORDER BY DrawdownRate {orderDir};
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        var result = await conn.QueryAsync<DrawdownListItemDto>(sql, new { userId });

        return result.ToList();
    }

    /// <summary>
    /// ログインユーザーのウォッチリスト銘柄一覧を取得する
    /// （下落チェック用データ更新の対象銘柄取得）
    /// </summary>
    public async Task<IReadOnlyList<(string Symbol, string Market)>> GetTargetSymbolsAsync(
        int userId)
    {
        var sql = $@"
            SELECT DISTINCT
                w.symbol,
                w.market
            FROM {_connectionFactory.Schema}.watchlist w
            WHERE w.user_id = @userId
            ORDER BY w.symbol;
        ";

        var list = new List<(string, string)>();

        await using var conn = await _connectionFactory.CreateAsync();
        var rows = await conn.QueryAsync(sql, new { userId });

        foreach (var row in rows)
        {
            list.Add(((string)row.symbol, (string)row.market));
        }

        return list;
    }
}