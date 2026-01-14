namespace StockCheck.Api.Models.Entities;

/// <summary>
/// EPS（四半期）エンティティ
/// DB: power_test.eps_quarterly
/// </summary>
public class Earnings
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
    /// 決算年（例: 2024）
    /// </summary>
    public int FiscalYear { get; set; }

    /// <summary>
    /// 決算四半期（1〜4）
    /// </summary>
    public int FiscalQuarter { get; set; }

    /// <summary>
    /// EPS（実績）
    /// </summary>
    public decimal Eps { get; set; }

    /// <summary>
    /// 決算発表日
    /// </summary>
    public DateTime? ReportDate { get; set; }

    /// <summary>
    /// 登録日時
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
