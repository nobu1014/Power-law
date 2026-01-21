using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Services;

namespace StockCheck.Api.Controllers;

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
    /// ウォッチ銘柄の下落率一覧を取得する
    /// </summary>
    /// <param name="periodMonths">
    /// 対象期間（月）:
    /// 1=1か月, 3=3か月, 6=6か月, 12=1年
    /// </param>
    /// <param name="sortOrder">asc / desc（省略時 desc）</param>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int periodMonths = 3,
        [FromQuery] string sortOrder = "desc")
    {
        // 🔒 想定外の値を防ぐ（UIと仕様を一致させる）
        var allowedPeriods = new[] { 1, 3, 6, 12 };
        if (!allowedPeriods.Contains(periodMonths))
        {
            return BadRequest("periodMonths must be 1, 3, 6, or 12.");
        }

        if (sortOrder != "asc" && sortOrder != "desc")
        {
            sortOrder = "desc";
        }

        var result = await _service.GetDrawdownListAsync(
            periodMonths,
            sortOrder
        );

        return Ok(result);
    }

    /// <summary>
    /// 下落チェック用の最新データを取得する
    /// （重い処理・明示実行）
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        await _service.RefreshLatestDataAsync(ct);
        return Ok(new
        {
            message = "最新データの取得が完了しました"
        });
    }
}
