namespace StockCheck.Api.Models.Analysis;

/// <summary>
/// 分析指標（Phase0）
///
/// ※ Phase0 では「変動率」のみを扱う
/// ※ PER / CAGR などは Phase1 以降
/// </summary>
public class AnalysisMetricsDto
{
    /// <summary>1か月変動率（%）</summary>
    public decimal OneMonth { get; set; }

    /// <summary>3か月変動率（%）</summary>
    public decimal ThreeMonths { get; set; }

    /// <summary>6か月変動率（%）</summary>
    public decimal SixMonths { get; set; }

    /// <summary>1年変動率（%）</summary>
    public decimal OneYear { get; set; }
}
