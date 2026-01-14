using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Services;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Controllers;

/// <summary>
/// 株価分析（Phase0）Controller
///
/// 【役割】
/// ・分析画面で使用する API エンドポイント
/// ・ビジネスロジックは持たず、Service に委譲する
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
    /// 分析画面用：ウォッチリスト銘柄一覧取得
    ///
    /// ・コンボボックス初期表示用
    /// ・symbol / market のみ返す
    /// </summary>
    [HttpGet("watchlist")]
    public async Task<IActionResult> GetWatchlist()
    {
        var list = await _watchlistRepository.GetAllAsync();
        return Ok(list);
    }

    /// <summary>
    /// 株価分析実行（Phase0）
    ///
    /// ・株価 / EPS / PER をすべて計算
    /// ・表示切替はフロントエンド側で行う
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] AnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            return BadRequest("Symbol is required.");
        }

        // Service に完全委譲
        var result = await _analysisService.AnalyzeAsync(request);

        return Ok(result);
    }
}
