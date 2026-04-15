using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// EPS（四半期）Import 専用 Repository
/// </summary>
public sealed class EpsImportRepository
{
    private readonly DbConnectionFactory _db;

    public EpsImportRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// 指定銘柄の登録済み決算期一覧を取得する
    /// </summary>
    public async Task<HashSet<(int Year, int Quarter)>> GetExistingPeriodsAsync(int symbolId)
    {
        var sql = $"""
            SELECT fiscal_year, fiscal_quarter
            FROM {_db.Schema}.eps_quarterly
            WHERE symbol_id = @symbolId
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        var set = new HashSet<(int, int)>();
        while (await reader.ReadAsync())
            set.Add((reader.GetInt32(0), reader.GetInt32(1)));

        return set;
    }

    /// <summary>
    /// EPS（四半期）を1件INSERTする（既存期はスキップ）
    /// </summary>
    public async Task InsertAsync(Earnings e)
    {
        var sql = $"""
            INSERT INTO {_db.Schema}.eps_quarterly
            (symbol_id, fiscal_year, fiscal_quarter, eps, report_date)
            VALUES (@sid, @y, @q, @eps, @date)
            ON CONFLICT (symbol_id, fiscal_year, fiscal_quarter) DO NOTHING
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("sid", e.SymbolId);
        cmd.Parameters.AddWithValue("y", e.FiscalYear);
        cmd.Parameters.AddWithValue("q", e.FiscalQuarter);
        cmd.Parameters.AddWithValue("eps", e.Eps);
        cmd.Parameters.AddWithValue("date", (object?)e.ReportDate ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 古いEPSデータを削除する（keep件を超えた分）
    /// </summary>
    public async Task<int> DeleteOldAsync(int symbolId, int keep)
    {
        var sql = $"""
            WITH ranked AS (
                SELECT id,
                       ROW_NUMBER() OVER (
                         ORDER BY fiscal_year DESC, fiscal_quarter DESC
                       ) rn
                FROM {_db.Schema}.eps_quarterly
                WHERE symbol_id = @symbolId
            )
            DELETE FROM {_db.Schema}.eps_quarterly
            WHERE id IN (SELECT id FROM ranked WHERE rn > @keep)
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("keep", keep);

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 指定銘柄の最新EPS期を取得する
    /// </summary>
    public async Task<(int FiscalYear, int FiscalQuarter)?> GetLatestPeriodAsync(int symbolId)
    {
        var sql = $"""
            SELECT fiscal_year, fiscal_quarter
            FROM {_db.Schema}.eps_quarterly
            WHERE symbol_id = @symbolId
            ORDER BY fiscal_year DESC, fiscal_quarter DESC
            LIMIT 1
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        return (reader.GetInt32(0), reader.GetInt32(1));
    }
}