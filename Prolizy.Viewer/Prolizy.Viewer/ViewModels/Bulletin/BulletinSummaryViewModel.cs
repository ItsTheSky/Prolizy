using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using Prolizy.API.Model;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels.Bulletin;

public partial class BulletinSummaryViewModel(BulletinPaneViewModel viewModel) : BaseBulletinTabViewModel(viewModel)
{
    
    [ObservableProperty] private ObservableCollection<InternalEvaluation> _latestEvals = [];

    public override void Update()
    {
        var resourcesEvals = BaseViewModel.BulletinRoot.Transcript.Resources
            .Select(x => x.Value.Evaluations).SelectMany(x => x);
        var saeEvals =BaseViewModel.BulletinRoot.Transcript.Saes
            .Select(x => x.Value.Evaluations).SelectMany(x => x);

        /*var latestEvals = allEvals.OrderByDescending(x => x.Date).Take(5);
        foreach (var eval in latestEvals)
        {
            if (double.TryParse(eval.Grade.Value.Replace(".", ","), out var studentNote) &&
                double.TryParse(eval.Grade.Average.Replace(".", ","), out var averageNote))
            {
                LatestEvals.Add(new InternalEvaluation(
                    eval,
                    studentNote > averageNote,
                    eval.Date.ToString("dd/MM/yyyy")));
            }
        }*/
        List<RichEvaluation> allEvals = new List<RichEvaluation>();
        foreach (var eval in resourcesEvals)
            allEvals.Add(new RichEvaluation(eval, true));
        foreach (var eval in saeEvals)
            allEvals.Add(new RichEvaluation(eval, false));
        
        allEvals = allEvals.OrderByDescending(x => x.Evaluation.Date).Take(5).ToList();
        
        foreach (var eval in allEvals)
        {
            if (double.TryParse(eval.Evaluation.Grade.Value.Replace(".", ","), out var studentNote) &&
                double.TryParse(eval.Evaluation.Grade.Average.Replace(".", ","), out var averageNote))
            {
                var symbol = eval.IsResource ? Symbol.Notebook : Symbol.ProjectionScreen;
     
                LatestEvals.Add(new InternalEvaluation(
                    eval.Evaluation,
                    studentNote > averageNote,
                    eval.Evaluation.Date.ToString("dd/MM"),
                    symbol));
            }
        }
    }

    public override void Clear()
    {
        LatestEvals.Clear();
    }
}

public record RichEvaluation(Evaluation Evaluation, bool IsResource);