using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API.Model;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Controls.Bulletin.Elements;

public partial class ProjectDisplay : UserControl
{
    public ProjectDisplay()
    {
        InitializeComponent();
    }
}

public partial class InternalSaeDisplay : ObservableObject
{
    
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private Sae _sae;
    [ObservableProperty] private ObservableCollection<InternalResourceEval> _evals = [];
    [ObservableProperty] private string _average = "NaN";
    [ObservableProperty] private bool _isAboveAverage;
    
    public BulletinPaneViewModel ViewModel { get; init; }

    public InternalSaeDisplay(Sae sae, BulletinPaneViewModel viewModel)
    {
        ViewModel = viewModel;
        Sae = sae;
        foreach (var eval in sae.Evaluations)
            Evals.Add(new InternalResourceEval(eval, viewModel));
        
        float total = 0;
        float totalOther = 0;
        float count = 0;
        
        foreach (var eval in sae.Evaluations)
        {
            if (!float.TryParse(eval.Grade.Value.Replace(".", ","), out var result))
                continue;
            var coef = float.Parse(eval.Coefficient.Replace(".", ","));
            var other = float.Parse(eval.Grade.Average.Replace(".", ","));

            total += result * coef;
            totalOther += other * coef;
            count += coef;
        }

        Average = $"{total / count:0.00}";
        IsAboveAverage = total / count > totalOther / count;
    }

}