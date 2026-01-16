namespace StockCheck.Api.Models.Import;

/// <summary>
/// Import処理がどの文脈で実行されているかを表す
/// </summary>
public enum ImportExecutionContext
{
    /// <summary>
    /// ユーザー操作による分析実行
    /// </summary>
    Analysis,

    /// <summary>
    /// 夜間バッチによる全銘柄Import
    /// </summary>
    NightBatch
}
