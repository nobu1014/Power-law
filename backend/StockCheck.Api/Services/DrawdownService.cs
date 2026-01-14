using StockCheck.Api.Models.Responses;
using StockCheck.Api.Repositories;

namespace StockCheck.Api.Services;

/// <summary>
/// 株価下落チェック Service（一覧専用）
/// </summary>
public class DrawdownService
{
    private readonly DrawdownRepository _repository;

    public DrawdownService(DrawdownRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// ウォッチ銘柄の下落率一覧を取得
    /// （銘柄ごとに1行、下落率順）
    /// </summary>
    /// <param name="periodMonths">直近◯ヶ月（例：3）</param>
    /// <param name="sortOrder">asc / desc（省略時 desc）</param>
    public async Task<IReadOnlyList<DrawdownListItemDto>> GetDrawdownListAsync(
        int periodMonths,
        string sortOrder = "desc")
    {
        return await _repository.GetDrawdownListAsync(
            periodMonths,
            sortOrder
        );
    }
}
