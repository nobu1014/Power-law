namespace StockCheck.Api.Models.Analysis;

/// <summary>
/// 分析対象期間
/// </summary>
public class AnalysisRangeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}
