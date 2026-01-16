using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StockCheck.Api.Infrastructure;

/// <summary>
/// Pythonスクリプトを実行し、標準出力（JSON想定）を文字列で返すための共通ランナー
///
/// 【設計方針】
/// ・OS差異（Windows / Linux）は設定ファイルで吸収する
/// ・Python実行パスはハードコードしない
/// ・C#は「司令塔」、Pythonは「取得ロジック専任」
/// ・失敗は exit code で厳密に判定する
/// </summary>
public sealed class PythonProcessRunner
{
    private readonly string _projectRoot;
    private readonly string _pythonExe;
    private readonly string _pythonSrcPath;
    private readonly ILogger<PythonProcessRunner> _logger;

    /// <summary>
    /// Python実行に必要なパス情報を設定ファイルから読み込む
    /// </summary>
    public PythonProcessRunner(
        IConfiguration config,
        ILogger<PythonProcessRunner> logger)
    {
        _projectRoot =
            config["Python:ProjectRoot"]
            ?? throw new InvalidOperationException("Python:ProjectRoot not configured");

        _pythonExe =
            config["Python:PythonExe"]
            ?? throw new InvalidOperationException("Python:PythonExe not configured");

        // Python側の import 解決用（src を PYTHONPATH に通す）
        _pythonSrcPath = Path.Combine(_projectRoot, "src");

        _logger = logger;
    }

    /// <summary>
    /// 指定されたPythonスクリプトを実行し、標準出力を返す
    /// </summary>
    public async Task<string> RunAsync(
        string scriptRelativePath,
        string arguments,
        CancellationToken ct)
    {
        // 実行するPythonスクリプトのフルパス
        var normalizedRelativePath =
            scriptRelativePath.Replace('/', Path.DirectorySeparatorChar);

        var scriptPath = Path.Combine(_projectRoot, normalizedRelativePath);


        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                $"Python script not found: {scriptPath}");
        }

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExe,
            Arguments = $"\"{scriptPath}\" {arguments}",
            WorkingDirectory = _projectRoot,

            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Python側が powerlaw.settings 等を import できるようにする
        psi.Environment["PYTHONPATH"] = _pythonSrcPath;

        _logger.LogInformation(
            "Python start: {PythonExe} {Script} {Args}",
            _pythonExe,
            scriptRelativePath,
            arguments
        );

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start python process");

        // 標準出力・標準エラーを非同期で取得
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        // Python側の警告やAPI制限メッセージはログに残す
        if (!string.IsNullOrWhiteSpace(stderr))
        {
            _logger.LogWarning(
                "Python stderr: {Message}",
                stderr.Trim());
        }

        // exit code ≠ 0 は業務的失敗とみなす
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Python failed (exit={process.ExitCode})");
        }

        return stdout;
    }
}
