using Npgsql;
using Microsoft.Extensions.Configuration;

namespace StockCheck.Api.Infrastructure;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Postgres connection string not found.");
    }

    public async Task<NpgsqlConnection> CreateAsync()
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
