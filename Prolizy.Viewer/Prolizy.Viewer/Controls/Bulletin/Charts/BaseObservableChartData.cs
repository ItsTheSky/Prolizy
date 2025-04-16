using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class BaseObservableChartData : ObservableObject
{
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private ICartesianAxis[] _xAxes = [];
    [ObservableProperty] private ICartesianAxis[] _yAxes = [];
    
    private bool _hasData;

    public bool HasData
    {
        get => _hasData;
        set
        {
            if (SetProperty(ref _hasData, value))
                OnPropertyChanged();
            if (!value) PopulateSampleData();
        }
    }

    public void PopulateSampleData()
    {
        // This method is a placeholder for populating sample data.
        // The actual chart is hidden, but it must have data anyway
        // else it'll crash the app.
        var sampleData = new[]
        {
            new ColumnSeries<double>
            {
                Values = [1, 2, 3, 4, 5],
                Name = "Sample Data",
                Fill = new SolidColorPaint(SKColors.Blue)
            }
        };
        
        Series = sampleData;
        XAxes =
        [
            new Axis
            {
                Labels = ["Jan", "Feb", "Mar", "Apr", "May"],
                Name = "Months",
                NamePaint = new SolidColorPaint(SKColors.White),
                LabelsPaint = new SolidColorPaint(SKColors.White)
            }
        ];
        YAxes =
        [
            new Axis
            {
                Name = "Values",
                NamePaint = new SolidColorPaint(SKColors.White),
                LabelsPaint = new SolidColorPaint(SKColors.White)
            }
        ];
    }
}