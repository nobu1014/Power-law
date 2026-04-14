using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Models.Entities;   // ← User クラスの名前空間
using StockCheck.Api.Services;

namespace StockCheck.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// ログイン
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // ログイン処理（成功時はUserオブジェクトが返る）
        var user = await _authService.LoginAsync(request);
        if (user == null)
        {
            return Unauthorized();
        }

        // 管理者判定
        var isAdmin = user.LoginId == "nobu1014b.b";

        // Claimにユーザーの情報を設定する
        // NameIdentifier にDBのidを入れることで
        // 各Controllerでログイン中ユーザーのidが取得できる
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ★ DBのidを追加
            new Claim(ClaimTypes.Name, user.LoginId),
            new Claim("IsAdmin", isAdmin.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Ok(new
        {
            loginId = user.LoginId,
            isAdmin
        });
    }

    /// <summary>
    /// ログアウト
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return Ok();
    }
}