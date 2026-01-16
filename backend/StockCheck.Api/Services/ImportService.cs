using StockCheck.Api.Models.Import;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 各種Import処理を統括し、銘柄単位・全銘柄単位で実行する司令塔Service
/// </summary>
public sealed class ImportService
{
    private readonly SymbolRepository _symbolRepository;
    private readonly PriceImportService _priceService;
    private readonly EpsImportService _epsService;

    /// <summary>
    /// Importに必要なRepositoryと各Import Serviceを受け取る
    /// </summary>
    public ImportService(
        SymbolRepository symbolRepository,
        PriceImportService priceService,
        EpsImportService epsService)
    {
        _symbolRepository = symbolRepository;
        _priceService = priceService;
        _epsService = epsService;
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
    /// 登録済みの全銘柄に対してImport処理を順次実行し、サマリ一覧を返す
    /// </summary>
    public async Task<List<ImportSummary>> ImportAllAsync(
        CancellationToken ct)
    {
        var results = new List<ImportSummary>();

        // symbols テーブルに登録されている全銘柄を取得する
        var symbols = await _symbolRepository.GetAllAsync();

        foreach (var s in symbols)
        {
            if (ct.IsCancellationRequested)
                break;

            // 夜間バッチ文脈で日次株価をImportする
            var priceSummary =
                await _priceService.ImportAsync(
                    s.SymbolCode,
                    ImportExecutionContext.NightBatch,
                    ct);

            // 夜間バッチ文脈でEPSをImportする
            var epsSummary =
                await _epsService.ImportAsync(
                    s.SymbolCode,
                    ImportExecutionContext.NightBatch,
                    ct);

            results.Add(new ImportSummary
            {
                Symbol = s.SymbolCode,
                Price = priceSummary,
                Eps = epsSummary
            });

            // Alpha Vantage API のレート制限対策として待機する
            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }

        return results;
    }
}
