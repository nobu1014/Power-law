using System.Text.Json;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;
using StockCheck.Api.Models.Import;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 日次株価を外部APIから取得しDBへ同期するService
/// </summary>
public sealed class PriceImportService
{
    private const string MARKET_US = "US";
    private const int MAX_KEEP_YEARS = 5;

    private readonly SymbolRepository _symbolRepo;
    private readonly PriceImportRepository _priceRepo;
    private readonly PythonProcessRunner _python;

    /// <summary>
    /// Importに必要なRepositoryとPython実行部品を受け取る
    /// </summary>
    public PriceImportService(
        SymbolRepository symbolRepo,
        PriceImportRepository priceRepo,
        PythonProcessRunner python)
    {
        _symbolRepo = symbolRepo;
        _priceRepo = priceRepo;
        _python = python;
    }


    /// <summary>
    /// 指定銘柄の日次株価をDBと差分同期し、処理件数サマリを返す
    /// </summary>
    public async Task<PriceImportSummary> ImportAsync(
        string symbol,
        ImportExecutionContext context,
        CancellationToken ct)
    {

        // Import結果を集計するためのサマリを初期化する
        var summary = new PriceImportSummary();

        // 表記揺れ防止
        symbol = symbol.Trim().ToUpperInvariant();

        // symbols から取得する（存在保証は上位層の責務）
        var entity =
            await _symbolRepo.GetBySymbolAsync(symbol, MARKET_US)
            ?? throw new InvalidOperationException($"Symbol not found: {symbol}");

        var symbolId = entity.Id;


        // DBに登録済みの最新取引日を取得する
        // ※ API取得可否の判断は呼び出し元の文脈に依存するため、
        //    NightBatch の場合のみここで抑止判定を行う
        var latestDate =
            await _priceRepo.GetLatestTradeDateAsync(symbolId);

        // 夜間バッチ実行時のみ、前営業日分が揃っていればAPI取得を行わない
        if (context == ImportExecutionContext.NightBatch &&
            latestDate.HasValue &&
            latestDate.Value.Date >= GetPreviousBusinessDay(DateTime.Today))
        {
            return summary;
        }

        // 初回かどうかで API の取得範囲を切り替える
        var outputSize =
            latestDate == null ? "full" : "compact";

        // -------------------------------
        // Python に渡す取得期間を算出する
        // -------------------------------
        var today = DateTime.Today;

        DateTime fromDate =
            latestDate == null
                // 初回Import：過去20年分
                ? today.AddYears(-20)
                // 差分Import：前回最新日の翌日
                : latestDate.Value.AddDays(1);

        DateTime toDate = today;

        // Python を実行し、株価データをJSONで取得する
        var json = await _python.RunAsync(
            "src/scripts/import_price_daily.py",
            $"--symbol {symbol} " +
            $"--from {fromDate:yyyy-MM-dd} " +
            $"--to {toDate:yyyy-MM-dd} " +
            $"--outputsize {outputSize}",
            ct);


        var doc = JsonDocument.Parse(json);

        var prices = doc.RootElement
            .GetProperty("prices")
            .EnumerateArray()
            .ToList();

        // 外部APIから取得した件数を記録する
        summary = summary with { Fetched = prices.Count };

        // 既存の取引日一覧を取得する
        var existingDates =
            await _priceRepo.GetExistingDatesAsync(symbolId);

        // Python結果を1日ずつ処理する
        foreach (var row in prices)
        {
            var date = DateTime.Parse(
                row.GetProperty("date").GetString()!);

            // 既に登録済みの日付はスキップする
            if (existingDates.Contains(date.Date))
            {
                summary = summary with { Skipped = summary.Skipped + 1 };
                continue;
            }

            // 新規の日次株価のみINSERTする
            await _priceRepo.InsertAsync(new PriceDaily
            {
                SymbolId = symbolId,
                TradeDate = date.Date,
                ClosePrice = row.GetProperty("close").GetDecimal()
            });

            summary = summary with { Inserted = summary.Inserted + 1 };
        }

        summary = summary with
        {
            // 最新5年分だけ残し、古いデータを削除する
            Deleted = await _priceRepo.DeleteOldAsync(symbolId, MAX_KEEP_YEARS),
            // 取引日欠損を補完し、補完件数を集計する
            Filled = await FillMissingBusinessDaysAsync(symbolId, ct)
        };

        return summary;
    }

    /// <summary>
    /// 取引日が連続していない箇所を検出し、欠損日を直前営業日の終値で補完する
    /// </summary>
    private async Task<int> FillMissingBusinessDaysAsync(
        int symbolId,
        CancellationToken ct)
    {
        var filled = 0;

        // 欠損補完の対象期間（直近5年）
        var fromDate = DateTime.Today.AddYears(-MAX_KEEP_YEARS);

        // DBに登録されている取引日と終値の一覧を取得する
        var list =
            await _priceRepo.GetDatePriceListAsync(symbolId, fromDate);

        // データが少なすぎる場合は補完しない
        if (list.Count < 2)
            return 0;

        // 前日との差分を見て欠損日を検出する
        for (int i = 1; i < list.Count; i++)
        {
            var prev = list[i - 1];
            var curr = list[i];

            var expectedNext = prev.Date.AddDays(1);

            // 連続している場合は何もしない
            if (expectedNext == curr.Date)
                continue;

            // gap が 2日以上ある場合、その間を補完対象とする
            var fillDate = expectedNext;

            while (fillDate < curr.Date)
            {
                // 土日は取引が無い前提なので補完しない
                if (fillDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    fillDate = fillDate.AddDays(1);
                    continue;
                }

                // 直前営業日の終値で補完する
                await _priceRepo.InsertAsync(new PriceDaily
                {
                    SymbolId = symbolId,
                    TradeDate = fillDate,
                    ClosePrice = prev.ClosePrice
                });

                filled++;
                fillDate = fillDate.AddDays(1);
            }
        }

        return filled;
    }

    /// <summary>
    /// 土日を考慮して前営業日を算出する
    /// （祝日は考慮しない）
    /// </summary>
    private static DateTime GetPreviousBusinessDay(DateTime today)
    {
        return today.DayOfWeek switch
        {
            DayOfWeek.Monday => today.AddDays(-3),
            DayOfWeek.Sunday => today.AddDays(-2),
            DayOfWeek.Saturday => today.AddDays(-1),
            _ => today.AddDays(-1)
        };
    }

}
