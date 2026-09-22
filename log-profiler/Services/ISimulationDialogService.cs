namespace LogProfiler.Services
{
    internal interface ISimulationDialogService
    {
        SimulationOptions? ShowSettings(SimulationOptions? current);
        void ShowMessage(string message);
    }
}
