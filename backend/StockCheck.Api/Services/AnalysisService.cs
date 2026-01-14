using StockCheck.Api.Models.Entities;
using StockCheck.Api.Models.Requests;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 株価分析（Phase0）サービス
///
/// 【責務】
/// ・入力正規化
/// ・watchlist / symbols 登録保証
/// ・DB から取得したデータを
///   画面表示用レスポンスに組み立てる
///
/// ※ 外部API取得は ImportService に完全委譲
/// </summary>
public class AnalysisService
{
    private readonly SymbolRepository _symbolRepository;
    private readonly WatchlistRepository _watchlistRepository;
    private readonly PriceDailyRepository _priceDailyRepository;
    private readonly EpsPriceRepository _epsPriceRepository;

    public AnalysisService(
        SymbolRepository symbolRepository,
        WatchlistRepository watchlistRepository,
        PriceDailyRepository priceDailyRepository,
        EpsPriceRepository epsPriceRepository)
    {
        _symbolRepository = symbolRepository;
        _watchlistRepository = watchlistRepository;
        _priceDailyRepository = priceDailyRepository;
        _epsPriceRepository = epsPriceRepository;
    }

    /// <summary>
    /// 株価分析を実行する（DB参照のみ）
    /// </summary>
    public async Task<AnalysisResponse> AnalyzeAsync(AnalysisRequest request)
    {
        // =====================================================
        // ① 銘柄正規化 & DB登録保証
        // =====================================================
        var symbol = request.Symbol.Trim().ToUpper();
        var market = request.Market.Trim().ToUpper();

        await _watchlistRepository.AddAsync(symbol, market);
        await _symbolRepository.InsertIfNotExistsAsync(symbol, market);

        var symbolEntity =
            await _symbolRepository.GetBySymbolAsync(symbol, market)
            ?? throw new InvalidOperationException("Symbol registration failed.");

        var symbolId = symbolEntity.Id;

        // =====================================================
        // ② 株価（日次）
        // =====================================================
        var today = DateTime.Today;
        var baseYears = Math.Clamp(request.BaseYears ?? 3, 1, 5);
        var priceFrom = today.AddYears(-baseYears);

        var prices = await _priceDailyRepository
            .GetByDateRangeAsync(symbolId, priceFrom, today);

        var latestPrice = await _priceDailyRepository.GetLatestAsync(symbolId);

        // =====================================================
        // ③ EPS（四半期）
        // =====================================================
        var epsRange = Math.Clamp(request.EpsRange ?? 4, 4, 16);

        var epsPriceRows = await _epsPriceRepository
            .GetRecentAsync(symbolId, epsRange);

        // =====================================================
        // ④ 株価レスポンス
        // =====================================================
        var priceResponse = new PriceAnalysisResponse
        {
            CurrentPrice = latestPrice?.ClosePrice,
            Prices = prices.Select(p => new PricePoint
            {
                Date = p.TradeDate,
                Value = p.ClosePrice
            }).ToList()
        };

        AddAverage(prices, 30, "1M", priceResponse.FixedAverages);
        AddAverage(prices, 60, "2M", priceResponse.FixedAverages);
        AddAverage(prices, 180, "6M", priceResponse.FixedAverages);

        for (int year = 1; year <= baseYears; year++)
            AddAverage(prices, year * 365, $"{year}Y", priceResponse.FixedAverages);

        var ytd = prices.Where(p => p.TradeDate.Year == today.Year).ToList();
        if (ytd.Any())
            priceResponse.FixedAverages["YTD"] = ytd.Average(p => p.ClosePrice);

        // =====================================================
        // ⑤ EPS レスポンス
        // =====================================================
        var epsResponse = new EpsAnalysisResponse();

        foreach (var row in epsPriceRows)
        {
            var period = $"{row.FiscalYear}Q{row.FiscalQuarter}";

            epsResponse.Table.Add(new EpsTableRow
            {
                Period = period,
                Value = row.Eps,
                Change = null,
                ChangeRate = null
            });

            epsResponse.EpsList.Add(new EpsPoint
            {
                Period = period,
                Value = row.Eps
            });
        }

        AddAverage(epsPriceRows, 2, epsResponse.Averages);
        AddAverage(epsPriceRows, 3, epsResponse.Averages);
        AddAverage(epsPriceRows, 4, epsResponse.Averages);

        // =====================================================
        // ⑥ PER レスポンス
        // =====================================================
        var perResponse = new PerAnalysisResponse();

        foreach (var row in epsResponse.Table)
        {
            decimal? per =
                row.Value != 0 && latestPrice != null
                    ? latestPrice.ClosePrice / row.Value
                    : null;

            perResponse.Table.Add(new PerTableRow
            {
                Period = row.Period,
                Value = per
            });

            perResponse.PerList.Add(new PerPoint
            {
                Period = row.Period,
                Value = per
            });
        }

        // =====================================================
        // ⑦ レスポンス
        // =====================================================
        return new AnalysisResponse
        {
            Symbol = symbol,
            Market = market,
            Price = priceResponse,
            Eps = epsResponse,
            Per = perResponse
        };
    }

    // =====================================================
    // 共通処理
    // =====================================================

    private static void AddAverage(
        List<PriceDaily> prices,
        int days,
        string key,
        Dictionary<string, decimal> result)
    {
        var from = DateTime.Today.AddDays(-days);
        var list = prices.Where(p => p.TradeDate >= from).ToList();
        if (list.Any())
            result[key] = list.Average(p => p.ClosePrice);
    }

    private static void AddAverage(
        List<EpsPriceRow> rows,
        int count,
        Dictionary<string, decimal> result)
    {
        var list = rows.Take(count).ToList();
        if (list.Any())
            result[$"{count}AVG"] = list.Average(r => r.Eps);
    }
}
