using LogProfiler.ViewModels;
using System.Windows;

namespace LogProfiler.Views
{
    public partial class SimulationSettingsView : Window
    {
        internal SimulationSettingsView(SimulationSettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.Saved += () => DialogResult = true;
        }
    }
}
