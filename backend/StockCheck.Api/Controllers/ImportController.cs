using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Services;

namespace StockCheck.Api.Controllers;

/// <summary>
/// データ Import 用 Controller
///
/// ・手動実行
/// ・デバッグ
/// ・Swagger 操作
/// </summary>
[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly ImportService _importService;

    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

    // =====================================================
    // 単一銘柄 Import
    // =====================================================

    /// <summary>
    /// 指定銘柄の株価・EPS を Import
    /// </summary>
    /// <param name="symbol">銘柄コード（例: AAPL）</param>
    [HttpPost("symbol/{symbol}")]
    public async Task<IActionResult> ImportSymbol(
        string symbol,
        [FromQuery] int maxPriceYears = 5,
        [FromQuery] int maxEpsQuarters = 16)
    {
        await _importService.ImportBySymbolAsync(
            symbol: symbol.ToUpper(),
            maxPriceYears: maxPriceYears,
            maxEpsQuarters: maxEpsQuarters,
            ct: HttpContext.RequestAborted
        );

        return Ok(new
        {
            symbol,
            maxPriceYears,
            maxEpsQuarters,
            status = "started"
        });
    }

    // =====================================================
    // 全銘柄 Import
    // =====================================================

    /// <summary>
    /// symbols テーブルに登録されている全銘柄を Import
    /// </summary>
    [HttpPost("all")]
    public async Task<IActionResult> ImportAll(
        [FromQuery] int maxPriceYears = 5,
        [FromQuery] int maxEpsQuarters = 16)
    {
        await _importService.ImportAllAsync(
            maxPriceYears: maxPriceYears,
            maxEpsQuarters: maxEpsQuarters,
            ct: HttpContext.RequestAborted
        );

        return Ok(new
        {
            maxPriceYears,
            maxEpsQuarters,
            status = "started"
        });
    }
}
