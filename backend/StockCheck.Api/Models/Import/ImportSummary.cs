namespace StockCheck.Api.Models.Import;

/// <summary>
/// 銘柄単位で Price / EPS Import の結果をまとめたDTO
///
/// 管理画面では「成功・失敗が混在する」前提のため、
/// Error プロパティを持たせている。
/// </summary>
public sealed class ImportSummary
{
    /// <summary>
    /// 対象銘柄コード（例: AAPL）
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 日次株価Import結果
    ///
    /// ※ Import に失敗した場合は未使用
    /// </summary>
    public PriceImportSummary Price { get; set; } = new();

    /// <summary>
    /// EPS（四半期）Import結果
    ///
    /// ※ Import に失敗した場合は未使用
    /// </summary>
    public EpsImportSummary Eps { get; set; } = new();

    /// <summary>
    /// Import に失敗した場合のエラーメッセージ
    ///
    /// ・成功時：null
    /// ・失敗時：例外メッセージ
    ///
    /// 管理画面で
    /// 「この銘柄だけ失敗した」
    /// と分かるようにするための項目。
    /// </summary>
    public string? Error { get; set; }
}
