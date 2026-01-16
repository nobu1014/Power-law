namespace StockCheck.Api.Models.Import;

/// <summary>
/// EPS（四半期）Importの結果サマリ
/// </summary>
public sealed record EpsImportSummary
{
    /// <summary>APIから取得した件数</summary>
    public int Fetched { get; init; }

    /// <summary>新規INSERTした件数</summary>
    public int Inserted { get; init; }

    /// <summary>既存のためスキップした件数</summary>
    public int Skipped { get; init; }
}
