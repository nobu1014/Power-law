using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Controllers;

[ApiController]
[Route("api/symbols")]
public class SymbolsController : ControllerBase
{
    private readonly SymbolRepository _symbolRepository;

    public SymbolsController(SymbolRepository symbolRepository)
    {
        _symbolRepository = symbolRepository;
    }

    /// <summary>
    /// 銘柄登録（カンマ区切り対応）
    /// ※ 登録のみ。Import（Python実行）は行わない
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterSymbolRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Symbols))
            return BadRequest("Symbols is required.");

        var market = (request.Market ?? "US").Trim().ToUpperInvariant();

        var symbols = request.Symbols
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpperInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();

        var registered = new List<string>();
        var skipped = new List<string>();

        foreach (var symbol in symbols)
        {
            // InsertIfNotExistsAsync は
            // true: 新規登録 / false: 既存でスキップ
            var inserted =
                await _symbolRepository.InsertIfNotExistsAsync(symbol, market, ct);

            if (inserted) registered.Add(symbol);
            else skipped.Add(symbol);
        }

        return Ok(new
        {
            requested = symbols.Count,
            registered = registered.Count,
            skipped = skipped.Count,
            registeredSymbols = registered,
            skippedSymbols = skipped,
            market
        });
    }

    /// <summary>
    /// 銘柄一覧取得
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var list = await _symbolRepository.GetAllAsync();

        var result = list.Select(s => new
        {
            id = s.Id,
            symbol = s.SymbolCode,
            market = s.Market,
            createdAt = s.CreatedAt
        });

        return Ok(result);
    }
}
