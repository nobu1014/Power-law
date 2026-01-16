namespace StockCheck.Api.Models.Import;

/// <summary>
/// 銘柄単位で Price / EPS Import の結果をまとめたDTO
/// </summary>
public sealed class ImportSummary
{
    /// <summary>対象銘柄</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>日次株価Import結果</summary>
    public PriceImportSummary Price { get; set; } = new();

    /// <summary>EPS（四半期）Import結果</summary>
    public EpsImportSummary Eps { get; set; } = new();
}
