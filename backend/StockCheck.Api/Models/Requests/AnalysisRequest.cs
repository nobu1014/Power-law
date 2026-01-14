namespace StockCheck.Api.Models.Requests;

/// <summary>
/// 株価分析（Phase0）リクエスト
/// 
/// ・銘柄は watchlist から選択、またはテキスト入力
/// ・watchlist / symbols に存在しない場合は自動登録される
/// ・株価 / EPS / PER はすべて毎回計算される
/// ・表示切替はフロントエンド側で行う
/// </summary>
public class AnalysisRequest
{
    /// <summary>
    /// 銘柄コード（例: AAPL）
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 市場（Phase0では US 固定）
    /// </summary>
    public string Market { get; set; } = "US";

    // ===== 株価（基準期間方式） =====

    /// <summary>
    /// 株価の基準期間（年）
    /// 1〜5 年を想定
    /// </summary>
    public int? BaseYears { get; set; }

    // ===== EPS / PER（分析レンジ方式） =====

    /// <summary>
    /// EPS / PER の分析レンジ（決算期数）
    /// 
    /// 例:
    /// 4  = 直近 4 期
    /// 8  = 直近 8 期
    /// 12 = 直近 12 期
    /// 16 = 直近 16 期
    /// </summary>
    public int? EpsRange { get; set; }
}
