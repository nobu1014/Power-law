using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// ウォッチリスト Repository
/// DB: power_test.watchlist を操作する
/// 
/// 方針:
/// - symbols テーブルとは紐付けない
/// - symbol / market をそのまま保持
/// - UNIQUE(symbol, market) により重複を防止
/// </summary>
public class WatchlistRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public WatchlistRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// ウォッチリストに追加（既に存在する場合は何もしない）
    /// </summary>
    public async Task AddAsync(string symbol, string market)
    {
        const string sql = @"
        INSERT INTO power_test.watchlist (symbol, market)
        VALUES (@symbol, @market)
        ON CONFLICT (symbol, market) DO NOTHING;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// ウォッチリストから削除
    /// </summary>
    public async Task RemoveAsync(string symbol, string market)
    {
        const string sql = @"
        DELETE FROM power_test.watchlist
        WHERE symbol = @symbol
        AND market = @market;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// ウォッチリスト一覧取得
    /// </summary>
    public async Task<List<Watchlist>> GetAllAsync()
    {
        const string sql = @"
        SELECT
            id,
            symbol,
            market,
            created_at
        FROM power_test.watchlist
        ORDER BY created_at DESC;
        ";

        var list = new List<Watchlist>();

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Watchlist
            {
                Id = reader.GetInt32(0),
                Symbol = reader.GetString(1),
                Market = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return list;
    }

    /// <summary>
    /// ウォッチリストに存在するか判定
    /// </summary>
    public async Task<bool> ExistsAsync(string symbol, string market)
    {
        const string sql = @"
        SELECT 1
        FROM power_test.watchlist
        WHERE symbol = @symbol
        AND market = @market
        LIMIT 1;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
}
