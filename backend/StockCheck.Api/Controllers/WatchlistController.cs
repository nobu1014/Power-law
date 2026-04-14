using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Controllers;

/// <summary>
/// ウォッチリスト管理 Controller
/// ログインユーザーごとに独立したウォッチリストを管理する
/// </summary>
[ApiController]
[Route("api/watchlist")]
public class WatchlistController : ControllerBase
{
    private readonly WatchlistRepository _repository;

    public WatchlistController(WatchlistRepository repository)
    {
        _repository = repository;
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
    /// ウォッチリスト一覧取得（ログインユーザーのみ）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // ログインユーザーのIDを取得する
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var list = await _repository.GetAllAsync(userId.Value);
        return Ok(list);
    }

    /// <summary>
    /// ウォッチリストへ追加
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] WatchlistRequest request)
    {
        // ログインユーザーのIDを取得する
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var symbol = (request.Symbol ?? string.Empty).Trim().ToUpper();
        var market = (request.Market ?? "US").Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        await _repository.AddAsync(userId.Value, symbol, market);

        return Ok(new { Symbol = symbol, Market = market });
    }

    /// <summary>
    /// ウォッチリストから削除
    /// </summary>
    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] WatchlistRequest request)
    {
        // ログインユーザーのIDを取得する
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var symbol = (request.Symbol ?? string.Empty).Trim().ToUpper();
        var market = (request.Market ?? "US").Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        await _repository.RemoveAsync(userId.Value, symbol, market);

        return Ok(new { Symbol = symbol, Market = market });
    }
}