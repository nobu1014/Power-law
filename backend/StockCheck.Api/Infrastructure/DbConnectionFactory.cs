using Npgsql;
using Microsoft.Extensions.Configuration;

namespace StockCheck.Api.Infrastructure;

/// <summary>
/// DB接続ファクトリ
/// 接続文字列とスキーマ名を管理する
/// 各Repositoryはこのクラスを通じてDB接続とスキーマ名を取得する
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// appsettings.json の DbSettings:Schema から取得したスキーマ名
    /// 各Repositoryはこのプロパティを使ってSQLを組み立てる
    /// 例: $"SELECT * FROM {_db.Schema}.watchlist"
    /// </summary>
    public string Schema { get; }

    public DbConnectionFactory(IConfiguration configuration)
    {
        // 接続文字列を設定から取得する
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string not found.");

        // スキーマ名を設定から取得する（appsettings.jsonのDbSettings:Schema）
        Schema = configuration["DbSettings:Schema"]
            ?? throw new InvalidOperationException("DbSettings:Schema not found.");
    }

    public async Task<NpgsqlConnection> CreateAsync()
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}