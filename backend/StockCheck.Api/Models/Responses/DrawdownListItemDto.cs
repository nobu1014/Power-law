namespace StockCheck.Api.Models.Responses;

public sealed class DrawdownListItemDto
{
    public string Symbol { get; set; } = default!;
    public decimal PeakPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DrawdownRate { get; set; }
}
