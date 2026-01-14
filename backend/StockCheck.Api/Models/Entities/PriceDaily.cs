namespace StockCheck.Api.Models.Entities;

/// <summary>
/// 日次株価エンティティ
/// DB: power_test.price_daily
/// </summary>
public class PriceDaily
{
    /// <summary>
    /// 主キー
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 銘柄ID（symbols.id）
    /// </summary>
    public int SymbolId { get; set; }

    /// <summary>
    /// 取引日
    /// </summary>
    public DateTime TradeDate { get; set; }

    /// <summary>
    /// 終値
    /// </summary>
    public decimal ClosePrice { get; set; }

    /// <summary>
    /// 登録日時
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
