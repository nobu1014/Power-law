using Microsoft.AspNetCore.Http;

namespace StockCheck.Api.Models.Requests;

/// <summary>
/// 銘柄一括登録リクエスト
/// </summary>
public class ImportSymbolsRequest
{
    /// <summary>
    /// 銘柄CSV/TXTファイル
    /// </summary>
    public IFormFile File { get; set; } = default!;
}
