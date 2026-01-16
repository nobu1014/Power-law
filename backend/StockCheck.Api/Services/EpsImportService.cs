using System.Text.Json;
using StockCheck.Api.Infrastructure;
using StockCheck.Api.Models.Entities;
using StockCheck.Api.Models.Import;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// EPS（四半期）データを外部APIから取得しDBへ同期するService
/// </summary>
public sealed class EpsImportService
{
    private const string MARKET_US = "US";
    private const int MAX_KEEP = 16;

    private readonly SymbolRepository _symbolRepo;
    private readonly EpsImportRepository _epsRepo;
    private readonly PythonProcessRunner _python;

    /// <summary>
    /// Importに必要なRepositoryとPython実行部品を受け取る
    /// </summary>
    public EpsImportService(
        SymbolRepository symbolRepo,
        EpsImportRepository epsRepo,
        PythonProcessRunner python)
    {
        _symbolRepo = symbolRepo;
        _epsRepo = epsRepo;
        _python = python;
    }

    /// <summary>
    /// 指定銘柄のEPSをDBと差分同期し、処理件数サマリを返す
    /// </summary>
    public async Task<EpsImportSummary> ImportAsync(
        string symbol,
        ImportExecutionContext context,
        CancellationToken ct)
    {
        var summary = new EpsImportSummary();

        // 表記揺れ防止
        symbol = symbol.Trim().ToUpperInvariant();

        // symbols から取得する（存在保証は上位層の責務）
        var entity =
            await _symbolRepo.GetBySymbolAsync(symbol, MARKET_US)
            ?? throw new InvalidOperationException($"Symbol not found: {symbol}");

        var symbolId = entity.Id;

        // DBに登録されている最新のEPS期を取得する
        // ※ API取得可否の判断は実行文脈に依存するため、
        //    NightBatch の場合のみここで抑止判定を行う
        var latestPeriod =
            await _epsRepo.GetLatestPeriodAsync(symbolId);

        // 夜間バッチ実行時のみ、
        // すでに最新期が登録済みであれば API を呼ばない
        if (context == ImportExecutionContext.NightBatch &&
            latestPeriod.HasValue &&
            IsLatestFiscalPeriod(latestPeriod.Value))
        {
            return summary;
        }

        // Python を実行し、EPSデータをJSONで取得する
        var json = await _python.RunAsync(
            "src/scripts/import_eps_quarterly.py",
            $"--symbol {symbol}",
            ct);

        var doc = JsonDocument.Parse(json);

        var epsArray = doc.RootElement
            .GetProperty("eps")
            .EnumerateArray()
            .ToList();

        // 外部APIから取得した件数を記録する
        summary = summary with { Fetched = epsArray.Count };

        // 既存の決算期を取得する
        var existingPeriods =
            await _epsRepo.GetExistingPeriodsAsync(symbolId);

        foreach (var row in epsArray)
        {
            var year = row.GetProperty("fiscalYear").GetInt32();
            var quarter = row.GetProperty("fiscalQuarter").GetInt32();

            if (existingPeriods.Contains((year, quarter)))
            {
                summary = summary with { Skipped = summary.Skipped + 1 };
                continue;
            }

            await _epsRepo.InsertAsync(new Earnings
            {
                SymbolId = symbolId,
                FiscalYear = year,
                FiscalQuarter = quarter,
                Eps = row.GetProperty("eps").GetDecimal(),
                ReportDate = DateTime.Parse(
                    row.GetProperty("reportDate").GetString()!)
            });

            summary = summary with { Inserted = summary.Inserted + 1 };
        }

        return summary;
    }

    /// <summary>
    /// 指定された fiscalPeriod が「現在時点で最新」とみなせるかを判定する
    /// ※ 厳密な決算日は考慮せず、Phase0として簡易判定とする
    /// </summary>
    private static bool IsLatestFiscalPeriod(
        (int FiscalYear, int FiscalQuarter) period)
    {
        var now = DateTime.Today;

        // 現在の想定 fiscal year
        var currentYear = now.Year;

        // 現在月から想定される最新四半期（簡易）
        var currentQuarter = (now.Month - 1) / 3 + 1;

        // DBの期が現在想定期以上であれば最新とみなす
        return period.FiscalYear > currentYear
            || (period.FiscalYear == currentYear &&
                period.FiscalQuarter >= currentQuarter);
    }

}
