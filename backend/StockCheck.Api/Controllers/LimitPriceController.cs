using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StockCheck.Api.Repositories;
using StockCheck.Api.Services;

namespace StockCheck.Api.Controllers;

/// <summary>
/// 指値計算 Controller
/// 指値設定の登録・取得と指値計算を行う
/// </summary>
[ApiController]
[Route("api/limit-price")]
public class LimitPriceController : ControllerBase
{
    private readonly LimitPriceService _limitPriceService;
    private readonly LimitPriceSettingsRepository _settingsRepository;

    public LimitPriceController(
        LimitPriceService limitPriceService,
        LimitPriceSettingsRepository settingsRepository)
    {
        _limitPriceService = limitPriceService;
        _settingsRepository = settingsRepository;
    }

    /// <summary>
    /// Claim からログイン中ユーザーのIDを取得するヘルパー
    /// </summary>
    private int? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// 指値設定を取得する（パターン別）
    /// </summary>
    [HttpGet("settings/{pattern}")]
    public async Task<IActionResult> GetSettings(int pattern)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (pattern != 1 && pattern != 2)
            return BadRequest("pattern must be 1 or 2.");

        var settings = await _settingsRepository.GetAsync(userId.Value, pattern);

        // 未登録の場合は404を返す（フロントで設定画面を表示する）
        if (settings == null)
            return NotFound(new { message = "設定が未登録です。下落率を設定してください。" });

        return Ok(settings);
    }

    /// <summary>
    /// 指値設定を登録・更新する（パターン別）
    /// </summary>
    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] LimitPriceSettingsRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (request.Pattern != 1 && request.Pattern != 2)
            return BadRequest("pattern must be 1 or 2.");

        if (request.PeakDropRate <= 0 || request.PeakDropRate >= 100)
            return BadRequest("peakDropRate must be between 0 and 100.");

        if (request.AvgDropRate <= 0 || request.AvgDropRate >= 100)
            return BadRequest("avgDropRate must be between 0 and 100.");

        await _settingsRepository.UpsertAsync(
            userId.Value,
            request.Pattern,
            request.PeakDropRate,
            request.AvgDropRate);

        return Ok(new { message = "設定を保存しました。" });
    }

    /// <summary>
    /// ウォッチリスト全銘柄の指値を計算して返す
    /// </summary>
    [HttpGet("calc/{pattern}")]
    public async Task<IActionResult> Calc(int pattern)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (pattern != 1 && pattern != 2)
            return BadRequest("pattern must be 1 or 2.");

        var result = await _limitPriceService.CalcAsync(userId.Value, pattern);
        return Ok(result);
    }
}

/// <summary>
/// 指値設定登録リクエスト
/// </summary>
public sealed class LimitPriceSettingsRequest
{
    public int Pattern { get; set; }           // 1 or 2
    public decimal PeakDropRate { get; set; }  // 最高値軸の下落率（例：20.00）
    public decimal AvgDropRate { get; set; }   // 平均軸の下落率（例：10.00）
}