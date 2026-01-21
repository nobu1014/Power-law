using System.Threading.Channels;
using StockCheck.Api.Models.Import;

namespace StockCheck.Api.Infrastructure;

/// <summary>
/// Import 進捗を Controller ↔ Service 間で共有するための Channel
///
/// ・管理画面 Import 専用
/// ・常に最新の進捗を流す
/// </summary>
public sealed class ImportProgressChannel
{
    // 単一リーダー・単一ライターで十分
    private readonly Channel<ImportProgress> _channel =
        Channel.CreateUnbounded<ImportProgress>();

    /// <summary>
    /// 書き込み側（ImportService から呼ばれる）
    /// </summary>
    public async ValueTask WriteAsync(ImportProgress progress)
    {
        await _channel.Writer.WriteAsync(progress);
    }

    /// <summary>
    /// 読み取り側（SSE Controller から使われる）
    /// </summary>
    public IAsyncEnumerable<ImportProgress> ReadAllAsync(
        CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
