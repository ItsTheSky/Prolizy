using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Prolizy.API;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class NoteViewModel : ObservableObject
{
    [ObservableProperty] private int? _value;
    [ObservableProperty] private string _imageUrl = string.Empty;
}

public record SkillLineData
{
    public required List<int?> Notes { get; init; } = new();
    public required int? Score { get; init; }
    public required string ImageUrl { get; init; } = string.Empty;
}

public partial class SkillGridRow : ObservableObject
{
    [ObservableProperty] private string _skillName = string.Empty;
    [ObservableProperty] private ObservableCollection<NoteViewModel> _notes = new();
    [ObservableProperty] private int _finalScore;
    [ObservableProperty] private string _scoreColor = string.Empty;
    [ObservableProperty] private string _displayedScore = string.Empty;

    public SkillGridRow(string skillName, List<int?> notes, string imageUrl, int? finalScore)
    {
        SkillName = skillName;
        Notes = new ObservableCollection<NoteViewModel>(
            notes.Select(n => new NoteViewModel 
            { 
                Value = n,
                ImageUrl = imageUrl
            })
        );
        DisplayedScore = finalScore == null ? "N/A" : $"{finalScore}";
        FinalScore = finalScore ?? -1;
        ScoreColor = ScoreUtils.GetScoreColor(finalScore);
    }
}

public class SkillGridRowComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        if (x is SkillGridRow rowX && y is SkillGridRow rowY)
        {
            return rowX.FinalScore.CompareTo(rowY.FinalScore);
        }
        return 0;
    }
}

public partial class InternalEvaluationReference : ObservableObject
{
    [ObservableProperty] private EvaluationReference _evaluation = null!;
    [ObservableProperty] private bool _areSkillsLoading;
    [ObservableProperty] private ObservableCollection<InternalSkill> _skills = new();
    
    public InternalEvaluationReference()
    {
        LoadSkillsCommand = new AsyncRelayCommand(LoadSkills);
    }

    public required SacocheClient SacocheClient { get; init; }
    public IAsyncRelayCommand LoadSkillsCommand { get; set; } = null!;
    
    public string Header => $"Évaluation du {Evaluation.Date:dd MMMM yyyy}";
    
    public async Task LoadSkills()
    {
        if (Skills.Count > 0 || AreSkillsLoading) 
            return;
        
        AreSkillsLoading = true;
        try 
        {
            var skillsData = await SacocheClient.GetSkills(Evaluation);
            foreach (var skill in skillsData.Skills)
            {
                if (skill.Note == null!)
                {
                    Skills.Add(new InternalSkill
                    {
                        Skill = skill,
                        Note = null
                    });
                    continue;
                }

                var note = int.TryParse(skill.Note, out var n) 
                    ? skillsData.SkillNotes[n] 
                    : new SkillNote
                    {
                        Image = "https://sacoche.ac-versailles.fr/_img/note/commun/h/NE.gif",
                        Sigle = "NE",
                        Tooltip = "Non Évalué"
                    };
                
                Skills.Add(new InternalSkill
                {
                    Skill = skill,
                    Note = note
                });
            }
        }
        finally 
        {
            AreSkillsLoading = false;
        }
    }
}

public partial class InternalSkill : ObservableObject
{
    private static readonly HttpClient ImageClient = new();
    
    [ObservableProperty] private EvaluationSkill _skill = null!;
    [ObservableProperty] private SkillNote? _note;

    public Task<Bitmap> NoteImage => Task.Run(async () =>
    {
        var url = Note?.Image ?? string.Empty;
        if (string.IsNullOrEmpty(url))
            return null!;

        var response = await ImageClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return new Bitmap(await response.Content.ReadAsStreamAsync());
    });
}

public record SortingType(string CanonicalName, Symbol Icon, Func<List<SkillGridRow>, List<SkillGridRow>> SortFunction)
{
    public static readonly SortingType HighestToLowest = new(
        "Score Décroissant", 
        Symbol.ArrowSortDownLines,
        skills => skills.OrderByDescending(s => s.FinalScore).ToList());

    public static readonly SortingType LowestToHighest = new(
        "Score Croissant",
        Symbol.ArrowSortUpLines,
        skills => skills.OrderBy(s => s.FinalScore).ToList());

    public static readonly SortingType Alphabetical = new(
        "Nom",
        Symbol.TextSortAscending,
        skills => skills.OrderBy(s => s.SkillName).ToList());

    public static readonly SortingType ReverseAlphabetical = new(
        "Nom Renversé",
        Symbol.TextSortDescending,
        skills => skills.OrderByDescending(s => s.SkillName).ToList());
}

public static class SacocheConstants
{
    public static class Urls
    {
        public const string NonEvalueImage = "https://sacoche.ac-versailles.fr/_img/note/commun/h/NE.gif";
    }

    public static class Scores
    {
        public const int RedThreshold = 40;
        public const int YellowThreshold = 61;
        public const int GreenThreshold = 100;
    }
}

public interface ISkillNote
{
    string Image { get; }
    string Sigle { get; }
    string Tooltip { get; }
}

public static class ScoreUtils
{
    public static string GetScoreColor(int? score) => score switch
    {
        < SacocheConstants.Scores.RedThreshold => ColorMatcher.TailwindColors["red"],
        < SacocheConstants.Scores.YellowThreshold => ColorMatcher.TailwindColors["yellow"],
        <= SacocheConstants.Scores.GreenThreshold => ColorMatcher.TailwindColors["green"],
        _ => ColorMatcher.TailwindColors["gray"]
    };

    public static string FormatScore(double score) => 
        $"{score:0.00}% soit {((score / 100.0) * 20):0.00}/20";

    public static int? CalculateWeightedScore(IEnumerable<int?> notes)
    {
        var totalScore = 0;
        var totalCoef = 0;

        var index = 0;
        foreach (var note in notes)
        {
            if (note.HasValue)
            {
                var coef = (int)Math.Pow(2, index);
                totalScore += (note.Value - 1) * coef;
                totalCoef += coef;
            }
            index++;
        }

        return totalCoef > 0
            ? (int)MathF.Round(totalScore * 100f / 3 / totalCoef)
            : null;
    }
}