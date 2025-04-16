using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using SkiaSharp;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class ResourceSaeComparisonChartViewModel : BaseObservableChartData
{

    [ObservableProperty] private string _title = "Comparaison Ressources & SAÉs par UE";
    
    private readonly BulletinPaneViewModel _bulletinViewModel;
    
    public ResourceSaeComparisonChartViewModel(BulletinPaneViewModel bulletinViewModel)
    {
        _bulletinViewModel = bulletinViewModel;
        UpdateChart();
        
        _bulletinViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_bulletinViewModel.Units) ||
                args.PropertyName == nameof(_bulletinViewModel.Resources) ||
                args.PropertyName == nameof(_bulletinViewModel.Saes))
            {
                UpdateChart();
            }
        };
    }

    public void UpdateChart()
    {
        if (_bulletinViewModel.Units.Count == 0)
        {
            HasData = false;
            return;
        }

        HasData = true;
        
        var units = _bulletinViewModel.Units.ToList();
        var allItems = new List<(string UeTitle, string ItemTitle, bool IsSae, double Grade, string Color)>();
        
        // Process each UE to get its resources and SAEs
        foreach (var unit in units)
        {
            foreach (var entry in unit.Entries)
            {
                var entryData = entry.Data;
                if (double.TryParse(entryData.Average, out var grade))
                {
                    // Determine color based on grade
                    string color = grade switch
                    {
                        < 8 => ColorMatcher.TailwindColors["red"],
                        < 12 => ColorMatcher.TailwindColors["yellow"],
                        _ => ColorMatcher.TailwindColors["green"]
                    };
                    
                    allItems.Add((unit.Title, entryData.Title, entryData.IsSae, grade, color));
                }
            }
        }
        
        // If no data, show info
        if (allItems.Count == 0)
        {
            HasData = false;
            return;
        }
        
        var resourceSeries = new List<ISeries>();
        var saeSeries = new List<ISeries>();
        var ueLabels = units.Select(u => u.Title).Distinct().ToArray();
        
        // Process resources for each UE
        var resourcesByUe = allItems
            .Where(i => !i.IsSae)
            .GroupBy(i => i.UeTitle)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var unit in ueLabels)
        {
            if (!resourcesByUe.TryGetValue(unit, out var resources))
                continue;
            
            var values = new double[ueLabels.Length];
            for (int i = 0; i < ueLabels.Length; i++)
            {
                if (ueLabels[i] == unit)
                {
                    // Calculate average for this UE's resources
                    values[i] = resources.Average(r => r.Grade);
                }
                else
                {
                    // No value for other UEs
                    values[i] = 0;
                }
            }
            
            // Choose color based on average grade
            var avgGrade = resources.Average(r => r.Grade);
            string color = avgGrade switch
            {
                < 8 => ColorMatcher.TailwindColors["red"],
                < 12 => ColorMatcher.TailwindColors["yellow"],
                _ => ColorMatcher.TailwindColors["green"]
            };
            
            resourceSeries.Add(new ColumnSeries<double>
            {
                Name = $"Ressources {unit}",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse(color)),
                Stroke = null,
                Padding = 5
            });
        }
        
        // Process SAEs for each UE
        var saesByUe = allItems
            .Where(i => i.IsSae)
            .GroupBy(i => i.UeTitle)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var unit in ueLabels)
        {
            if (!saesByUe.TryGetValue(unit, out var saes))
                continue;
            
            var values = new double[ueLabels.Length];
            for (int i = 0; i < ueLabels.Length; i++)
            {
                if (ueLabels[i] == unit)
                {
                    // Calculate average for this UE's SAEs
                    values[i] = saes.Average(s => s.Grade);
                }
                else
                {
                    // No value for other UEs
                    values[i] = 0;
                }
            }
            
            // Choose color based on average grade
            var avgGrade = saes.Average(s => s.Grade);
            string color = avgGrade switch
            {
                < 8 => ColorMatcher.TailwindColors["red"],
                < 12 => ColorMatcher.TailwindColors["yellow"],
                _ => ColorMatcher.TailwindColors["green"]
            };
            
            saeSeries.Add(new ColumnSeries<double>
            {
                Name = $"SAÉs {unit}",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse(color).WithAlpha(150)),
                Stroke = new SolidColorPaint(SKColor.Parse(color)) { StrokeThickness = 2 },
                Padding = 5
            });
        }
        
        // Configure axes
        XAxes = [
            new Axis
            {
                Labels = ueLabels,
                LabelsRotation = -15,
                Padding = new Padding(15)
            }
        ];
        
        YAxes = [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 20
            }
        ];
        
        // Combine all series
        Series = resourceSeries.Concat(saeSeries).ToArray();
    }
}