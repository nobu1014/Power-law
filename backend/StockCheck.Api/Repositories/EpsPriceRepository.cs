using Dapper;
using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// EPS × 株価（四半期）取得用 Repository
/// </summary>
public class EpsPriceRepository
{
    private readonly DbConnectionFactory _db;

    public EpsPriceRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// 指定銘柄のEPS×株価を直近順で取得する
    /// </summary>
    public async Task<List<EpsPriceRow>> GetRecentAsync(int symbolId, int range)
    {
        range = Math.Clamp(range, 1, 32);

        var sql = $"""
            SELECT
                fiscal_year               AS FiscalYear,
                fiscal_quarter            AS FiscalQuarter,
                eps                       AS Eps,
                close_price_before_report AS ClosePrice,
                report_date               AS TradeDate,
                per                       AS Per
            FROM {_db.Schema}.eps_price_recent_view
            WHERE symbol_id = @SymbolId
            ORDER BY fiscal_year DESC, fiscal_quarter DESC
            LIMIT @Limit
        """;

        await using var conn = await _db.CreateAsync();

        var result = await conn.QueryAsync<EpsPriceRow>(sql, new
        {
            SymbolId = symbolId,
            Limit = range
        });

        return result.ToList();
    }

    /// <summary>
    /// 指定銘柄のEPS件数を取得する（Import判定用）
    /// </summary>
    public async Task<int> CountAsync(int symbolId)
    {
        var sql = $"""
            SELECT COUNT(*)
            FROM {_db.Schema}.eps_quarterly
            WHERE symbol_id = @SymbolId
        """;

        await using var conn = await _db.CreateAsync();

        return await conn.ExecuteScalarAsync<int>(sql, new { SymbolId = symbolId });
    }

    /// <summary>
    /// 指定銘柄の最新EPS期を取得する
    /// </summary>
    public async Task<(int FiscalYear, int FiscalQuarter)?> GetLatestPeriodAsync(int symbolId)
    {
        var sql = $"""
            SELECT fiscal_year, fiscal_quarter
            FROM {_db.Schema}.eps_quarterly
            WHERE symbol_id = @SymbolId
            ORDER BY fiscal_year DESC, fiscal_quarter DESC
            LIMIT 1
        """;

        await using var conn = await _db.CreateAsync();

        var result = await conn.QueryFirstOrDefaultAsync(sql, new { SymbolId = symbolId });

        if (result == null) return null;

        return ((int)result.fiscal_year, (int)result.fiscal_quarter);
    }
}