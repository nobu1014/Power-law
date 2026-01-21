using StockCheck.Api.Models.Responses;
using StockCheck.Api.Repositories;
using StockCheck.Api.Models.Import;

namespace StockCheck.Api.Services;

/// <summary>
/// 株価下落チェック Service
/// </summary>
public class DrawdownService
{
    private readonly DrawdownRepository _repository;
    private readonly AnalysisService _analysisService;

    public DrawdownService(
        DrawdownRepository repository,
        AnalysisService analysisService)
    {
        _repository = repository;
        _analysisService = analysisService;
    }

    /// <summary>
    /// 【軽量】
    /// 下落率一覧を取得（DB参照のみ）
    /// </summary>
    public async Task<IReadOnlyList<DrawdownListItemDto>> GetDrawdownListAsync(
        int periodMonths,
        string sortOrder = "desc")
    {
        return await _repository.GetDrawdownListAsync(
            periodMonths,
            sortOrder
        );
    }

    /// <summary>
    /// 【重い・明示実行】
    /// 下落チェック対象銘柄の最新データを取得する
    /// </summary>
    public async Task RefreshLatestDataAsync(CancellationToken ct)
    {
        var symbols = await _repository.GetTargetSymbolsAsync();

        using var semaphore = new SemaphoreSlim(3);

        var tasks = symbols.Select(async s =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await _analysisService.EnsureLatestDataAsync(
                    s.Symbol,
                    s.Market,
                    ImportExecutionContext.Analysis,
                    ct);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
