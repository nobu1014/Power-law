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
    /// 登録済みの全銘柄に対してImportを実行する（管理者専用）
    /// </summary>
    [HttpPost("all")]
    public async Task<ActionResult<List<ImportSummary>>> ImportAll(
        CancellationToken ct)
    {
        // 管理者以外は実行不可
        if (!IsAdmin())
            return Forbid();

        // 全銘柄Importを実行し、結果サマリ一覧を返す
        var result = await _importService.ImportAllAsync(ct);

        return Ok(result);
    }

    /// <summary>
    /// 指定銘柄に対してImportを実行する（管理者専用）
    /// </summary>
    [HttpPost("symbol/{symbol}")]
    public async Task<ActionResult<ImportSummary>> ImportBySymbol(
        string symbol,
        CancellationToken ct)
    {
        // 管理者以外は実行不可
        if (!IsAdmin())
            return Forbid();

        // 管理者手動実行は NightBatch 文脈として扱う
        var result = await _importService.ImportBySymbolAsync(
            symbol,
            ImportExecutionContext.NightBatch,
            ct);

        return Ok(result);
    }


    /// <summary>
    /// 現在ログイン中のユーザーが管理者かどうかを判定する
    /// </summary>
    private bool IsAdmin()
    {
        // 認証済みユーザーの login_id を取得する
        var loginId =
            User.Identity?.Name ??
            User.Claims.FirstOrDefault(c => c.Type == "login_id")?.Value;

        // 特定の login_id のみ管理者として許可する
        return loginId == ADMIN_LOGIN_ID;
    }
}
