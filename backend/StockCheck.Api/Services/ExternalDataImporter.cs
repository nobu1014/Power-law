using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StockCheck.Api.Services;

public class ExternalDataImporter
{
    private readonly string _pythonProjectRoot;
    private readonly string _pythonExePath;
    private readonly ILogger<ExternalDataImporter> _logger;

    public ExternalDataImporter(
        IConfiguration config,
        ILogger<ExternalDataImporter> logger)
    {
        _pythonProjectRoot =
            config["Python:ProjectRoot"]
            ?? throw new InvalidOperationException("Python:ProjectRoot not configured");

        _pythonExePath =
            config["Python:PythonExe"]
            ?? throw new InvalidOperationException("Python:PythonExe not configured");

        _logger = logger;
    }

    public Task ImportPriceAsync(string symbol, DateTime fromDate)
        => RunPythonAsync(
            "src/scripts/import_price_daily.py",
            $"{symbol} {fromDate:yyyy-MM-dd}"
        );

    public Task ImportEpsAsync(string symbol, int maxCount)
        => RunPythonAsync(
            "src/scripts/import_eps_quarterly.py",
            $"{symbol} {maxCount}"
        );

    private async Task RunPythonAsync(string scriptRelativePath, string args)
    {
        var scriptPath = Path.Combine(_pythonProjectRoot, scriptRelativePath);

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExePath,                 // ← /usr/bin/python3
            Arguments = $"\"{scriptPath}\" {args}",
            WorkingDirectory = _pythonProjectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["PYTHONPATH"] =
        Path.Combine(_pythonProjectRoot, "src");

        _logger.LogInformation("Python start: {Script} {Args}", scriptRelativePath, args);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start python process");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stdout))
            _logger.LogInformation(stdout);

        if (!string.IsNullOrWhiteSpace(stderr))
            _logger.LogWarning(stderr);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Python failed ({process.ExitCode})");
    }
}
