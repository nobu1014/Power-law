namespace StockCheck.Api.Models.Entities;

/// <summary>
/// ウォッチリストエンティティ
/// DB: power_test.watchlist と 1対1
/// </summary>
public class Watchlist
{
    /// <summary>
    /// 主キー
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 銘柄コード（例: AAPL）
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 市場（例: US）
    /// </summary>
    public string Market { get; set; } = "US";

    /// <summary>
    /// 登録日時
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
