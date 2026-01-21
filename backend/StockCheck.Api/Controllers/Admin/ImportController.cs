using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Models.Import;
using StockCheck.Api.Services;

namespace StockCheck.Api.Controllers.Admin;

/// <summary>
/// 管理者専用：Import手動実行API
///
/// 【責務】
/// ・管理者かどうかを判定する
/// ・ImportService を呼び出す
/// ・Import結果（Summary）をそのまま返す
/// </summary>
[ApiController]
[Route("api/admin/import")]
[Authorize]
public sealed class ImportController : ControllerBase
{
    private const string ADMIN_LOGIN_ID = "nobu1014b.b";

    private readonly ImportService _importService;

    /// <summary>
    /// Import処理を統括するServiceを受け取る
    /// </summary>
    public ImportController(ImportService importService)
    {
        _importService = importService;
    }

/// <summary>
    /// 【管理画面】
    /// 登録済みの全銘柄に対してImportを実行する
    /// </summary>
    [HttpPost("all")]
    public async Task<ActionResult<List<ImportSummary>>> ImportAll(
        CancellationToken ct)
    {
        // 管理者以外は実行不可
        if (!IsAdmin())
            return Forbid();

        // ★ 管理画面専用の安全な全件Importを呼び出す
        var result = await _importService.ImportAllForAdminAsync(ct);

        return Ok(result);
    }

    /// <summary>
    /// 【管理画面】
    /// 指定銘柄 Import（安全版）
    /// </summary>
    [HttpPost("symbol/{symbol}")]
    public async Task<ActionResult<ImportSummary>> ImportBySymbol(
        string symbol,
        CancellationToken ct)
    {
        if (!IsAdmin())
            return Forbid();

        // ★ NightBatch ではなく、管理画面専用メソッドを呼ぶ
        var result =
            await _importService.ImportBySymbolForAdminAsync(symbol, ct);

        return Ok(result);
    }

    /// <summary>
    /// 現在ログイン中のユーザーが管理者かどうかを判定する
    /// </summary>
    private bool IsAdmin()
    {
        var loginId =
            User.Identity?.Name ??
            User.Claims.FirstOrDefault(c => c.Type == "login_id")?.Value;

        return loginId == ADMIN_LOGIN_ID;
    }
}
