namespace StockCheck.Api.Models.Requests;

/// <summary>
/// 銘柄登録リクエスト（画面・API共通）
/// </summary>
public class RegisterSymbolRequest
{
    /// <summary>
    /// カンマ区切りで複数指定可能（例: AAPL,MSFT,GOOGL）
    /// </summary>
    public string Symbols { get; set; } = string.Empty;

    /// <summary>市場（省略時 US）</summary>
    public string Market { get; set; } = "US";
}
