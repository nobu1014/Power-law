namespace StockCheck.Api.Models.Analysis;

/// <summary>
/// 時系列データ（株価 / EPS 共通）
///
/// Date:
/// - 株価: yyyy-MM-dd
/// - EPS  : 2024Q3 など
/// → フロント描画用の文字列として扱う
/// </summary>
public class TimeSeriesPointDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
