using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Models.Entities;   // ← User クラスの名前空間
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 認証サービス
/// ログイン処理を担当する
/// </summary>
public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;

    public AuthService(
        UserRepository userRepository,
        PasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// ログイン処理
    /// 成功時はUserオブジェクトを返す
    /// 失敗時はnullを返す
    /// </summary>
    public async Task<User?> LoginAsync(LoginRequest request)
    {
        // DBからlogin_idでユーザーを取得する
        var user = await _userRepository.GetByLoginIdAsync(request.LoginId);

        if (user == null)
        {
            Console.WriteLine("❌ user not found");
            return null;
        }

        if (!user.IsActive)
        {
            Console.WriteLine("❌ user inactive");
            return null;
        }

        // パスワードの検証
        var result = _passwordHasher.Verify(
            request.Password,
            user.PasswordHash
        );

        Console.WriteLine($"🔑 password verify result: {result}");

        // 検証成功時はUserオブジェクトを返す
        return result ? user : null;
    }
}