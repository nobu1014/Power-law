namespace StockCheck.Api.Models.Responses;

/// <summary>
/// 株価分析レスポンス
/// 株価 / EPS / PER の分析結果をすべて含む
/// </summary>
public class AnalysisResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public PriceAnalysisResponse Price { get; set; } = new();
    public EpsAnalysisResponse Eps { get; set; } = new();
    public PerAnalysisResponse Per { get; set; } = new();
}

/// <summary>株価分析レスポンス</summary>
public class PriceAnalysisResponse
{
    public decimal? CurrentPrice { get; set; }
    public List<PricePoint> Prices { get; set; } = new();
    public Dictionary<string, decimal> FixedAverages { get; set; } = new();
    public Dictionary<string, decimal> CustomAverages { get; set; } = new();
}

/// <summary>株価ポイント（グラフ用）</summary>
public class PricePoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
}

/// <summary>EPS分析レスポンス</summary>
public class EpsAnalysisResponse
{
    public List<EpsPoint> EpsList { get; set; } = new();
    public List<EpsTableRow> Table { get; set; } = new();
    public Dictionary<string, decimal> Averages { get; set; } = new();
}

/// <summary>EPSポイント（グラフ用）</summary>
public class EpsPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

/// <summary>EPS表行</summary>
public class EpsTableRow
{
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangeRate { get; set; }
}

/// <summary>PER分析レスポンス</summary>
public class PerAnalysisResponse
{
    public List<PerPoint> PerList { get; set; } = new();
    public List<PerTableRow> Table { get; set; } = new();
}

/// <summary>PERポイント</summary>
public class PerPoint
{
    public string Period { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}

/// <summary>PER表行</summary>
public class PerTableRow
{
    public string Period { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}