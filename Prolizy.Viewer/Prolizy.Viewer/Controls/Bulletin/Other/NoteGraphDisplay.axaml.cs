using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.ConditionalDraw;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using SkiaSharp;

namespace Prolizy.Viewer.Controls.Bulletin.Other;

public partial class NoteGraphDisplay : UserControl
{
    public NoteGraphDisplay()
    {
        InitializeComponent();

        var random = new Random();
        var values = new List<int>();
        for (var i = 0; i <= 20; i++)
            values.Add(random.Next(0, 50));
        DataContext = new NoteGraphDisplayViewModel()
        {
            NoteValues = values
        };
    }
}

public partial class NoteGraphDisplayViewModel : ObservableObject
{

    [ObservableProperty] private List<int> _noteValues =
    [
        20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,
    ];

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _hasAnyNotes = true;
    [ObservableProperty] private int _ownerNote = -1;

    public NoteGraphDisplayViewModel()
    {
        
    }
    
    public ISeries[] Series => [
        new ColumnSeries<int>
        {
            Values = NoteValues,
            Stroke = null,
            Fill = new SolidColorPaint(ColorMatcher.Slate.ToSKColor()),
            IgnoresBarPosition = true,
            
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 12,
            DataLabelsPosition = DataLabelsPosition.End,
            IsVisibleAtLegend = true,
            
            MaxBarWidth = double.MaxValue,
            Padding = 1.5,
        }.OnPointMeasured(point =>
        {
            if (point.Visual is null) return;

            Console.WriteLine("Point measured: " + point.Index + " --- " + OwnerNote);
            var isOwner = point.Index == OwnerNote;
            point.Visual.Fill = isOwner
                ? new SolidColorPaint(ColorMatcher.Blue.ToSKColor())
                : null;
        })
    ];

    public ICartesianAxis[] YAxes => [ 
        new Axis()
        {
            MinLimit = 0,
            IsVisible = false
        }
    ];
    
}