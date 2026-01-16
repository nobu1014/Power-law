namespace StockCheck.Api.Models.Import;

/// <summary>
/// 日次株価Importの結果サマリ
/// </summary>
public sealed record PriceImportSummary
{
    /// <summary>APIから取得した件数</summary>
    public int Fetched { get; init; }

    /// <summary>新規INSERTした件数</summary>
    public int Inserted { get; init; }

    /// <summary>既存のためスキップした件数</summary>
    public int Skipped { get; init; }

    /// <summary>欠損補完した件数</summary>
    public int Filled { get; init; }

    /// <summary>古いデータを削除した件数</summary>
    public int Deleted { get; init; }
}
