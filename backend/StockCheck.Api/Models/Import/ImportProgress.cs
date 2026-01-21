namespace StockCheck.Api.Models.Import;

/// <summary>
/// Import 処理の進捗をフロントへ通知するための DTO
/// （SSE で逐次送信される）
/// </summary>
public sealed class ImportProgress
{
    /// <summary>
    /// 現在処理中、または完了した銘柄
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 処理状態
    /// start   : 処理開始
    /// success : 正常終了
    /// failed  : エラー終了
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// エラー時のメッセージ（成功時は null）
    /// </summary>
    public string? Error { get; set; }
}
