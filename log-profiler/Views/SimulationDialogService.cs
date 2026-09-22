using LogProfiler.Services;
using LogProfiler.ViewModels;
using System.Windows;

namespace LogProfiler.Views
{
    internal sealed class SimulationDialogService(Window owner) : ISimulationDialogService
    {
        public SimulationOptions? ShowSettings(SimulationOptions? current)
        {
            var viewModel = new SimulationSettingsViewModel(current, SimulationSettingsStore.Save);
            var window = new SimulationSettingsView(viewModel) { Owner = owner };
            return window.ShowDialog() == true ? viewModel.SavedOptions : null;
        }

        public void ShowMessage(string message) => MessageBox.Show(owner, message, "模拟发送");
    }
}
