using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Controllers;

/// <summary>
/// ウォッチリスト管理 Controller
/// - 一覧取得
/// - 追加
/// - 削除
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
    /// ウォッチリスト一覧取得
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repository.GetAllAsync();
        return Ok(list);
    }

    /// <summary>
    /// ウォッチリストへ追加
    /// （symbols はフロントの v-select で制御）
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] WatchlistRequest request)
    {
        var symbol = (request.Symbol ?? string.Empty).Trim().ToUpper();
        var market = (request.Market ?? "US").Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Symbol is required.");
        }

        await _repository.AddAsync(symbol, market);

        return Ok(new
        {
            Symbol = symbol,
            Market = market
        });
    }

    /// <summary>
    /// ウォッチリストから削除
    /// </summary>
    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] WatchlistRequest request)
    {
        var symbol = (request.Symbol ?? string.Empty).Trim().ToUpper();
        var market = (request.Market ?? "US").Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Symbol is required.");
        }

        await _repository.RemoveAsync(symbol, market);

        return Ok(new
        {
            Symbol = symbol,
            Market = market
        });
    }
}
