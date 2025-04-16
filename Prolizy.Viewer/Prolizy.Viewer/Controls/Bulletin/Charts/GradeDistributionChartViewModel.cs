using System.Collections.Generic;
using System.Linq;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class GradeDistributionChartViewModel : ObservableObject
{
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private string _title = "Distribution des notes";
    [ObservableProperty] private bool _hasData = false;
    
    private readonly BulletinPaneViewModel _bulletinViewModel;
    
    public GradeDistributionChartViewModel(BulletinPaneViewModel bulletinViewModel)
    {
        _bulletinViewModel = bulletinViewModel;
        UpdateChart();
        
        _bulletinViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_bulletinViewModel.Resources) ||
                args.PropertyName == nameof(_bulletinViewModel.Saes))
            {
                UpdateChart();
            }
        };
    }
    
    public void UpdateChart()
    {
        if (_bulletinViewModel.Resources == null && _bulletinViewModel.Saes == null)
        {
            HasData = false;
            return;
        }
        
        var allGrades = new List<double>();
        
        // Get grades from Resources
        if (_bulletinViewModel.Resources != null)
        {
            foreach (var resource in _bulletinViewModel.Resources)
            {
                foreach (var eval in resource.Evals)
                {
                    if (double.TryParse(eval.Evaluation.Grade.Value.Replace(".", ","), out var grade))
                    {
                        allGrades.Add(grade);
                    }
                }
            }
        }
        
        // Get grades from SAEs
        if (_bulletinViewModel.Saes != null)
        {
            foreach (var sae in _bulletinViewModel.Saes)
            {
                foreach (var eval in sae.Evals)
                {
                    if (double.TryParse(eval.Evaluation.Grade.Value.Replace(".", ","), out var grade))
                    {
                        allGrades.Add(grade);
                    }
                }
            }
        }
        
        if (allGrades.Count == 0)
        {
            HasData = false;
            return;
        }
        
        HasData = true;
        
        // Group grades by range
        var gradeRanges = new[]
        {
            (Range: "0-5", Count: allGrades.Count(g => g >= 0 && g < 5), Color: ColorMatcher.Red.ToSKColor()),
            (Range: "5-10", Count: allGrades.Count(g => g >= 5 && g < 10), Color: ColorMatcher.Yellow.ToSKColor()),
            (Range: "10-15", Count: allGrades.Count(g => g >= 10 && g < 15), Color: ColorMatcher.Lime.ToSKColor()),
            (Range: "15-20", Count: allGrades.Count(g => g >= 15 && g <= 20), Color: ColorMatcher.Green.ToSKColor())
        };
        
        // Create pie series
        Series = [
            new PieSeries<int>
            {
                Values = gradeRanges.Select(r => r.Count).ToArray(),
                Name = "Distribution des notes",
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{gradeRanges[point.Index].Range}: {point.Model} ({point.StackedValue!.Share:P0})",
                Pushout = 5,
                RadialAlign = RadialAlignment.Center,
                InnerRadius = 50,
                MaxRadialColumnWidth = double.MaxValue
            }
        ];
    }
}