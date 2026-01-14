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
/// ・Repository から取得した完成データを
///   画面表示用レスポンスに組み立てる
///
/// ※ 計算・JOIN・並び順は DB VIEW に委譲
/// </summary>
public class AnalysisService
{
    private readonly SymbolRepository _symbolRepository;
    private readonly WatchlistRepository _watchlistRepository;
    private readonly PriceDailyRepository _priceDailyRepository;
    private readonly EpsPriceRepository _epsPriceRepository;
    private readonly ExternalDataImporter _externalImporter;

     private readonly DrawdownRepository _drawdownRepository;

    public AnalysisService(
        SymbolRepository symbolRepository,
        WatchlistRepository watchlistRepository,
        PriceDailyRepository priceDailyRepository,
        EpsPriceRepository epsPriceRepository,
        ExternalDataImporter externalImporter,
        DrawdownRepository drawdownRepository
        )
    {
        _symbolRepository = symbolRepository;
        _watchlistRepository = watchlistRepository;
        _priceDailyRepository = priceDailyRepository;
        _epsPriceRepository = epsPriceRepository;
         _externalImporter = externalImporter; 
         _drawdownRepository = drawdownRepository;
    }

    /// <summary>
    /// 株価分析を実行する（Phase0）
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

        var symbolEntity = await _symbolRepository.GetBySymbolAsync(symbol, market)
            ?? throw new InvalidOperationException("Symbol registration failed.");

        var symbolId = symbolEntity.Id;

        // =====================================================
        // ② 外部データ取得判定
        // =====================================================

        // ---- Price ----
        var latestTradeDate =
            await _priceDailyRepository.GetLatestTradeDateAsync(symbolId);

        var latestBusinessDay = GetLatestBusinessDay(DateTime.Today);

        var needPriceImport =
            latestTradeDate == null ||
            latestTradeDate < latestBusinessDay;


        // ---- EPS ----
        var epsRange = Math.Clamp(request.EpsRange ?? 4, 4, 16);
        var epsCount = await _epsPriceRepository.CountAsync(symbolId);

        var needEpsImport = epsCount < epsRange;

        // =====================================================
        // ③ 外部データ取得（並列）
        // =====================================================
        Task? priceTask = null;
        Task? epsTask = null;

        if (needPriceImport)
            priceTask = _externalImporter.ImportPriceAsync(symbol);

        if (needEpsImport)
            epsTask = _externalImporter.ImportEpsAsync(symbol);

        // Price は必ず待つ（UX最優先）
        if (priceTask != null)
        {
            try
            {
                await priceTask;
            }
            catch (Exception ex)
            {
                // Phase0：既存DBデータで表示継続
                // TODO: ILogger 追加時に LogError
            }
        }

        // EPS は裏で処理（ログだけ拾う）
        if (epsTask != null)
        {
            _ = epsTask.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    // TODO: ILogger 追加時に LogError
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        // =====================================================
        // ④ 株価（日次）
        // =====================================================
        var today = DateTime.Today;
        var baseYears = Math.Clamp(request.BaseYears ?? 3, 1, 5);
        var priceFrom = today.AddYears(-baseYears);

        var prices = await _priceDailyRepository
            .GetByDateRangeAsync(symbolId, priceFrom, today);

        var latestPrice = await _priceDailyRepository.GetLatestAsync(symbolId);

        // =====================================================
        // ⑤ EPS（四半期）
        // =====================================================
        var epsPriceRows = await _epsPriceRepository
            .GetRecentAsync(symbolId, epsRange);

        // =====================================================
        // ⑥ 株価レスポンス
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
        // ⑦ EPS レスポンス
        // =====================================================
        var epsResponse = new EpsAnalysisResponse();

        foreach (var row in epsPriceRows)
        {
            epsResponse.Table.Add(new EpsTableRow
            {
                Period = $"{row.FiscalYear}Q{row.FiscalQuarter}",
                Value = row.Eps,
                Change = null,
                ChangeRate = null
            });

            epsResponse.EpsList.Add(new EpsPoint
            {
                Period = $"{row.FiscalYear}Q{row.FiscalQuarter}",
                Value = row.Eps
            });
        }


        AddAverage(epsPriceRows, 2, epsResponse.Averages);
        AddAverage(epsPriceRows, 3, epsResponse.Averages);
        AddAverage(epsPriceRows, 4, epsResponse.Averages);

        // =====================================================
        // ⑧ PER レスポンス
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
        // ⑨ レスポンス
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
        {
            result[key] = list.Average(p => p.ClosePrice);
        }
    }

    private static void AddAverage(
        List<EpsPriceRow> rows,
        int count,
        Dictionary<string, decimal> result)
    {
        var list = rows.Take(count).ToList();
        if (list.Any())
        {
            result[$"{count}AVG"] = list.Average(r => r.Eps);
        }
    }

    //直近営業日を求める
        private static DateTime GetLatestBusinessDay(DateTime today)
    {
        return today.DayOfWeek switch
        {
            DayOfWeek.Saturday => today.AddDays(-1), // 金曜
            DayOfWeek.Sunday   => today.AddDays(-2), // 金曜
            _                  => today.AddDays(-1), // 平日は前日
        };
    }

}
