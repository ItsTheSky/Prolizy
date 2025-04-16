using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using SkiaSharp;

namespace Prolizy.Viewer.Controls.Bulletin.Charts;

public partial class AbsenceAnalysisChartViewModel : BaseObservableChartData
{

    [ObservableProperty] private string _title = "Analyse des absences";
    private readonly BulletinPaneViewModel _bulletinViewModel;
    
    public AbsenceAnalysisChartViewModel(BulletinPaneViewModel bulletinViewModel)
    {
        _bulletinViewModel = bulletinViewModel;
        UpdateChart();
        
        _bulletinViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(_bulletinViewModel.Absences))
            {
                UpdateChart();
            }
        };
    }
    
    public void UpdateChart()
    {
        if (_bulletinViewModel.Absences == null || _bulletinViewModel.Absences.Count == 0)
        {
            HasData = false;
            return;
        }

        HasData = true;
        
        // Group absences by month
        var absencesByMonth = new Dictionary<string, (int JustifiedAbsences, int UnjustifiedAbsences, int Lates)>();
        
        foreach (var absenceDay in _bulletinViewModel.Absences)
        {
            foreach (var absence in absenceDay.DayAbsences)
            {
                var date = absence.Date;
                var monthKey = date.ToString("MMM yy");
                
                if (!absencesByMonth.ContainsKey(monthKey))
                {
                    absencesByMonth[monthKey] = (0, 0, 0);
                }
                
                var current = absencesByMonth[monthKey];
                
                if (absence.IsLate)
                {
                    absencesByMonth[monthKey] = (current.JustifiedAbsences, current.UnjustifiedAbsences, current.Lates + 1);
                }
                else if (absence.IsJustified)
                {
                    absencesByMonth[monthKey] = (current.JustifiedAbsences + 1, current.UnjustifiedAbsences, current.Lates);
                }
                else
                {
                    absencesByMonth[monthKey] = (current.JustifiedAbsences, current.UnjustifiedAbsences + 1, current.Lates);
                }
            }
        }
        
        // Sort months chronologically
        var sortedMonths = absencesByMonth.Keys
            .Select(m => new { Month = m, Date = DateTime.ParseExact(m, "MMM yy", System.Globalization.CultureInfo.CurrentCulture) })
            .OrderBy(m => m.Date)
            .Select(m => m.Month)
            .ToArray();
        
        var justifiedValues = new List<double>();
        var unjustifiedValues = new List<double>();
        var lateValues = new List<double>();
        
        foreach (var month in sortedMonths)
        {
            var data = absencesByMonth[month];
            justifiedValues.Add(data.JustifiedAbsences);
            unjustifiedValues.Add(data.UnjustifiedAbsences);
            lateValues.Add(data.Lates);
        }
        
        // Configure axes
        XAxes = [
            new Axis
            {
                Labels = sortedMonths,
                LabelsRotation = 0
            }
        ];
        
        YAxes = [
            new Axis
            {
                MinLimit = 0,
                Name = "Nombre d'absences"
            }
        ];
        
        // Create stacked series
        Series = [
            new StackedColumnSeries<double>
            {
                Name = "Absences justifiées",
                Values = justifiedValues,
                Fill = new SolidColorPaint(ColorMatcher.Green.ToSKColor()),
                Stroke = null,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                DataLabelsFormatter = point => point.Model > 0 ? point.Model.ToString(CultureInfo.InvariantCulture) : string.Empty
            },
            new StackedColumnSeries<double>
            {
                Name = "Absences non justifiées",
                Values = unjustifiedValues,
                Fill = new SolidColorPaint(ColorMatcher.Red.ToSKColor()),
                Stroke = null,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                DataLabelsFormatter = point => point.Model > 0 ? point.Model.ToString(CultureInfo.InvariantCulture) : string.Empty
            },
            new StackedColumnSeries<double>
            {
                Name = "Retards",
                Values = lateValues,
                Fill = new SolidColorPaint(ColorMatcher.Yellow.ToSKColor()),
                Stroke = null,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                DataLabelsFormatter = point => point.Model > 0 ? point.Model.ToString(CultureInfo.InvariantCulture) : string.Empty
            }
        ];
    }
}