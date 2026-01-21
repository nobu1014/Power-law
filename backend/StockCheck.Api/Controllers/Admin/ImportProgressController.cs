using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StockCheck.Api.Infrastructure;
using System.Text.Json;

namespace StockCheck.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/import/progress")]
[Authorize]
public sealed class ImportProgressController : ControllerBase
{
    private readonly ImportProgressChannel _channel;
    private readonly ILogger<ImportProgressController> _logger;

    /// <summary>
    /// Import 進捗 Channel と Logger を受け取る
    /// </summary>
    public ImportProgressController(
        ImportProgressChannel channel,
        ILogger<ImportProgressController> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    /// <summary>
    /// SSE 接続エンドポイント
    /// </summary>
    [HttpGet]
    public async Task Get(CancellationToken ct)
    {
        // SSE 用レスポンスヘッダ設定
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        _logger.LogInformation("SSE connection established");

        await foreach (var progress in _channel.ReadAllAsync(ct))
        {
            _logger.LogInformation(
                "SSE send: {Symbol} {Status}",
                progress.Symbol,
                progress.Status
            );

            var json = JsonSerializer.Serialize(progress);

            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        _logger.LogInformation("SSE connection closed");
    }
}
