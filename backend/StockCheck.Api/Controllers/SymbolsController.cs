using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Repositories;
using StockCheck.Api.Services;
using Microsoft.AspNetCore.Authorization;
namespace StockCheck.Api.Controllers;

[ApiController]
[Route("api/symbols")]
public class SymbolsController : ControllerBase
{
    private readonly SymbolRepository _symbolRepository;
    private readonly ImportService _importService;

    public SymbolsController(
        SymbolRepository symbolRepository,
        ImportService importService)
    {
        _symbolRepository = symbolRepository;
        _importService = importService;
    }

    /// <summary>
    /// 銘柄登録（カンマ区切り対応）
    /// 登録後、即フル Import（登録銘柄のみ）
    /// </summary>
    [HttpPost("register")]
     [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterSymbolRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Symbols))
        {
            return BadRequest("Symbols is required.");
        }

        var symbols = request.Symbols
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpper())
            .Distinct()
            .ToList();

        foreach (var symbol in symbols)
        {
            // ===== 銘柄登録 =====
            await _symbolRepository.InsertIfNotExistsAsync(
                symbol,
                request.Market ?? "US"
            );

            // ===== 即フル Import（この銘柄のみ）=====
            await _importService.ImportBySymbolAsync(
                symbol: symbol,
                maxPriceYears: 5,
                maxEpsQuarters: 16,
                ct: ct
            );
        }

        return Ok(new
        {
            registered = symbols.Count,
            symbols
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
            symbol = s.SymbolCode,   // ← ここが重要
            market = s.Market,
            createdAt = s.CreatedAt
        });

        return Ok(result);
    }


    /// <summary>
    /// 銘柄一括登録（CSV / TXT）
    /// ※ 現状フロント未使用
    /// </summary>
    [HttpPost("import")]
     [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFromFile(
        [FromForm] ImportSymbolsRequest request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        string content;
        using (var reader = new StreamReader(request.File.OpenReadStream()))
        {
            content = await reader.ReadToEndAsync();
        }

        var symbols = content
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpper())
            .Distinct()
            .ToList();

        foreach (var symbol in symbols)
        {
            await _symbolRepository.InsertIfNotExistsAsync(symbol, "US");

            await _importService.ImportBySymbolAsync(
                symbol: symbol,
                maxPriceYears: 5,
                maxEpsQuarters: 16,
                ct: ct
            );
        }

        return Ok(new
        {
            count = symbols.Count,
            symbols
        });
    }
}
