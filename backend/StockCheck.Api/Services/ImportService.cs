using StockCheck.Api.Models.Import;
using StockCheck.Api.Repositories;
using StockCheck.Api.Infrastructure;
namespace StockCheck.Api.Services;

/// <summary>
/// 各種Import処理を統括し、銘柄単位・全銘柄単位で実行する司令塔Service
/// </summary>
public sealed class ImportService
{
    private readonly SymbolRepository _symbolRepository;
    private readonly PriceImportService _priceService;
    private readonly EpsImportService _epsService;
    private readonly ImportProgressChannel _progressChannel;

    /// <summary>
    /// Importに必要なRepositoryと各Import Serviceを受け取る
    /// </summary>
    public ImportService(
        SymbolRepository symbolRepository,
        PriceImportService priceService,
        EpsImportService epsService,
        ImportProgressChannel progressChannel)
    {
        _symbolRepository = symbolRepository;
        _priceService = priceService;
        _epsService = epsService;
        _progressChannel = progressChannel;
    }

    /// <summary>
    /// 指定銘柄について、株価 → EPS の順でImport処理を実行し、統合サマリを返す
    /// </summary>
    public async Task<ImportSummary> ImportBySymbolAsync(
        string symbol,
        ImportExecutionContext context,
        CancellationToken ct)
    {
        // 表記揺れ防止のため正規化する
        symbol = symbol.Trim().ToUpperInvariant();

        // 日次株価のImportを実行する
        var priceSummary =
            await _priceService.ImportAsync(symbol, context, ct);

        // EPS（四半期）のImportを実行する
        var epsSummary =
            await _epsService.ImportAsync(symbol, context, ct);

        // 各Import結果をまとめた統合サマリを返す
        return new ImportSummary
        {
            Symbol = symbol,
            Price = priceSummary,
            Eps = epsSummary
        };
    }

     /// <summary>
    /// 【管理画面専用】
    /// 全銘柄 Import（進捗通知あり）
    /// </summary>
    public async Task<List<ImportSummary>> ImportAllForAdminAsync(
        CancellationToken ct)
    {
        var results = new List<ImportSummary>();
        var symbols = await _symbolRepository.GetAllAsync();

        foreach (var s in symbols)
        {
            if (ct.IsCancellationRequested)
                break;

            // ===== 進捗：開始 =====
            await _progressChannel.WriteAsync(new ImportProgress
            {
                Symbol = s.SymbolCode,
                Status = "start"
            });

            try
            {
                var price =
                    await _priceService.ImportAsync(
                        s.SymbolCode,
                        ImportExecutionContext.Manual,
                        ct);

                var eps =
                    await _epsService.ImportAsync(
                        s.SymbolCode,
                        ImportExecutionContext.Manual,
                        ct);

                results.Add(new ImportSummary
                {
                    Symbol = s.SymbolCode,
                    Price = price,
                    Eps = eps
                });

                // ===== 進捗：成功 =====
                await _progressChannel.WriteAsync(new ImportProgress
                {
                    Symbol = s.SymbolCode,
                    Status = "success"
                });
            }
            catch (Exception ex)
            {
                results.Add(new ImportSummary
                {
                    Symbol = s.SymbolCode,
                    Error = ex.Message
                });

                // ===== 進捗：失敗 =====
                await _progressChannel.WriteAsync(new ImportProgress
                {
                    Symbol = s.SymbolCode,
                    Status = "failed",
                    Error = ex.Message
                });
            }

            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }

        return results;
    }

    /// <summary>
    /// 【管理画面専用】
    /// 指定銘柄について Import を実行する。
    ///
    /// ■ 特徴
    /// ・例外を投げない
    /// ・失敗理由は ImportSummary.Error に格納
    /// ・ImportExecutionContext.Manual を使用
    /// </summary>
    public async Task<ImportSummary> ImportBySymbolForAdminAsync(
        string symbol,
        CancellationToken ct)
    {
        // 表記揺れ防止
        symbol = symbol.Trim().ToUpperInvariant();

        try
        {
            // 管理画面実行なので Manual 文脈を使用
            var priceSummary =
                await _priceService.ImportAsync(
                    symbol,
                    ImportExecutionContext.Manual,
                    ct);

            var epsSummary =
                await _epsService.ImportAsync(
                    symbol,
                    ImportExecutionContext.Manual,
                    ct);

            return new ImportSummary
            {
                Symbol = symbol,
                Price = priceSummary,
                Eps = epsSummary,
                Error = null
            };
        }
        catch (Exception ex)
        {
            // ★重要★
            // 管理画面では例外をそのまま返さず、
            // Error として UI に返す
            return new ImportSummary
            {
                Symbol = symbol,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 【NightBatch 専用】
    /// 管理画面と同等の取得仕様で全銘柄 Import を実行する
    /// ・必ず API を呼ぶ
    /// ・失敗しても次の銘柄へ進む
    /// ・進捗はログ出力のみ
    /// </summary>
    public async Task ImportAllForNightBatchAsync(
        CancellationToken ct)
    {
        var symbols = await _symbolRepository.GetAllAsync();

        foreach (var s in symbols)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                // ★ 管理画面と同じ Manual 文脈を使う
                var price =
                    await _priceService.ImportAsync(
                        s.SymbolCode,
                        ImportExecutionContext.Manual,
                        ct);

                var eps =
                    await _epsService.ImportAsync(
                        s.SymbolCode,
                        ImportExecutionContext.Manual,
                        ct);

                // NightBatch は UI が無いのでログで可視化
                Console.WriteLine(
                    $"[NightBatch] {s.SymbolCode} " +
                    $"Price(I:{price.Inserted},S:{price.Skipped}) " +
                    $"EPS(I:{eps.Inserted},S:{eps.Skipped})");
            }
            catch (Exception ex)
            {
                // ★ 失敗しても NightBatch は止めない
                Console.WriteLine(
                    $"[NightBatch][ERROR] {s.SymbolCode} {ex.Message}");
            }

            // API制限対策
            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }
    }

}
