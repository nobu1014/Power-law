using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// EPS（四半期）を Import するための専用 Repository
/// </summary>
public sealed class EpsImportRepository
{
    private readonly DbConnectionFactory _db;

    /// <summary>
    /// DB接続ファクトリを受け取る
    /// </summary>
    public EpsImportRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// 指定銘柄について、既に登録済みの決算期（年・四半期）一覧を取得する
    /// </summary>
    public async Task<HashSet<(int Year, int Quarter)>> GetExistingPeriodsAsync(
        int symbolId)
    {
        const string sql = """
            SELECT fiscal_year, fiscal_quarter
            FROM power_test.eps_quarterly
            WHERE symbol_id = @symbolId
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        var set = new HashSet<(int, int)>();
        while (await reader.ReadAsync())
        {
            set.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        return set;
    }

    /// <summary>
    /// EPS（四半期）を1件INSERTする（既存期はUNIQUE制約で無視される）
    /// </summary>
    public async Task InsertAsync(Earnings e)
    {
        const string sql = """
            INSERT INTO power_test.eps_quarterly
            (symbol_id, fiscal_year, fiscal_quarter, eps, report_date)
            VALUES
            (@sid, @y, @q, @eps, @date)
            ON CONFLICT (symbol_id, fiscal_year, fiscal_quarter)
            DO NOTHING
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("sid", e.SymbolId);
        cmd.Parameters.AddWithValue("y", e.FiscalYear);
        cmd.Parameters.AddWithValue("q", e.FiscalQuarter);
        cmd.Parameters.AddWithValue("eps", e.Eps);
        cmd.Parameters.AddWithValue(
            "date",
            (object?)e.ReportDate ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 最新keep件を超えた古いEPSデータを削除する
    /// </summary>
    public async Task<int> DeleteOldAsync(int symbolId, int keep)
    {
        const string sql = """
            WITH ranked AS (
                SELECT id,
                       ROW_NUMBER() OVER (
                         ORDER BY fiscal_year DESC, fiscal_quarter DESC
                       ) rn
                FROM power_test.eps_quarterly
                WHERE symbol_id = @symbolId
            )
            DELETE FROM power_test.eps_quarterly
            WHERE id IN (
                SELECT id FROM ranked WHERE rn > @keep
            )
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("keep", keep);

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 指定銘柄の最新EPS期（年・四半期）を取得する
    /// NightBatch 実行時の API取得可否判定用
    /// </summary>
    public async Task<(int FiscalYear, int FiscalQuarter)?> GetLatestPeriodAsync(
        int symbolId)
    {
        const string sql = """
        SELECT fiscal_year, fiscal_quarter
        FROM power_test.eps_quarterly
        WHERE symbol_id = @symbolId
        ORDER BY fiscal_year DESC, fiscal_quarter DESC
        LIMIT 1
    """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return (
            reader.GetInt32(0), // FiscalYear
            reader.GetInt32(1)  // FiscalQuarter
        );
    }

}
