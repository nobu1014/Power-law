using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// EPS（四半期）Repository
/// DB: power_test.eps_quarterly
///
/// 方針:
/// ・Repository では計算をしない
/// ・「存在確認・件数取得・時系列取得」のみを責務とする
/// </summary>
public class EarningsRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public EarningsRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // =====================================================
    // 表示・計算用
    // =====================================================

    /// <summary>
    /// 直近 n 回分の EPS を取得する
    /// （差分計算のため、表示件数 + 1 を取得する想定）
    /// </summary>
    public async Task<List<Earnings>> GetLatestAsync(
        int symbolId,
        int count)
    {
        const string sql = @"
        SELECT
            id,
            symbol_id,
            fiscal_year,
            fiscal_quarter,
            eps,
            report_date,
            created_at
        FROM power_test.eps_quarterly
        WHERE symbol_id = @symbolId
        ORDER BY fiscal_year DESC, fiscal_quarter DESC
        LIMIT @count;
        ";

        var list = new List<Earnings>();

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("count", count);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Earnings
            {
                Id = reader.GetInt32(0),
                SymbolId = reader.GetInt32(1),
                FiscalYear = reader.GetInt32(2),
                FiscalQuarter = reader.GetInt32(3),
                Eps = reader.GetDecimal(4),
                ReportDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                CreatedAt = reader.GetDateTime(6)
            });
        }

        return list;
    }

    // =====================================================
    // 判定用（Service が Import 判断に使う）
    // =====================================================

    /// <summary>
    /// 指定銘柄の EPS 件数を取得する
    /// </summary>
    public async Task<int> CountAsync(int symbolId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM power_test.eps_quarterly
            WHERE symbol_id = @symbolId;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// 指定銘柄の最新 EPS 期を取得する
    /// </summary>
    public async Task<(int FiscalYear, int FiscalQuarter)?> GetLatestPeriodAsync(
        int symbolId)
    {
        const string sql = @"
            SELECT fiscal_year, fiscal_quarter
            FROM power_test.eps_quarterly
            WHERE symbol_id = @symbolId
            ORDER BY fiscal_year DESC, fiscal_quarter DESC
            LIMIT 1;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return (
                reader.GetInt32(0),
                reader.GetInt32(1)
            );
        }

        return null;
    }
}
