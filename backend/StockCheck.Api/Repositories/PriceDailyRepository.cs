using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// 日次株価 Repository
/// DB: power_test.price_daily
/// 
/// 方針:
/// ・平均値やYTDの計算は Service 層で行う
/// ・Repository は「期間指定でデータを返す」ことに専念する
/// </summary>
public class PriceDailyRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public PriceDailyRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// 指定期間の株価（日次）を取得する
    /// </summary>
    /// <param name="symbolId">symbols.id</param>
    /// <param name="from">開始日（含む）</param>
    /// <param name="to">終了日（含む）</param>
    public async Task<List<PriceDaily>> GetByDateRangeAsync(
        int symbolId,
        DateTime from,
        DateTime to)
    {
        const string sql = @"
        SELECT
            id,
            symbol_id,
            trade_date,
            close_price,
            created_at
        FROM power_test.price_daily
        WHERE symbol_id = @symbolId
        AND trade_date BETWEEN @from AND @to
        ORDER BY trade_date;
        ";

        var list = new List<PriceDaily>();

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("from", from.Date);
        cmd.Parameters.AddWithValue("to", to.Date);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new PriceDaily
            {
                Id = reader.GetInt32(0),
                SymbolId = reader.GetInt32(1),
                TradeDate = reader.GetDateTime(2),
                ClosePrice = reader.GetDecimal(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return list;
    }

    /// <summary>
    /// 指定日以前の最新株価を取得する
    /// （PER計算用・直近株価取得用）
    /// </summary>
    public async Task<PriceDaily?> GetLatestAsync(int symbolId)
    {
        const string sql = @"
        SELECT
            id,
            symbol_id,
            trade_date,
            close_price,
            created_at
        FROM power_test.price_daily
        WHERE symbol_id = @symbolId
        ORDER BY trade_date DESC
        LIMIT 1;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new PriceDaily
            {
                Id = reader.GetInt32(0),
                SymbolId = reader.GetInt32(1),
                TradeDate = reader.GetDateTime(2),
                ClosePrice = reader.GetDecimal(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return null;
    }

    /// <summary>
    /// 指定銘柄の期間内最高値を取得
    /// </summary>
    public async Task<decimal?> GetPeakPriceAsync(
        int symbolId,
        DateTime fromDate)
    {
        const string sql = @"
        SELECT MAX(close_price)
        FROM power_test.price_daily
        WHERE symbol_id = @symbolId
        AND trade_date >= @fromDate;
    ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);
        cmd.Parameters.AddWithValue("fromDate", fromDate);

        var result = await cmd.ExecuteScalarAsync();
        return result == DBNull.Value ? null : (decimal?)result;
    }

    /// <summary>
/// 指定銘柄の最新取引日を取得する
/// </summary>
    public async Task<DateTime?> GetLatestTradeDateAsync(int symbolId)
    {
        const string sql = @"
            SELECT MAX(trade_date)
            FROM power_test.price_daily
            WHERE symbol_id = @symbolId;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbolId", symbolId);

        var result = await cmd.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        // PostgreSQL date → DateOnly → DateTime
        if (result is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        // 念のための保険（timestamp に変わった場合）
        if (result is DateTime dateTime)
        {
            return dateTime;
        }

        throw new InvalidCastException(
            $"Unexpected date type: {result.GetType()}");
    }
}
