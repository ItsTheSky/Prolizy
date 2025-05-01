using System.Collections.Generic;
using System.Linq;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class UePerformanceChartViewModel : BaseObservableChartData
{

    [ObservableProperty] private string _title = "Performance par UE";
    
    private readonly BulletinPaneViewModel _bulletinViewModel;
    
    public UePerformanceChartViewModel(BulletinPaneViewModel bulletinViewModel)
    {
        _bulletinViewModel = bulletinViewModel;
        UpdateChart();
        
        _bulletinViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_bulletinViewModel.Units))
            {
                UpdateChart();
            }
        };
    }
    
    public void UpdateChart()
    {
        if (_bulletinViewModel.Units == null || _bulletinViewModel.Units.Count == 0)
        {
            HasData = false;
            return;
        }

        HasData = true;
        var units = _bulletinViewModel.Units.ToList();
        var labels = units.Select(u => u.Title).ToArray();
        var studentValues = new List<double>();
        var averageValues = new List<double>();
        
        foreach (var unit in units)
        {
            if (double.TryParse(unit.TeachingUnit.Average.Value.Replace(".", ","), out var studentValue))
                studentValues.Add(studentValue);
            else
                studentValues.Add(0);
            
            if (double.TryParse(unit.TeachingUnit.Average.Average.Replace(".", ","), out var averageValue))
                averageValues.Add(averageValue);
            else
                averageValues.Add(0);
        }
        
        // Configure X and Y axes
        XAxes = [
            new Axis
            {
                Labels = labels,
                LabelsRotation = -20,
                LabelsAlignment = Align.Middle,
                Padding = new Padding(-15, 0, 0, 0),
                TextSize = 14,
                ForceStepToMin = true
            }
        ];
        
        YAxes = [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 20
            }
        ];
        
        // Configure the series
        Series = [
            new ColumnSeries<double>
            {
                Name = "Mes notes",
                Values = studentValues,
                
                Stroke = new SolidColorPaint(App.GetAccentColor()) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(App.GetAccentColor(true)),
                
                Padding = 5,
                DataLabelsPosition = DataLabelsPosition.Top
            },
            new ColumnSeries<double>
            {
                Name = "Moyenne de promotion",
                Values = averageValues,
                
                Fill = new SolidColorPaint(ColorMatcher.Green.ToSKColor().LittleTransparent()),
                Stroke = new SolidColorPaint(ColorMatcher.Green.ToSKColor()) { StrokeThickness = 2 },
                
                Padding = 5,
                DataLabelsPosition = DataLabelsPosition.Top
            }
        ];
    }
}