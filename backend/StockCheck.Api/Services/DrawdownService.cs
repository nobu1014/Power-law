using StockCheck.Api.Models.Import;
using StockCheck.Api.Models.Responses;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 株価下落チェック Service
/// ログインユーザーのウォッチリスト銘柄を対象に下落率を計算する
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
    /// ログインユーザーの下落率一覧を取得する（DB参照のみ・高速）
    /// </summary>
    public async Task<IReadOnlyList<DrawdownListItemDto>> GetDrawdownListAsync(
        int userId,
        int periodMonths,
        string sortOrder = "desc")
    {
        // ログインユーザーのウォッチリスト銘柄のみを対象にする
        return await _repository.GetDrawdownListAsync(userId, periodMonths, sortOrder);
    }

    /// <summary>
    /// ログインユーザーのウォッチリスト銘柄の最新データを取得する
    /// （重い処理・明示実行）
    /// </summary>
    public async Task RefreshLatestDataAsync(int userId, CancellationToken ct)
    {
        // ログインユーザーのウォッチリスト銘柄一覧を取得する
        var symbols = await _repository.GetTargetSymbolsAsync(userId);

        foreach (var s in symbols)
        {
            if (ct.IsCancellationRequested)
                break;

            // ImportService を直接使って最新データを取得する
            await _importService.ImportBySymbolAsync(
                s.Symbol,
                ImportExecutionContext.Manual,
                ct
            );
        }
    }
}