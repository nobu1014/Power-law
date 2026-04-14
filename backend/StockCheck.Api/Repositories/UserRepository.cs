using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// ユーザー Repository
/// DB: users テーブルを操作する
/// </summary>
public class UserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// login_id からユーザーを取得する
    /// </summary>
    public async Task<User?> GetByLoginIdAsync(string loginId)
    {
        // スキーマ名を appsettings.json から取得して SQL に組み込む
        var sql = $@"
        SELECT
            id,
            login_id,
            password_hash,
            is_active,
            created_at
        FROM {_connectionFactory.Schema}.users
        WHERE login_id = @login_id
        LIMIT 1;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("login_id", loginId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                LoginId = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                IsActive = reader.GetBoolean(3),
                CreatedAt = reader.GetDateTime(4)
            };
        }

        return null;
    }
}