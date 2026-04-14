using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// ウォッチリスト Repository
/// DB: watchlist テーブルを操作する
/// user_id でユーザーごとに分離されている
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
    public async Task AddAsync(int userId, string symbol, string market)
    {
        var sql = $@"
        INSERT INTO {_connectionFactory.Schema}.watchlist (user_id, symbol, market)
        VALUES (@userId, @symbol, @market)
        ON CONFLICT (user_id, symbol, market) DO NOTHING;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// ウォッチリストから削除
    /// </summary>
    public async Task RemoveAsync(int userId, string symbol, string market)
    {
        var sql = $@"
        DELETE FROM {_connectionFactory.Schema}.watchlist
        WHERE user_id = @userId
        AND symbol = @symbol
        AND market = @market;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// ウォッチリスト一覧取得（ログインユーザーのみ）
    /// </summary>
    public async Task<List<Watchlist>> GetAllAsync(int userId)
    {
        var sql = $@"
        SELECT
            id,
            user_id,
            symbol,
            market,
            created_at
        FROM {_connectionFactory.Schema}.watchlist
        WHERE user_id = @userId
        ORDER BY created_at DESC;
        ";

        var list = new List<Watchlist>();

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("userId", userId);

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Watchlist
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Symbol = reader.GetString(2),
                Market = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4)
            });
        }

        return list;
    }

    /// <summary>
    /// ウォッチリストに存在するか判定（ユーザー別）
    /// </summary>
    public async Task<bool> ExistsAsync(int userId, string symbol, string market)
    {
        var sql = $@"
        SELECT 1
        FROM {_connectionFactory.Schema}.watchlist
        WHERE user_id = @userId
        AND symbol = @symbol
        AND market = @market
        LIMIT 1;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
}