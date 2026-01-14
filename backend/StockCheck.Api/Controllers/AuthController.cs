using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<bool>> Login([FromBody] LoginRequest request)
    {
        var success = await _authService.LoginAsync(request);

        // ★ 成功 / 失敗を boolean で返す
        return Ok(success);
    }
}
