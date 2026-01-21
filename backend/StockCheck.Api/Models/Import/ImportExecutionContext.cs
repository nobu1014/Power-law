namespace StockCheck.Api.Models.Import;

/// <summary>
/// Import処理が「どの文脈で実行されているか」を表す
///
/// 文脈ごとに
/// ・API取得の抑止条件
/// ・エラー時の扱い
/// ・再実行ポリシー
/// を変えるために使用する
/// </summary>
public enum ImportExecutionContext
{
    /// <summary>
    /// ユーザー操作による分析実行
    ///
    /// ・必要なデータが無ければ取得を試みる
    /// ・頻繁に呼ばれる可能性がある
    /// </summary>
    Analysis,

    /// <summary>
    /// 管理画面からの手動実行
    ///
    /// ・管理者が明示的に実行する
    /// ・NightBatch の抑止条件は適用しない
    /// ・エラーは UI に返す前提
    /// </summary>
    Manual,

    /// <summary>
    /// 夜間バッチによる全銘柄Import
    ///
    /// ・API制限を最優先で考慮
    /// ・最新データが揃っていれば取得しない
    /// ・1日1回の実行が前提
    /// </summary>
    NightBatch
}
