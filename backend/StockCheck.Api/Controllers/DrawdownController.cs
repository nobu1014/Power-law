using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StockCheck.Api.Services;

namespace StockCheck.Api.Controllers;

/// <summary>
/// 下落チェック Controller
/// ログインユーザーのウォッチリスト銘柄を対象に下落率を取得する
/// </summary>
[ApiController]
[Route("api/drawdown")]
public class DrawdownController : ControllerBase
{
    private readonly DrawdownService _service;

    public DrawdownController(DrawdownService service)
    {
        _service = service;
    }

    /// <summary>
    /// Claim からログイン中ユーザーのIDを取得するヘルパー
    /// 未ログインの場合は null を返す
    /// </summary>
    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// ログインユーザーのウォッチ銘柄の下落率一覧を取得する
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int periodMonths = 3,
        [FromQuery] string sortOrder = "desc")
    {
        // ログインユーザーのIDを取得する
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var allowedPeriods = new[] { 1, 3, 6, 12 };
        if (!allowedPeriods.Contains(periodMonths))
            return BadRequest("periodMonths must be 1, 3, 6, or 12.");

        if (sortOrder != "asc" && sortOrder != "desc")
            sortOrder = "desc";

        var result = await _service.GetDrawdownListAsync(
            userId.Value,
            periodMonths,
            sortOrder
        );

        return Ok(result);
    }

    /// <summary>
    /// ログインユーザーのウォッチリスト銘柄の最新データを取得する
    /// （重い処理・明示実行）
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        // ログインユーザーのIDを取得する
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        await _service.RefreshLatestDataAsync(userId.Value, ct);

        return Ok(new { message = "最新データの取得が完了しました" });
    }
}