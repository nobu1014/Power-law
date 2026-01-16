using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using StockCheck.Api.Models.Requests;
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
        var success = await _authService.LoginAsync(request);
        if (!success)
        {
            return Unauthorized();
        }

        // ★ 管理者判定（要件どおり）
        var isAdmin = request.LoginId == "nobu1014b.b";

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, request.LoginId),
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
            loginId = request.LoginId,
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
