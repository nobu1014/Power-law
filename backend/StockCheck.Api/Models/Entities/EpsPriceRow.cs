public class EpsPriceRow
{
    public int FiscalYear { get; set; }
    public int FiscalQuarter { get; set; }
    public decimal Eps { get; set; }
    public decimal? ClosePrice { get; set; }

    // ★ ここを DateOnly? にする
    public DateOnly? TradeDate { get; set; }

    public decimal? Per { get; set; }
}
