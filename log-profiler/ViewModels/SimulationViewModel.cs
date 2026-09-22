using CommunityToolkit.Mvvm.Input;
using LogProfiler.Services;

namespace LogProfiler.ViewModels
{
    public sealed class SimulationViewModel : NotifyBase
    {
        private readonly Func<IReadOnlyList<SimulationEntry>> getEntries;
        private readonly ISimulationDialogService dialog;
        private SimulationOptions? options;
        private CancellationTokenSource? cancellation;
        private bool isStopping;

        internal SimulationViewModel(Func<IReadOnlyList<SimulationEntry>> getEntries, ISimulationDialogService dialog)
        {
            this.getEntries = getEntries;
            this.dialog = dialog;
            try { options = SimulationSettingsStore.Load(); }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"读取模拟发送配置失败：{ex}"); }
        }

        public bool IsConfigured => options != null;
        public bool IsRunning => cancellation != null;
        public string StartStopText => IsRunning ? "停止" : "开始";

        private RelayCommand? configureCommand;
        public RelayCommand ConfigureCommand => configureCommand ??= new RelayCommand(Configure, () => !IsRunning);

        private RelayCommand? startStopCommand;
        public RelayCommand StartStopCommand => startStopCommand ??= new RelayCommand(Toggle,
            () => IsConfigured && !isStopping);

        private void Configure()
        {
            SimulationOptions? saved = dialog.ShowSettings(options);
            if (saved == null) return;
            options = saved;
            MyPropertyChanged(nameof(IsConfigured));
            StartStopCommand.NotifyCanExecuteChanged();
        }

        private void Toggle()
        {
            if (cancellation != null)
            {
                isStopping = true;
                StartStopCommand.NotifyCanExecuteChanged();
                cancellation.Cancel();
                return;
            }

            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            if (options == null) return;
            IReadOnlyList<SimulationEntry> entries;
            try
            {
                entries = getEntries();
                if (entries.Count == 0)
                {
                    dialog.ShowMessage("当前查询结果中没有所选设备的 Receive Debug 日志。");
                    return;
                }
            }
            catch (Exception ex)
            {
                dialog.ShowMessage(ex.Message);
                return;
            }

            var source = new CancellationTokenSource();
            cancellation = source;
            NotifyStateChanged();
            try
            {
                await SimulationReplayService.ReplayAsync(options, entries, source.Token);
            }
            catch (OperationCanceledException) when (source.IsCancellationRequested) { }
            catch (Exception) when (source.IsCancellationRequested) { }
            catch (Exception ex)
            {
                dialog.ShowMessage($"模拟发送失败：{ex.Message}");
            }
            finally
            {
                cancellation = null;
                isStopping = false;
                source.Dispose();
                NotifyStateChanged();
            }
        }

        public void Stop() => cancellation?.Cancel();

        private void NotifyStateChanged()
        {
            MyPropertyChanged(nameof(IsRunning));
            MyPropertyChanged(nameof(StartStopText));
            ConfigureCommand.NotifyCanExecuteChanged();
            StartStopCommand.NotifyCanExecuteChanged();
        }
    }
}
