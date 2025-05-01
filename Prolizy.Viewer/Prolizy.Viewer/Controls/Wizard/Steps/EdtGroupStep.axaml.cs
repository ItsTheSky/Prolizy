using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API;
using Prolizy.API.Model.Request;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Controls.Wizard.Steps;

public partial class EdtGroupStep : UserControl
{
    public static EdtGroupStep Instance { get; private set; }
    
    private List<string> _foundGroups = [];
    public EdtGroupStep(string? current)
    {
        InitializeComponent();
        Instance = this;

        AutoCompleteBox.Watermark = string.IsNullOrEmpty(current) ? "Entrez votre groupe" : current;
        AutoCompleteBox.FilterMode = AutoCompleteFilterMode.None;
        AutoCompleteBox.TextChanged += (_, _) =>
        {
            Dispatcher.UIThread.InvokeAsync(() => SelfRefresh(AutoCompleteBox.Text));
        };

        DataContext = new EdtGroupStepViewModel();
    }
    
    public EdtGroupStepViewModel ViewModel => (EdtGroupStepViewModel)DataContext!;
    
    public EdtGroupStep() : this(null) { }

    public async Task SelfRefresh(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            ViewModel.Groups = ["Veuillez entrer au moins 3 caractères"];
            return;
        }
        
        if (input.Length < 3)
        {
            ViewModel.Groups = ["Veuillez entrer au moins 3 caractères"];
            return;
        }
        
        ViewModel.Groups = ["Recherche en cours..."];
        
        try
        {
            _foundGroups = await EDTClient.GetGroups(new FederationRequest { SearchTerm = input });
        }
        catch (Exception e)
        {
            ViewModel.Groups = [e.ToString()];
            return;
        }
        ViewModel.Groups = new ObservableCollection<string>(_foundGroups);
        
        if (_foundGroups.Contains(input))
        {
            if (WizardManager.HasBeenOpened)
                WizardManager.CanContinue(WizardManager.SacocheChoice);
            Settings.Instance.StudentGroup = input;
            Settings.Instance.Save();
        }
        
        if (_foundGroups.Count == 0)
        {
            ViewModel.Groups = ["Aucun groupe trouvé"];
        }
    }

    public static void Refresh()
    {
        Dispatcher.UIThread.InvokeAsync(() => Instance.SelfRefresh(Instance.AutoCompleteBox.Text));
    }
}

public partial class EdtGroupStepViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<string> _groups = ["Veuillez entrer au moins 3 caractères"];
}