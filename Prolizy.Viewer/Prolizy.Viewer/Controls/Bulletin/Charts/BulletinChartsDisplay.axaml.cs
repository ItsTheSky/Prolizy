using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class BulletinChartsDisplay : UserControl
{
    public BulletinChartsDisplay()
    {
        InitializeComponent();
    }
    
    public BulletinChartsViewModel ViewModel => (BulletinChartsViewModel)DataContext!;
}

public class BulletinChartsViewModel(BulletinPaneViewModel bulletinViewModel)
{
    public AbsenceAnalysisChartViewModel AbsenceChart { get; } = new(bulletinViewModel);
    public UePerformanceChartViewModel UeChart { get; } = new(bulletinViewModel);
    public GradeEvolutionChartViewModel EvolutionChart { get; } = new(bulletinViewModel);
    public ResourceSaeComparisonChartViewModel ComparisonChart { get; } = new(bulletinViewModel);
    public GradeDistributionChartViewModel DistributionChart { get; } = new(bulletinViewModel);

    public RelayCommand UpdateAllCommand => new(() =>
    {
        AbsenceChart.UpdateChart();
        
        UeChart.UpdateChart();
        EvolutionChart.UpdateChart();
        ComparisonChart.UpdateChart();
        DistributionChart.UpdateChart();
    });
}