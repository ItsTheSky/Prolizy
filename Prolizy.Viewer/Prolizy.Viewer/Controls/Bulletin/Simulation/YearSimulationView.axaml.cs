using Avalonia.Controls;
using Prolizy.Viewer.ViewModels.Bulletin.Simulation;

namespace Prolizy.Viewer.Controls.Bulletin.Simulation;

public partial class YearSimulationView : UserControl
{
    public YearSimulationView()
    {
        InitializeComponent();
    }
    
    public YearSimulationView(YearSimulationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}