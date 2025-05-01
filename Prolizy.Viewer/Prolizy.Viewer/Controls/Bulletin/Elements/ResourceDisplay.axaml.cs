using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentIcons.Common;
using Prolizy.API.Model;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using SpacedGridControl.Avalonia;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIcon = FluentIcons.Avalonia.SymbolIcon;

namespace Prolizy.Viewer.Controls.Bulletin.Elements;

public partial class ResourceDisplay : UserControl
{
    public ResourceDisplay()
    {
        InitializeComponent();
    }
}

public partial class InternalResource : ObservableObject
{
    
    [ObservableProperty] private Resource _resource;
    [ObservableProperty] private ObservableCollection<InternalResourceEval> _evals = [];
    [ObservableProperty] private bool _isAboveAverage; 
    [ObservableProperty] private string _average = "NaN";
    [ObservableProperty] private bool _isExpanded = true;

    public BulletinPaneViewModel ViewModel { get; init; }

    public InternalResource(Resource resource, BulletinPaneViewModel viewModel)
    {
        ViewModel = viewModel;
        Resource = resource;
        foreach (var eval in resource.Evaluations)
            Evals.Add(new InternalResourceEval(eval, viewModel));
        
        float total = 0;
        float totalOther = 0;
        float count = 0;
        
        foreach (var eval in resource.Evaluations)
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

public partial class InternalResourceEval : ObservableObject
{
    
    [ObservableProperty] private Evaluation _evaluation;
    
    public BulletinPaneViewModel ViewModel { get; init; }
    
    public InternalResourceEval(Evaluation evaluation, BulletinPaneViewModel viewModel)
    {
        ViewModel = viewModel;
        Evaluation = evaluation;
    }

    [RelayCommand]
    public async Task ShowEval()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 5
        };
        
        try
        {
            var studentNote = float.Parse(Evaluation.Grade.Value.Replace(".", ","));
            var averageNote = float.Parse(Evaluation.Grade.Average.Replace(".", ","));

            var noteEntry = new SpacedGrid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 5
            };
            var studentNoteText = new TextBlock
            {
                Text = "Note",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var studentNoteValue = new TextBlock
            {
                Text = Evaluation.Grade.Value,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var studentNoteIndicator = new SymbolIcon
            {
                Symbol = studentNote > averageNote ? Symbol.ChevronUp : Symbol.ChevronDown,
                Foreground = studentNote > averageNote 
                    ? new SolidColorBrush(Color.Parse(ColorMatcher.TailwindColors["green"])) 
                    : new SolidColorBrush(Color.Parse(ColorMatcher.TailwindColors["red"])),
                IconVariant = IconVariant.Filled,
                
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            noteEntry.Children.Add(studentNoteText);
            noteEntry.Children.Add(studentNoteValue);
            noteEntry.Children.Add(studentNoteIndicator);
            
            Grid.SetColumn(studentNoteText, 0);
            Grid.SetColumn(studentNoteIndicator, 1);
            Grid.SetColumn(studentNoteValue, 2);
            
            panel.Children.Add(noteEntry);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var noteEntry = new TextBlock
            {
                Text = "Note non renseignée",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            panel.Children.Add(noteEntry);
        }
        
        var datas = new Dictionary<string, string>()
        {
            { "Max. Promotion", Evaluation.Grade.Maximum },
            { "Moy. Promotion", Evaluation.Grade.Average },
            { "Min. Promotion", Evaluation.Grade.Minimum },
            { "Date", Evaluation.Date.ToString("dd/MM/yyyy") }
        };
        var separatorIndex = datas.Count;
        foreach (var unitWeight in Evaluation.Weights)
            datas.Add($"Poids {unitWeight.Key}", unitWeight.Value.ToString());
        
        var grid = Utilities.Controls.CreateDataGrid(datas, separatorIndexes: [separatorIndex], rowSpacing: 3);
        panel.Children.Add(grid);
        
        var dialog = new ContentDialog
        {
            Title = Evaluation.Description,
            Content = panel,
            
            CloseButtonText = "Fermer",
            
            PrimaryButtonCommand = OpenEvalNotesCommand,
            PrimaryButtonText = "Liste des Notes"
        };
        dialog.PrimaryButtonCommandParameter = dialog;
        await dialog.ShowAsync();
    }

    [RelayCommand]
    public async Task OpenEvalNotes(ContentDialog dialog)
    {
        dialog.Hide();
        await ViewModel.OpenNotesList(Evaluation);
    }
    
}