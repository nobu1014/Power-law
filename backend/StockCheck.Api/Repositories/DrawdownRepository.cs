using Dapper;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Infrastructure;

namespace StockCheck.Api.Repositories;

public sealed class DrawdownRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DrawdownRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// ウォッチ銘柄ごとの下落率一覧を取得する
    /// （銘柄ごとに1行のみ）
    /// </summary>
    public async Task<IReadOnlyList<DrawdownListItemDto>> GetDrawdownListAsync(
        int periodMonths,
        string sortOrder)
    {
        var orderDir = sortOrder == "asc" ? "ASC" : "DESC";

        var sql = $@"
        WITH latest_price AS (
            SELECT DISTINCT ON (pd.symbol_id)
                pd.symbol_id,
                pd.close_price AS current_price
            FROM power_test.price_daily pd
            ORDER BY pd.symbol_id, pd.trade_date DESC
        ),
        peak_price AS (
            SELECT
                pd.symbol_id,
                MAX(pd.close_price) AS peak_price
            FROM power_test.price_daily pd
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
        FROM power_test.watchlist w
        JOIN power_test.symbols s
        ON s.symbol = w.symbol AND s.market = w.market
        JOIN peak_price p ON p.symbol_id = s.id
        JOIN latest_price l ON l.symbol_id = s.id
        ORDER BY DrawdownRate {orderDir};
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        var result = await conn.QueryAsync<DrawdownListItemDto>(sql);

        return result.ToList();
    }

    /// <summary>
    /// 下落チェック対象の銘柄一覧を取得する
    /// （watchlist由来）
    /// </summary>
    public async Task<IReadOnlyList<(string Symbol, string Market)>> GetTargetSymbolsAsync()
    {
        const string sql = @"
            SELECT DISTINCT
                w.symbol,
                w.market
            FROM power_test.watchlist w
            ORDER BY w.symbol;
        ";

        var list = new List<(string, string)>();

        await using var conn = await _connectionFactory.CreateAsync();
        var rows = await conn.QueryAsync(sql);

        foreach (var row in rows)
        {
            list.Add((
                (string)row.symbol,
                (string)row.market
            ));
        }

        return list;
    }
}
