using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Models.Analysis;
using StockCheck.Api.Services;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Controllers;

/// <summary>
/// 株価分析（Phase0）Controller
///
/// 【役割】
/// ・分析画面で使用する API エンドポイント
/// ・ビジネスロジックは Service に完全委譲
/// </summary>
[ApiController]
[Route("api/analysis")]
[AllowAnonymous]
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
    /// 分析画面用：ウォッチリスト銘柄一覧取得
    /// </summary>
    [HttpGet("watchlist")]
    public async Task<IActionResult> GetWatchlist()
    {
        var list = await _watchlistRepository.GetAllAsync();
        return Ok(list);
    }

    /// <summary>
    /// 株価分析実行（Phase0）
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<AnalysisResultDto>> Execute(
        [FromBody] AnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest("Symbol is required.");

        var symbol = request.Symbol.Trim().ToUpperInvariant();

        // Service 実行
        AnalysisResponse analysis =
            await _analysisService.AnalyzeAsync(request);

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

            // Phase0 では Metrics は空で返す（後続で詰める）
            Metrics = new AnalysisMetricsDto()
        };

        return Ok(dto);
    }
}
