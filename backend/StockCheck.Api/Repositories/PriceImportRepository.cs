using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// 日次株価 Import 専用 Repository
/// </summary>
public sealed class PriceImportRepository
{
    private readonly DbConnectionFactory _db;

    public PriceImportRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// 指定銘柄の最新取引日を取得する
    /// </summary>
    public async Task<DateTime?> GetLatestTradeDateAsync(int symbolId)
    {
        var sql = $"""
            SELECT MAX(trade_date)
            FROM {_db.Schema}.price_daily
            WHERE symbol_id = @symbolId
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);

        var result = await cmd.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value) return null;
        if (result is DateOnly d) return d.ToDateTime(TimeOnly.MinValue);

        return (DateTime)result;
    }

    /// <summary>
    /// 指定銘柄の既存取引日一覧を取得する
    /// </summary>
    public async Task<HashSet<DateTime>> GetExistingDatesAsync(int symbolId)
    {
        var sql = $"""
            SELECT trade_date
            FROM {_db.Schema}.price_daily
            WHERE symbol_id = @symbolId
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        var set = new HashSet<DateTime>();
        while (await reader.ReadAsync())
            set.Add(reader.GetDateTime(0).Date);

        return set;
    }

    /// <summary>
    /// 日次株価を1件INSERTする（既存日はスキップ）
    /// </summary>
    public async Task InsertAsync(PriceDaily price)
    {
        var sql = $"""
            INSERT INTO {_db.Schema}.price_daily
            (symbol_id, trade_date, close_price)
            VALUES (@sid, @date, @price)
            ON CONFLICT (symbol_id, trade_date) DO NOTHING
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("sid", price.SymbolId);
        cmd.Parameters.AddWithValue("date", price.TradeDate.Date);
        cmd.Parameters.AddWithValue("price", price.ClosePrice);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 古い株価データを削除する（keepYears年を超えた分）
    /// </summary>
    public async Task<int> DeleteOldAsync(int symbolId, int keepYears)
    {
        var sql = $"""
            DELETE FROM {_db.Schema}.price_daily
            WHERE symbol_id = @symbolId
              AND trade_date < CURRENT_DATE - (@years || ' years')::INTERVAL
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("years", keepYears);

        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 指定銘柄の取引日と終値を日付順で取得する（欠損補完用）
    /// </summary>
    public async Task<List<(DateTime Date, decimal ClosePrice)>> GetDatePriceListAsync(
        int symbolId,
        DateTime fromDate)
    {
        var sql = $"""
            SELECT trade_date, close_price
            FROM {_db.Schema}.price_daily
            WHERE symbol_id = @symbolId
              AND trade_date >= @fromDate
            ORDER BY trade_date
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("fromDate", fromDate.Date);

        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<(DateTime, decimal)>();
        while (await reader.ReadAsync())
            list.Add((reader.GetDateTime(0).Date, reader.GetDecimal(1)));

        return list;
    }
}