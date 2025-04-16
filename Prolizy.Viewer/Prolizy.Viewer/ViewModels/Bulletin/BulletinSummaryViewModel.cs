using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API.Model;

namespace Prolizy.Viewer.ViewModels.Bulletin;

public partial class BulletinSummaryViewModel(BulletinPaneViewModel viewModel) : BaseBulletinTabViewModel(viewModel)
{
    
    [ObservableProperty] private ObservableCollection<InternalEvaluation> _latestEvals = [];

    public override void Update()
    {
        var allEvals = new List<Evaluation>();
        allEvals.AddRange(BaseViewModel.BulletinRoot.Transcript.Resources
            .Select(x => x.Value.Evaluations).SelectMany(x => x));
        allEvals.AddRange(BaseViewModel.BulletinRoot.Transcript.Saes
            .Select(x => x.Value.Evaluations).SelectMany(x => x));

        var latestEvals = allEvals.OrderByDescending(x => x.Date).Take(5);
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
        }
    }

    public override void Clear()
    {
        LatestEvals.Clear();
    }
}