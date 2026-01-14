using Microsoft.Extensions.Logging;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 外部データ Import 業務ロジック
///
/// 【責務】
/// ・差分判定
/// ・取得範囲決定
/// ・Python Import 呼び出し
///
/// ※ Controller / BackgroundService 共通
/// </summary>
public class ImportService
{
    private const string DEFAULT_MARKET = "US";

    private readonly SymbolRepository _symbolRepository;
    private readonly PriceDailyRepository _priceDailyRepository;
    private readonly ExternalDataImporter _externalImporter;
    private readonly ILogger<ImportService> _logger;

    public ImportService(
        SymbolRepository symbolRepository,
        PriceDailyRepository priceDailyRepository,
        ExternalDataImporter externalImporter,
        ILogger<ImportService> logger)
    {
        _symbolRepository = symbolRepository;
        _priceDailyRepository = priceDailyRepository;
        _externalImporter = externalImporter;
        _logger = logger;
    }

    // =====================================================
    // 公開 API
    // =====================================================

    /// <summary>
    /// 全銘柄 Import（BackgroundService 用）
    /// </summary>
    public async Task ImportAllAsync(
        int maxPriceYears,
        int maxEpsQuarters,
        CancellationToken ct)
    {
        var symbols = await _symbolRepository.GetAllAsync();

        foreach (var s in symbols)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                await ImportBySymbolAsync(
                    s.SymbolCode,
                    maxPriceYears,
                    maxEpsQuarters,
                    ct
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Import failed: {Symbol}",
                    s.SymbolCode
                );
            }

            // ★ AlphaVantage レート制限対策（最低限）
            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }
    }

    /// <summary>
    /// 単一銘柄 Import（画面登録 / 手動実行）
    /// </summary>
    public async Task ImportBySymbolAsync(
        string symbol,
        int maxPriceYears,
        int maxEpsQuarters,
        CancellationToken ct)
    {
        symbol = symbol.Trim().ToUpper();

        _logger.LogInformation("Import start: {Symbol}", symbol);

        await ImportPriceAsync(symbol, maxPriceYears, ct);
        await ImportEpsAsync(symbol, maxEpsQuarters, ct);

        _logger.LogInformation("Import completed: {Symbol}", symbol);
    }

    // =====================================================
    // 内部処理
    // =====================================================

    /// <summary>
    /// 株価（日次）
    /// ・DBにある最新日以降のみ取得
    /// ・最大取得年数を考慮
    /// </summary>
    private async Task ImportPriceAsync(
        string symbol,
        int maxYears,
        CancellationToken ct)
    {
        var entity =
            await _symbolRepository.GetBySymbolAsync(symbol, DEFAULT_MARKET)
            ?? throw new InvalidOperationException($"Symbol not found: {symbol}");

        var latestDate =
            await _priceDailyRepository.GetLatestTradeDateAsync(entity.Id);

        var maxFromDate = DateTime.Today.AddYears(-maxYears);

        var fromDate =
            latestDate != null
                ? latestDate.Value.AddDays(1)
                : maxFromDate;

        // 最大取得範囲より古くならないよう制御
        if (fromDate < maxFromDate)
            fromDate = maxFromDate;

        if (fromDate > DateTime.Today)
        {
            _logger.LogInformation(
                "Price already up-to-date: {Symbol}",
                symbol
            );
            return;
        }

        _logger.LogInformation(
            "Import price: {Symbol} from {From}",
            symbol,
            fromDate.ToString("yyyy-MM-dd")
        );

        await _externalImporter.ImportPriceAsync(symbol, fromDate);
    }

    /// <summary>
    /// EPS（四半期）
    /// ・決算日はズレるため毎回取得
    /// ・Python 側で maxQuarters 制御
    /// </summary>
    private async Task ImportEpsAsync(
        string symbol,
        int maxQuarters,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Import EPS: {Symbol} (max {Count})",
            symbol,
            maxQuarters
        );

        await _externalImporter.ImportEpsAsync(symbol, maxQuarters);
    }
}
