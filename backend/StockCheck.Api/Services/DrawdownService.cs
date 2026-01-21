using StockCheck.Api.Models.Import;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Repositories;


namespace StockCheck.Api.Services;

/// <summary>
/// 株価下落チェック Service
/// </summary>
public class DrawdownService
{
    private readonly DrawdownRepository _repository;
    private readonly ImportService _importService;

    public DrawdownService(
        DrawdownRepository repository,
        ImportService importService)
    {
        _repository = repository;
        _importService = importService;
    }

    /// <summary>
    /// 下落率一覧（DB参照のみ・高速）
    /// </summary>
    public async Task<IReadOnlyList<DrawdownListItemDto>> GetDrawdownListAsync(
        int periodMonths,
        string sortOrder = "desc")
    {
        return await _repository.GetDrawdownListAsync(periodMonths, sortOrder);
    }

    /// <summary>
    /// 下落チェック用：最新データ取得（重い・明示実行）
    /// </summary>
    public async Task RefreshLatestDataAsync(CancellationToken ct)
    {
        // ウォッチリスト由来の銘柄一覧
        var symbols = await _repository.GetTargetSymbolsAsync();

        foreach (var s in symbols)
        {
            if (ct.IsCancellationRequested)
                break;

            // ★ AnalysisService は使わない
            // ★ ImportService を直接使う
            await _importService.ImportBySymbolAsync(
                s.Symbol,
                ImportExecutionContext.Manual,
                ct
            );
        }
    }
}
