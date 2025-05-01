using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class ReleveEvaluationViewModel : SacochePaneBaseViewModel
{
    private SortingType _selectedSortingType = SortingType.Alphabetical;
    public SortingType SelectedSortingType
    {
        get => _selectedSortingType;
        set
        {
            _selectedSortingType = value;
            SkillRows = new ObservableCollection<SkillGridRow>(value.SortFunction(SkillRows.ToList()));
            OnPropertyChanged();
        }
    }

    [ObservableProperty] private ObservableCollection<SkillGridRow> _skillRows = new();
    [ObservableProperty] private double _averageScore;
    [ObservableProperty] private string _formattedScore = string.Empty;

    public List<SortingType> SortingTypes { get; } =
    [
        SortingType.Alphabetical,
        SortingType.ReverseAlphabetical,
        SortingType.HighestToLowest,
        SortingType.LowestToHighest,
    ];

    public override async Task Initialize(SacocheClient client)
    {
        await base.Initialize(client);
        await GenerateSkillRows();
    }

    public async Task GenerateSkillRows()
    {
        var lines = new Dictionary<string, SkillLineData>();
        var sortedEval = Evaluations.OrderBy(e => e.Evaluation.Date);

        foreach (var eval in sortedEval)
        {
            await eval.LoadSkills();
            foreach (var skill in eval.Skills)
            {
                var skillName = skill.Skill.Name;
                if (!lines.ContainsKey(skillName))
                {
                    lines[skillName] = new SkillLineData
                    {
                        Notes = new List<int?>(),
                        Score = null,
                        ImageUrl = skill.Note?.Image ?? string.Empty
                    };
                }

                var currentNotes = new List<int?>(lines[skillName].Notes);
                var preNote = skill.Skill.Note;
                currentNotes.Add(preNote == null! || skill.Skill.Note == "NE" ? null : int.Parse(skill.Skill.Note));

                var score = ScoreUtils.CalculateWeightedScore(currentNotes);

                lines[skillName] = lines[skillName] with
                {
                    Notes = currentNotes,
                    Score = score
                };
            }
        }

        GenerateDataGridRows(lines);
    }


    private void GenerateDataGridRows(Dictionary<string, SkillLineData> lines)
    {
        SkillRows.Clear();
        
        var totalScores = 0;
        var amountOfScores = 0;

        foreach (var (skillName, data) in lines)
        {
            SkillRows.Add(new SkillGridRow(skillName, data.Notes, data.ImageUrl, data.Score));
            
            if (data.Score == null)
                continue;
            
            totalScores += data.Score.Value;
            amountOfScores++;
        }

        AverageScore = totalScores / (double)amountOfScores;
        FormattedScore = $"{AverageScore:0.00}% soit {((AverageScore / 100.0) * 20):0.00}/20";
    }
}