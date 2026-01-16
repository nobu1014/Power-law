namespace StockCheck.Api.Models.Analysis;

/// <summary>
/// 株価分析 API レスポンス（Phase0）
///
/// フロントはこの構造を前提に描画する
/// </summary>
public class AnalysisResultDto
{
    public string Symbol { get; set; } = string.Empty;

    public AnalysisRangeDto Range { get; set; } = new();

    public List<TimeSeriesPointDto> PriceSeries { get; set; } = new();

    public List<TimeSeriesPointDto> EpsSeries { get; set; } = new();

    /// <summary>
    /// 分析指標（null 不可）
    /// </summary>
    public AnalysisMetricsDto Metrics { get; set; } = new();
}
