using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 認証サービス
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
    /// </summary>
    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByLoginIdAsync(request.LoginId);

        if (user == null)
        {
            Console.WriteLine("❌ user not found");
            return false;
        }

        if (!user.IsActive)
        {
            Console.WriteLine("❌ user inactive");
            return false;
        }

        var result = _passwordHasher.Verify(
            request.Password,
            user.PasswordHash
        );

        Console.WriteLine($"🔑 password verify result: {result}");

        return result;
    }

}
