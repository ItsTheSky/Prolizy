using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using SkiaSharp;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class GradeEvolutionChartViewModel : BaseObservableChartData
{
    [ObservableProperty] private string _title = "Évolution des notes au fil du temps";

    private readonly BulletinPaneViewModel _bulletinViewModel;
    private readonly ObservableCollection<ObservablePoint> _studentPoints = new();
    private readonly ObservableCollection<ObservablePoint> _avgPoints = new();
    private List<(DateTime Date, double StudentGrade, double AverageGrade, string Label)> _allEvaluations = new();
    private List<string> _labels = new();
    
    // Properties for scrollable chart
    [ObservableProperty] private ISeries[] _scrollbarSeries;
    [ObservableProperty] private Axis[] _invisibleX;
    [ObservableProperty] private Axis[] _invisibleY;
    [ObservableProperty] private RectangularSection[] _thumbs;
    [ObservableProperty] private LiveChartsCore.Measure.Margin _margin;
    
    public GradeEvolutionChartViewModel(BulletinPaneViewModel bulletinViewModel)
    {
        _bulletinViewModel = bulletinViewModel;
        
        // Initialize scrollbar components
        var auto = LiveChartsCore.Measure.Margin.Auto;
        Margin = new(100, auto, 50, auto);
        
        // Initialize thumbs for scrollbar
        Thumbs = new RectangularSection[]
        {
            new RectangularSection
            {
                Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100)),
                Stroke = new SolidColorPaint(SKColors.Transparent)
            }
        };
        
        // Initialize invisible axes for scrollbar
        InvisibleX = new Axis[] { new Axis { IsVisible = false } };
        InvisibleY = new Axis[] { new Axis { IsVisible = false } };
        
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
            return;
        
        _allEvaluations = new List<(DateTime Date, double StudentGrade, double AverageGrade, string Label)>();
        
        // Get evaluations from Resources
        if (_bulletinViewModel.Resources != null)
        {
            foreach (var resource in _bulletinViewModel.Resources)
            {
                foreach (var eval in resource.Evals)
                {
                    if (double.TryParse(eval.Evaluation.Grade.Value.Replace(".", ","), out var studentGrade) &&
                        double.TryParse(eval.Evaluation.Grade.Average.Replace(".", ","), out var avgGrade))
                    {
                        _allEvaluations.Add((eval.Evaluation.Date, studentGrade, avgGrade, resource.Resource.Title));
                    }
                }
            }
        }
        
        // Get evaluations from SAEs
        if (_bulletinViewModel.Saes != null)
        {
            foreach (var sae in _bulletinViewModel.Saes)
            {
                foreach (var eval in sae.Evals)
                {
                    if (double.TryParse(eval.Evaluation.Grade.Value.Replace(".", ","), out var studentGrade) &&
                        double.TryParse(eval.Evaluation.Grade.Average.Replace(".", ","), out var avgGrade))
                    {
                        _allEvaluations.Add((eval.Evaluation.Date, studentGrade, avgGrade, sae.Sae.Title));
                    }
                }
            }
        }
        
        if (_allEvaluations.Count == 0)
        {
            HasData = false;
            return;
        }
        
        HasData = true;
        
        // Sort by date
        _allEvaluations = _allEvaluations.OrderBy(e => e.Date).ToList();
        
        _studentPoints.Clear();
        _avgPoints.Clear();
        _labels = new List<string>();
        
        for (int i = 0; i < _allEvaluations.Count; i++)
        {
            var eval = _allEvaluations[i];
            _studentPoints.Add(new ObservablePoint(i, eval.StudentGrade));
            _avgPoints.Add(new ObservablePoint(i, eval.AverageGrade));
            _labels.Add(eval.Date.ToString("dd/MM/yy"));
        }
        
        // Configure main chart axes
        XAxes = [
            new Axis
            {
                Labels = _labels.ToArray(),
                LabelsRotation = -45,
                MinStep = 1,
                // Set the zoom feature active
                MinLimit = 0,
                MaxLimit = Math.Min(10, _allEvaluations.Count) // Display maximum 10 items at once
            }
        ];
        
        YAxes = [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 20
            }
        ];
        
        // Configure main chart series
        Series = [
            new LineSeries<ObservablePoint>
            {
                Name = "Mes notes",
                Values = _studentPoints,
                Fill = new SolidColorPaint(App.GetAccentColor(true)),
                Stroke = new SolidColorPaint(App.GetAccentColor()) { StrokeThickness = 3 },
                GeometrySize = 10,
                GeometryStroke = new SolidColorPaint(App.GetAccentColor()) { StrokeThickness = 3 },
                XToolTipLabelFormatter = point => $"{_allEvaluations[(int)point.Model!.X!].Label}",
                YToolTipLabelFormatter = point => $"{point.Model!.Y:0.##}",
                DataPadding = new LvcPoint(0, 1)
            },
            new LineSeries<ObservablePoint>
            {
                Name = "Moyenne de promotion",
                Values = _avgPoints,
                Fill = null,
                Stroke = new SolidColorPaint(ColorMatcher.Green.ToSKColor()) { StrokeThickness = 2 },
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(ColorMatcher.Green.ToSKColor()) { StrokeThickness = 2 },
                LineSmoothness = 0.7,
                XToolTipLabelFormatter = point => $"{_allEvaluations[(int)point.Model!.X!].Label}",
                DataPadding = new(0, 1)
            }
        ];
        
        // Configure scrollbar series
        ScrollbarSeries = [
            new LineSeries<ObservablePoint>
            {
                Values = _studentPoints,
                GeometryStroke = null,
                GeometryFill = null,
                Stroke = new SolidColorPaint(App.GetAccentColor()) { StrokeThickness = 1 },
                Fill = null,
                DataPadding = new(0, 1)
            }
        ];
        
        // Initialize the thumb position
        if (Thumbs.Length > 0)
        {
            var thumb = Thumbs[0];
            thumb.Xi = 0;
            thumb.Xj = Math.Min(10, _allEvaluations.Count);
        }
    }
    
    [RelayCommand]
    public void ChartUpdated(ChartCommandArgs args)
    {
        var cartesianChart = (ICartesianChartView) args.Chart;
        var x = cartesianChart.XAxes.First();
        
        // Update the scroll bar thumb when the chart is updated (zoom/pan)
        // This will let the user know the current visible range
        var thumb = Thumbs[0];
        thumb.Xi = x.MinLimit;
        thumb.Xj = x.MaxLimit;
    }
    
    private bool _isDown = false;
    
    [RelayCommand]
    public void PointerDown(PointerCommandArgs args)
    {
        _isDown = true;
    }
    
    [RelayCommand]
    public void PointerMove(PointerCommandArgs args)
    {
        if (!_isDown) return;
        
        var chart = (ICartesianChartView) args.Chart;
        var positionInData = chart.ScalePixelsToData(args.PointerPosition);
        
        var thumb = Thumbs[0];
        var w = thumb.Xj - thumb.Xi;
        
        if (positionInData.X - w / 2 < 0)
        {
            thumb.Xi = 0;
            thumb.Xj = w;
        }
        else if (positionInData.X + w / 2 > _allEvaluations.Count)
        {
            thumb.Xi = _allEvaluations.Count - w;
            thumb.Xj = _allEvaluations.Count;
        }
        else
        {
            thumb.Xi = positionInData.X - w / 2;
            thumb.Xj = positionInData.X + w / 2;
        }
        
        // Update ALL CartesianChart controls that use these axes
        foreach (var axis in XAxes)
        {
            axis.MinLimit = thumb.Xi;
            axis.MaxLimit = thumb.Xj;
        }
    }
    
    [RelayCommand]
    public void PointerUp(PointerCommandArgs args)
    {
        _isDown = false;
    }
}