namespace StockCheck.Api.Models.Requests;

/// <summary>
/// ウォッチリスト操作用リクエスト
/// 追加・削除で共通利用する
/// </summary>
public class WatchlistRequest
{
    /// <summary>
    /// 銘柄コード（例: AAPL）
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 市場（例: US）
    /// 未指定時は US として扱う
    /// </summary>
    public string Market { get; set; } = "US";
}
