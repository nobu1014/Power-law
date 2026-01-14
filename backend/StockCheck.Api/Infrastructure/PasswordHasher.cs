using BCrypt.Net;

namespace StockCheck.Api.Infrastructure;

/// <summary>
/// パスワードハッシュ管理（BCrypt）

/// </summary>
public class PasswordHasher
{
    /// <summary>
    /// 平文パスワードをハッシュ化
    /// </summary>
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    /// <summary>
    /// パスワード検証
    /// </summary>
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
