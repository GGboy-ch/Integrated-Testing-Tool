using Microsoft.Extensions.Options;

namespace PackageTransferService;

public sealed class PackageTransferWorker : BackgroundService
{
    private readonly FileTransferProcessor _processor;
    private readonly ILogger<PackageTransferWorker> _logger;
    private readonly TimeSpan _scanInterval;

    public PackageTransferWorker(
        FileTransferProcessor processor,
        IOptions<PackageTransferOptions> options,
        ILogger<PackageTransferWorker> logger)
    {
        _processor = processor;
        _logger = logger;
        _scanInterval = TimeSpan.FromSeconds(options.Value.ScanIntervalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("安装包转移服务已启动，扫描间隔：{ScanInterval}", _scanInterval);

        using PeriodicTimer timer = new(_scanInterval);
        do
        {
            try
            {
                await _processor.ProcessOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "本次安装包扫描处理失败，下次扫描将重试。");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
