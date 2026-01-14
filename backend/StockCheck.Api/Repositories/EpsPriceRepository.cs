using Dapper;
using Npgsql;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// EPS × 株価（四半期）取得用 Repository
///
/// 【責務】
/// ・eps_price_recent_view からデータを取得
/// ・「DBにどれだけEPSがあるか」を返す
/// ・Import が必要かどうかの判断材料を提供
/// </summary>
public class EpsPriceRepository
{
    private readonly string _connectionString;

    public EpsPriceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string not found.");
    }

    // =====================================================
    // 取得系（表示用）
    // =====================================================

    /// <summary>
    /// 指定銘柄の EPS × 株価（四半期）を直近順で取得
    /// </summary>
    public async Task<List<EpsPriceRow>> GetRecentAsync(
        int symbolId,
        int range)
    {
        range = Math.Clamp(range, 1, 32);

        const string sql = """
            SELECT
                fiscal_year               AS FiscalYear,
                fiscal_quarter            AS FiscalQuarter,
                eps                       AS Eps,
                close_price_before_report AS ClosePrice,
                report_date               AS TradeDate,
                per                       AS Per
            FROM power_test.eps_price_recent_view
            WHERE symbol_id = @SymbolId
            ORDER BY fiscal_year DESC, fiscal_quarter DESC
            LIMIT @Limit
        """;

        await using var conn = new NpgsqlConnection(_connectionString);

        var result = await conn.QueryAsync<EpsPriceRow>(
            sql,
            new
            {
                SymbolId = symbolId,
                Limit = range
            });

        return result.ToList();
    }

    // =====================================================
    // 判定系（Service 用）
    // =====================================================

    /// <summary>
    /// 指定銘柄の EPS 件数を取得
    /// Import が必要かどうかの判定用
    /// </summary>
    public async Task<int> CountAsync(int symbolId)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM power_test.eps_quarterly
            WHERE symbol_id = @SymbolId
        """;

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.ExecuteScalarAsync<int>(
            sql,
            new { SymbolId = symbolId }
        );
    }

    /// <summary>
    /// 指定銘柄の最新 EPS 期を取得
    /// </summary>
    public async Task<(int FiscalYear, int FiscalQuarter)?> GetLatestPeriodAsync(
        int symbolId)
    {
        const string sql = """
            SELECT fiscal_year, fiscal_quarter
            FROM power_test.eps_quarterly
            WHERE symbol_id = @SymbolId
            ORDER BY fiscal_year DESC, fiscal_quarter DESC
            LIMIT 1
        """;

        await using var conn = new NpgsqlConnection(_connectionString);

        var result = await conn.QueryFirstOrDefaultAsync(sql, new
        {
            SymbolId = symbolId
        });

        if (result == null)
        {
            return null;
        }

        return ((int)result.fiscal_year, (int)result.fiscal_quarter);
    }
}
