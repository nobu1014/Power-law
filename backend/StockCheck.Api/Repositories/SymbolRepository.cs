using Npgsql;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;

namespace StockCheck.Api.Repositories;

/// <summary>
/// 銘柄マスタ Repository
/// </summary>
public class SymbolRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public SymbolRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// 銘柄を存在しなければ登録する
    /// true: 新規登録 / false: 既存のためスキップ
    /// </summary>
    public async Task<bool> InsertIfNotExistsAsync(
        string symbol,
        string market,
        CancellationToken ct)
    {
        const string sql = @"
        INSERT INTO power_test.symbols (symbol, market)
        VALUES (@symbol, @market)
        ON CONFLICT (symbol, market) DO NOTHING;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    /// <summary>
    /// 銘柄一覧取得（全件）
    /// </summary>
    public async Task<List<Symbol>> GetAllAsync()
    {
        const string sql = @"
        SELECT
            id,
            symbol,
            market,
            created_at
        FROM power_test.symbols
        ORDER BY symbol;
        ";

        var list = new List<Symbol>();

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Symbol
            {
                Id = reader.GetInt32(0),
                SymbolCode = reader.GetString(1),
                Market = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return list;
    }

    /// <summary>
    /// symbol + market で銘柄エンティティを取得する
    /// </summary>
    public async Task<Symbol?> GetBySymbolAsync(string symbol, string market)
    {
        const string sql = @"
        SELECT
            id,
            symbol,
            market,
            created_at
        FROM power_test.symbols
        WHERE symbol = @symbol
          AND market = @market
        LIMIT 1;
        ";

        await using var conn = await _connectionFactory.CreateAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("symbol", symbol);
        cmd.Parameters.AddWithValue("market", market);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Symbol
            {
                Id = reader.GetInt32(0),
                SymbolCode = reader.GetString(1),
                Market = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        return null;
    }
}
