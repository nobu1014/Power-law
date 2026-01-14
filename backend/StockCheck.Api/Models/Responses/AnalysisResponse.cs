namespace StockCheck.Api.Models.Responses;

/// <summary>
/// 株価分析（Phase0）レスポンス
/// ・株価 / EPS / PER の分析結果をすべて含む
/// ・表示切替はフロントエンド側で行う
/// </summary>
public class AnalysisResponse
{
    /// <summary>
    /// 銘柄コード
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 市場
    /// </summary>
    public string Market { get; set; } = string.Empty;

    /// <summary>
    /// 株価分析結果
    /// </summary>
    public PriceAnalysisResponse Price { get; set; } = new();

    /// <summary>
    /// EPS分析結果
    /// </summary>
    public EpsAnalysisResponse Eps { get; set; } = new();

    /// <summary>
    /// PER分析結果
    /// </summary>
    public PerAnalysisResponse Per { get; set; } = new();
}

/// <summary>
/// 株価分析レスポンス
/// </summary>
public class PriceAnalysisResponse
{
    /// <summary>
    /// 現在値（直近株価）
    /// </summary>
    public decimal? CurrentPrice { get; set; }

    /// <summary>
    /// 日次株価（折れ線グラフ用）
    /// </summary>
    public List<PricePoint> Prices { get; set; } = new();

    /// <summary>
    /// 固定平均値
    /// </summary>
    public Dictionary<string, decimal> FixedAverages { get; set; } = new();

    /// <summary>
    /// 任意条件平均値
    /// </summary>
    public Dictionary<string, decimal> CustomAverages { get; set; } = new();
}

/// <summary>
/// 株価ポイント（グラフ用）
/// </summary>
public class PricePoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

/// <summary>
/// EPS分析レスポンス
/// </summary>
public class EpsAnalysisResponse
{
    /// <summary>
    /// EPS推移（折れ線グラフ用）
    /// </summary>
    public List<EpsPoint> EpsList { get; set; } = new();

    /// <summary>
    /// EPS表（値・変動・変動率）
    /// </summary>
    public List<EpsTableRow> Table { get; set; } = new();

    /// <summary>
    /// EPS平均値
    /// </summary>
    public Dictionary<string, decimal> Averages { get; set; } = new();
}

/// <summary>
/// EPSポイント（グラフ用）
/// </summary>
public class EpsPoint
{
    public string Period { get; set; } = string.Empty; // 例: 2024Q3
    public decimal Value { get; set; }
}

/// <summary>
/// EPS表行
/// </summary>
public class EpsTableRow
{
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangeRate { get; set; }
}

/// <summary>
/// PER分析レスポンス
/// </summary>
public class PerAnalysisResponse
{
    /// <summary>
    /// PER推移（折れ線グラフ用）
    /// </summary>
    public List<PerPoint> PerList { get; set; } = new();

    /// <summary>
    /// PER表
    /// </summary>
    public List<PerTableRow> Table { get; set; } = new();
}

/// <summary>
/// PERポイント
/// </summary>
public class PerPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}

/// <summary>
/// PER表行
/// </summary>
public class PerTableRow
{
    public string Period { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}


