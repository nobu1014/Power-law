namespace StockCheck.Api.Models.Requests;

/// <summary>
/// 株価分析リクエスト
/// ・銘柄は watchlist から選択、またはテキスト入力
/// ・watchlist / symbols に存在しない場合は自動登録される
/// </summary>
public class AnalysisRequest
{
    /// <summary>
    /// 銘柄コード（例: AAPL）
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 市場（US 固定）
    /// </summary>
    public string Market { get; set; } = "US";

    /// <summary>
    /// ログイン中ユーザーのID
    /// Controller から設定される（フロントからは送らない）
    /// watchlist への自動登録に使用する
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 株価の基準期間（年）1〜5年
    /// </summary>
    public int? BaseYears { get; set; }

    /// <summary>
    /// EPS / PER の分析レンジ（決算期数）4 / 8 / 12 / 16
    /// </summary>
    public int? EpsRange { get; set; }
}