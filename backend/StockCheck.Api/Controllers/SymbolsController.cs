using Microsoft.AspNetCore.Mvc;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Repositories;



namespace StockCheck.Api.Controllers;

[ApiController]
[Route("api/symbols")]
public class SymbolsController : ControllerBase
{
    private readonly SymbolRepository _repo;

    public SymbolsController(SymbolRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// 銘柄登録（カンマ区切り対応）
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterSymbolRequest request)
    {
        var symbols = request.Symbols
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToUpper())
            .Distinct();

        foreach (var symbol in symbols)
        {
            await _repo.InsertIfNotExistsAsync(symbol, request.Market);
        }

        return Ok(new
        {
            registered = symbols.Count(),
            symbols
        });
    }

    /// <summary>
    /// 銘柄一覧取得
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync();
        return Ok(list);
    }

    /// <summary>
    /// 銘柄一括登録（ファイルアップロード）
    /// CSV / TXT（カンマ区切り）対応
    /// すべて米国株（US）として登録する
    /// </summary>
    // 今回のフロントでは使用しない
    // [HttpPost("import")]

    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportFromFile([FromForm] ImportSymbolsRequest request)
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
            await _repo.InsertIfNotExistsAsync(symbol, "US");
        }

        return Ok(new
        {
            Count = symbols.Count,
            Symbols = symbols
        });
    }

}
