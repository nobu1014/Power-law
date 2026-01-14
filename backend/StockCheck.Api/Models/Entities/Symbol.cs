namespace StockCheck.Api.Models.Entities;

/// <summary>
/// 銘柄マスタ（symbols テーブル）
/// </summary>
public class Symbol
{
    public int Id { get; set; }

    /// <summary>銘柄コード / ティッカー</summary>
    public string SymbolCode { get; set; } = null!;

    /// <summary>市場（US / JP など）</summary>
    public string Market { get; set; } = "US";

    public DateTime CreatedAt { get; set; }
}
