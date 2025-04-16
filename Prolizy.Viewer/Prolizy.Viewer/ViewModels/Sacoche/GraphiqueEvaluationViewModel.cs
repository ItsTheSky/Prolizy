using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.ConditionalDraw;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Prolizy.API;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views.Panes;
using SkiaSharp;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class GraphiqueEvaluationViewModel : SacochePaneBaseViewModel
{
    #region Eval Graph

    [ObservableProperty] private ISeries[] _evalsSeries = [];
    [ObservableProperty] private ICartesianAxis[] _evalsYAxis = [];
    [ObservableProperty] private ICartesianAxis[] _evalsXAxis = [];
    public string EvalsTitle => "Évalutations";

    #endregion
    
    #region Skills Graph

    [ObservableProperty] private ISeries[] _skillsSeries = [];
    [ObservableProperty] private ICartesianAxis[] _skillsYAxis = [];
    [ObservableProperty] private ICartesianAxis[] _skillsXAxis = [];
    public string SkillsTitle => "Compétences";

    #endregion
    
    [ObservableProperty] private int _emptyEvalsCount;
    public Margin DrawMargin => new(10);

    public override async Task Initialize(SacocheClient client)
    {
        await base.Initialize(client);
        await GenerateEvalsCharts();
        await GenerateSkillsCharts();
    }

    private async Task GenerateEvalsCharts()
    {
        EvalsYAxis =
        [
            new Axis
            {
                Labels = [ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                          "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" ],
                MinLimit = 0,
                MaxLimit = 20,
            }
        ];

        var evalsToShowOnChart = Evaluations
            .Where(e => e.Skills.All(s => s.Note != null))
            .OrderBy(e => e.Evaluation.Date);

        EmptyEvalsCount = 0;
        var dates = new List<string>();
        var points = new List<ObservablePoint>();

        foreach (var eval in evalsToShowOnChart)
        {
            await eval.LoadSkills();
            if (eval.Skills.Any(skill => skill.Note == null))
            {
                EmptyEvalsCount++;
                continue;
            }
            
            var validSkills = eval.Skills.Where(skill => skill.Skill.Note != "NE").ToList();
            
            var sum = validSkills.Sum(skill => int.Parse(skill.Skill.Note) * 5);
            var value = sum / validSkills.Count;
            
            var date = eval.Evaluation.Date;
            dates.Add(date.ToString("dd/MM/yyyy"));
            points.Add(new ObservablePoint(dates.Count - 1, value));
        }

        EvalsXAxis =
        [
            new Axis
            {
                Labels = dates,
                MinLimit = 0,
                MaxLimit = dates.Count - 1,
            }
        ];

        EvalsSeries =
        [
            new LineSeries<ObservablePoint>
            {
                Values = points,
                IsVisibleAtLegend = false,
                Stroke = new SolidColorPaint(App.GetAccentColor()) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(App.GetAccentColor(true)),
                GeometryStroke = new SolidColorPaint(App.GetAccentColor()),
            }
        ];
    }

    private async Task GenerateSkillsCharts()
    {
        SkillsYAxis = [
            new Axis { 
                MinLimit = 0, 
                MaxLimit = 100
            }
        ];

        var rows = SacochePane.Instance.ViewModel.ReleveViewModel.SkillRows;
        if (rows.Count == 0)
        {
            await SacochePane.Instance.ViewModel.ReleveViewModel.GenerateSkillRows();
            rows = SacochePane.Instance.ViewModel.ReleveViewModel.SkillRows;
        }

        var array = rows.Select(skillRow => skillRow.FinalScore).ToArray();
        
        var skills = rows.Select(skillRow => skillRow.DisplayedScore).ToArray();
        var skillsNames = rows.Select(skillRow => skillRow.SkillName).ToArray();
        
        SkillsSeries = [
            new ColumnSeries<int>
            {
                Values = array,
                DataLabelsFormatter = point => skillsNames[point.Index],
                Fill = new SolidColorPaint(App.GetAccentColor(true)),
                Stroke = new SolidColorPaint(App.GetAccentColor()) { StrokeThickness = 1 },
            }.OnPointMeasured(point =>
            {
                if (point.Visual is null) return;

                var red = ColorMatcher.Red.ToSKColor();
                var green = ColorMatcher.Green.ToSKColor();
                var clr = 
                    new SKColor(
                        (byte)(red.Red + (green.Red - red.Red) * (point.Model / 100f)),
                        (byte)(red.Green + (green.Green - red.Green) * (point.Model / 100f)),
                        (byte)(red.Blue + (green.Blue - red.Blue) * (point.Model / 100f)),
                        255
                    );
                point.Visual.Fill = new SolidColorPaint(clr.LittleTransparent());
                point.Visual.Stroke = new SolidColorPaint(clr) { StrokeThickness = 1 };
            })
        ];
        SkillsXAxis = [
            new Axis
            {
                Labels = skills
            }
        ];
    }
}