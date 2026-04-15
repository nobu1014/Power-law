using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 指値計算 Service
/// ウォッチリストの全銘柄について指値を計算する
/// </summary>
public sealed class LimitPriceService
{
    private readonly WatchlistRepository _watchlistRepository;
    private readonly SymbolRepository _symbolRepository;
    private readonly PriceDailyRepository _priceDailyRepository;
    private readonly LimitPriceSettingsRepository _settingsRepository;

    public LimitPriceService(
        WatchlistRepository watchlistRepository,
        SymbolRepository symbolRepository,
        PriceDailyRepository priceDailyRepository,
        LimitPriceSettingsRepository settingsRepository)
    {
        _watchlistRepository = watchlistRepository;
        _symbolRepository = symbolRepository;
        _priceDailyRepository = priceDailyRepository;
        _settingsRepository = settingsRepository;
    }

    /// <summary>
    /// ウォッチリストの全銘柄について指値を計算して返す
    /// </summary>
    public async Task<LimitPriceResultResponse> CalcAsync(int userId, int pattern)
    {
        // ① 指値設定を取得する（未登録ならエラー）
        var settings = await _settingsRepository.GetAsync(userId, pattern);
        if (settings == null)
        {
            return new LimitPriceResultResponse
            {
                IsSettingsRequired = true,
                Items = new List<LimitPriceItemDto>()
            };
        }

        // ② ウォッチリストの全銘柄を取得する
        var watchlist = await _watchlistRepository.GetAllAsync(userId);

        var today = DateTime.Today;
        var items = new List<LimitPriceItemDto>();

        foreach (var w in watchlist)
        {
            // ③ 銘柄エンティティを取得する
            var symbolEntity = await _symbolRepository.GetBySymbolAsync(w.Symbol, w.Market);
            if (symbolEntity == null) continue;

            var symbolId = symbolEntity.Id;

            // ④ パターンに応じた期間で株価データを取得する
            // 最大6ヶ月分あれば全パターン対応できる
            var prices = await _priceDailyRepository
                .GetByDateRangeAsync(symbolId, today.AddMonths(-6), today);

            if (prices.Count == 0) continue;

            // ⑤ パターン別に最高値・平均値を計算する
            decimal peakA, peakB;
            decimal avgA, avgB, avgC;

            if (pattern == 1)
            {
                // パターン①：最高値1ヶ月・3ヶ月 / 平均1・2・6ヶ月
                peakA = GetPeak(prices, today, 1);
                peakB = GetPeak(prices, today, 3);
                avgA = GetAverage(prices, today, 1);
                avgB = GetAverage(prices, today, 2);
                avgC = GetAverage(prices, today, 6);
            }
            else
            {
                // パターン②：最高値3ヶ月・6ヶ月 / 平均2・4・6ヶ月
                peakA = GetPeak(prices, today, 3);
                peakB = GetPeak(prices, today, 6);
                avgA = GetAverage(prices, today, 2);
                avgB = GetAverage(prices, today, 4);
                avgC = GetAverage(prices, today, 6);
            }

            // ⑥ 指値を計算する
            // 最高値軸指値 = (最高値A + 最高値B) / 2 × (100 - peak_drop_rate) / 100
            var peakBase = (peakA + peakB) / 2;
            var peakLimitPrice = peakBase * (100 - settings.PeakDropRate) / 100;

            // 平均軸指値 = (平均A + 平均B + 平均C) / 3 × (100 - avg_drop_rate) / 100
            var avgBase = (avgA + avgB + avgC) / 3;
            var avgLimitPrice = avgBase * (100 - settings.AvgDropRate) / 100;

            // 最終指値 = (最高値軸指値 + 平均軸指値) / 2
            var finalLimitPrice = (peakLimitPrice + avgLimitPrice) / 2;

            // 最新株価を取得する
            var latestPrice = prices.LastOrDefault()?.ClosePrice;

            items.Add(new LimitPriceItemDto
            {
                Symbol = w.Symbol,
                Market = w.Market,
                LatestPrice = latestPrice,
                PeakA = peakA,
                PeakB = peakB,
                AvgA = avgA,
                AvgB = avgB,
                AvgC = avgC,
                PeakLimitPrice = Math.Round(peakLimitPrice, 2),
                AvgLimitPrice = Math.Round(avgLimitPrice, 2),
                FinalLimitPrice = Math.Round(finalLimitPrice, 2)
            });
        }

        return new LimitPriceResultResponse
        {
            IsSettingsRequired = false,
            Pattern = pattern,
            PeakDropRate = settings.PeakDropRate,
            AvgDropRate = settings.AvgDropRate,
            Items = items
        };
    }

    /// <summary>
    /// 指定期間内の最高値を取得する
    /// </summary>
    private static decimal GetPeak(
        List<StockCheck.Api.Models.Entities.PriceDaily> prices,
        DateTime baseDate,
        int months)
    {
        var from = baseDate.AddMonths(-months);
        var targets = prices.Where(p => p.TradeDate >= from && p.TradeDate <= baseDate).ToList();
        return targets.Any() ? targets.Max(p => p.ClosePrice) : 0;
    }

    /// <summary>
    /// 指定期間内の平均値を取得する
    /// </summary>
    private static decimal GetAverage(
        List<StockCheck.Api.Models.Entities.PriceDaily> prices,
        DateTime baseDate,
        int months)
    {
        var from = baseDate.AddMonths(-months);
        var targets = prices.Where(p => p.TradeDate >= from && p.TradeDate <= baseDate).ToList();
        return targets.Any() ? targets.Average(p => p.ClosePrice) : 0;
    }
}

/// <summary>
/// 指値計算レスポンス
/// </summary>
public sealed class LimitPriceResultResponse
{
    /// <summary>
    /// 設定未登録フラグ（trueの場合は設定が必要）
    /// </summary>
    public bool IsSettingsRequired { get; set; }
    public int Pattern { get; set; }
    public decimal PeakDropRate { get; set; }
    public decimal AvgDropRate { get; set; }
    public List<LimitPriceItemDto> Items { get; set; } = new();
}

/// <summary>
/// 銘柄ごとの指値計算結果
/// </summary>
public sealed class LimitPriceItemDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public decimal? LatestPrice { get; set; }   // 最新株価
    public decimal PeakA { get; set; }           // 最高値A（短い期間）
    public decimal PeakB { get; set; }           // 最高値B（長い期間）
    public decimal AvgA { get; set; }            // 平均値A
    public decimal AvgB { get; set; }            // 平均値B
    public decimal AvgC { get; set; }            // 平均値C
    public decimal PeakLimitPrice { get; set; }  // 最高値軸の指値
    public decimal AvgLimitPrice { get; set; }   // 平均軸の指値
    public decimal FinalLimitPrice { get; set; } // 最終指値
}