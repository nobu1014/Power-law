using Npgsql;
using StockCheck.Api.Infrastructure;

namespace StockCheck.Api.Repositories;

/// <summary>
/// 指値設定 Repository
/// ユーザーごとのパターン別下落率設定を管理する
/// </summary>
public sealed class LimitPriceSettingsRepository
{
    private readonly DbConnectionFactory _db;

    public LimitPriceSettingsRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    /// <summary>
    /// 指値設定を取得する（ユーザー×パターンで1件）
    /// 未登録の場合は null を返す
    /// </summary>
    public async Task<LimitPriceSettingsDto?> GetAsync(int userId, int pattern)
    {
        var sql = $"""
            SELECT id, user_id, pattern, peak_drop_rate, avg_drop_rate, updated_at
            FROM {_db.Schema}.limit_price_settings
            WHERE user_id = @userId AND pattern = @pattern
            LIMIT 1
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("pattern", pattern);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync()) return null;

        return new LimitPriceSettingsDto
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            Pattern = reader.GetInt16(2),
            PeakDropRate = reader.GetDecimal(3),
            AvgDropRate = reader.GetDecimal(4),
            UpdatedAt = reader.GetDateTime(5)
        };
    }

    /// <summary>
    /// 指値設定を登録または更新する（UPSERT）
    /// </summary>
    public async Task UpsertAsync(int userId, int pattern, decimal peakDropRate, decimal avgDropRate)
    {
        var sql = $"""
            INSERT INTO {_db.Schema}.limit_price_settings
                (user_id, pattern, peak_drop_rate, avg_drop_rate, updated_at)
            VALUES
                (@userId, @pattern, @peakDropRate, @avgDropRate, NOW())
            ON CONFLICT (user_id, pattern)
            DO UPDATE SET
                peak_drop_rate = @peakDropRate,
                avg_drop_rate  = @avgDropRate,
                updated_at     = NOW()
        """;

        await using var conn = await _db.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId", userId);
        cmd.Parameters.AddWithValue("pattern", pattern);
        cmd.Parameters.AddWithValue("peakDropRate", peakDropRate);
        cmd.Parameters.AddWithValue("avgDropRate", avgDropRate);

        await cmd.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// 指値設定DTO
/// </summary>
public sealed class LimitPriceSettingsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Pattern { get; set; }
    public decimal PeakDropRate { get; set; }  // 最高値軸の下落率（例：20.00）
    public decimal AvgDropRate { get; set; }   // 平均軸の下落率（例：10.00）
    public DateTime UpdatedAt { get; set; }
}