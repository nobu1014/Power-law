using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Models.Analysis;
using StockCheck.Api.Services;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Controllers;

/// <summary>
/// 株価分析 Controller
/// ログインユーザーのウォッチリストを使って分析を行う
/// </summary>
[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly AnalysisService _analysisService;
    private readonly WatchlistRepository _watchlistRepository;

    public AnalysisController(
        AnalysisService analysisService,
        WatchlistRepository watchlistRepository)
    {
        _analysisService = analysisService;
        _watchlistRepository = watchlistRepository;
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
    /// 分析画面用：ログインユーザーのウォッチリスト銘柄一覧取得
    /// </summary>
    [HttpGet("watchlist")]
    public async Task<IActionResult> GetWatchlist()
    {
        // ログインユーザーのIDを取得する
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var list = await _watchlistRepository.GetAllAsync(userId.Value);
        return Ok(list);
    }

    /// <summary>
    /// 株価分析実行
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<AnalysisResultDto>> Execute(
        [FromBody] AnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest("Symbol is required.");

        // ログインユーザーのIDをリクエストにセットする
        // （フロントからは送らず、サーバー側で付与する）
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        request.UserId = userId.Value;

        var symbol = request.Symbol.Trim().ToUpperInvariant();

        // Service 実行
        var analysis = await _analysisService.AnalyzeAsync(request);

        // DTO 変換
        var dto = new AnalysisResultDto
        {
            Symbol = symbol,

            Range = new AnalysisRangeDto
            {
                From = analysis.Price.Prices.Any()
                    ? analysis.Price.Prices.First().Date.ToString("yyyy-MM-dd")
                    : string.Empty,
                To = analysis.Price.Prices.Any()
                    ? analysis.Price.Prices.Last().Date.ToString("yyyy-MM-dd")
                    : string.Empty
            },

            PriceSeries = analysis.Price.Prices
                .Select(p => new TimeSeriesPointDto
                {
                    Date = p.Date.ToString("yyyy-MM-dd"),
                    Value = p.Value
                })
                .ToList(),

            EpsSeries = analysis.Eps.EpsList
                .Select(e => new TimeSeriesPointDto
                {
                    Date = e.Period,
                    Value = e.Value
                })
                .ToList(),

            Metrics = new AnalysisMetricsDto()
        };

        return Ok(dto);
    }
}