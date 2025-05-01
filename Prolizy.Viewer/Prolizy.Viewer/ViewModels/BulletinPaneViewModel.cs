using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.API;
using Prolizy.API.Model;
using Prolizy.Viewer.Controls.Bulletin.Charts;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels.Bulletin;
using Prolizy.Viewer.Views;
using BulletinLoginDialog = Prolizy.Viewer.Controls.Bulletin.Other.BulletinLoginDialog;
using InternalResource = Prolizy.Viewer.Controls.Bulletin.Elements.InternalResource;
using InternalSaeDisplay = Prolizy.Viewer.Controls.Bulletin.Elements.InternalSaeDisplay;
using InternalTeachingUnit = Prolizy.Viewer.Controls.Bulletin.Elements.InternalTeachingUnit;
using NoteGraphDisplay = Prolizy.Viewer.Controls.Bulletin.Other.NoteGraphDisplay;
using NoteGraphDisplayViewModel = Prolizy.Viewer.Controls.Bulletin.Other.NoteGraphDisplayViewModel;
using Symbol = FluentIcons.Common.Symbol;

namespace Prolizy.Viewer.ViewModels;

public partial class BulletinPaneViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<InternalTeachingUnit> _units = [];
    [ObservableProperty] private ObservableCollection<InternalResource> _resources = [];
    [ObservableProperty] private ObservableCollection<InternalSaeDisplay> _saes = [];
    [ObservableProperty] private ObservableCollection<InternalAbsenceDay> _absences = [];

    [ObservableProperty] private ObservableCollection<InternalSemester> _availableSemesters = [];
    [ObservableProperty] private InternalSemester _currentSemester;

    [ObservableProperty] private BulletinRoot _bulletinRoot;
    [ObservableProperty] private bool _isBulletinAvailable = false;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isNetworkUnavailable = false;

    [ObservableProperty] private BulletinSummaryViewModel _summaryTabViewModel;
    [ObservableProperty] private BulletinChartsViewModel _chartsViewModel;
    [ObservableProperty] private BulletinUnitsViewModel _unitsViewModel;
    
    public Collection<BaseBulletinTabViewModel> Tabs =>
    [
        SummaryTabViewModel,
        UnitsViewModel
    ];

    public RelayCommand<AbsenceSortingType> ChangeAbsenceSortingTypeCommand => new(
        sortingType => SelectedAbsenceSortingType = sortingType!);
    [ObservableProperty] private ObservableCollection<AbsenceSortingType> _absenceSortingTypes =
    [
        new()
        {
            DisplayedName = "Tous",
            Icon = Symbol.Multiselect,
            IsSelected = true
        },

        new()
        {
            DisplayedName = "Absences",
            Icon = Symbol.PersonQuestionMark,
            IsSelected = false
        },

        new()
        {
            DisplayedName = "Retards",
            Icon = Symbol.Clock,
            IsSelected = false
        }
    ];
    [ObservableProperty] private AbsenceSortingType _selectedAbsenceSortingType;

    private BulletinClient _client;

    public BulletinPaneViewModel()
    {
        // Initialize the client
        if (!string.IsNullOrEmpty(Settings.Instance.BulletinUsername) &&
            !string.IsNullOrEmpty(Settings.Instance.BulletinPassword))
        {
            _client = new BulletinClient
            {
                Username = Settings.Instance.BulletinUsername,
                Password = SecureStorage.DecryptPassword(Settings.Instance.BulletinPassword)
            };
        }

        SelectedAbsenceSortingType = AbsenceSortingTypes[0];
        PropertyChanged += (source, args) =>
        {
            if (args.PropertyName == nameof(SelectedAbsenceSortingType))
            {
                foreach (var otherSortingType in AbsenceSortingTypes)
                {
                    otherSortingType.IsSelected = otherSortingType == SelectedAbsenceSortingType;
                }
                
                UpdateAbsences(SelectedAbsenceSortingType);
            }
        };
        
        ConnectivityService.Instance.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ConnectivityService.Instance.IsNetworkAvailable))
            {
                UpdateNetworkStatus();
            }
        };
        
        // Initialize network status
        UpdateNetworkStatus();
        
        // Initialize the view models
        ChartsViewModel = new BulletinChartsViewModel(this);
        SummaryTabViewModel = new BulletinSummaryViewModel(this);
        UnitsViewModel = new BulletinUnitsViewModel(this);
    }
    
    private void UpdateNetworkStatus()
    {
        // Only set IsNetworkUnavailable to true if we're configured but can't connect due to network issues
        IsNetworkUnavailable = !ConnectivityService.Instance.IsNetworkAvailable && 
                               !string.IsNullOrEmpty(Settings.Instance.BulletinUsername) &&
                               !string.IsNullOrEmpty(Settings.Instance.BulletinPassword);
    }

    [RelayCommand]
    public async Task RetryConnection()
    {
        // Show loading while we check connectivity
        IsLoading = true;
        IsNetworkUnavailable = false;
        
        try
        {
            bool isAvailable = await ConnectivityService.Instance.CheckConnectivity();
            if (isAvailable)
            {
                await RefreshBulletin();
            }
            else
            {
                IsNetworkUnavailable = true;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task OpenNotesList(Evaluation evaluation)
    {
        var vm = new NoteGraphDisplayViewModel();
        var dialog = new ContentDialog()
        {
            Title = "Liste des Notes",
            Content = new NoteGraphDisplay { DataContext = vm },
            
            CloseButtonText = "Fermer"
        };
        dialog.Opened += (source, args) => _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try 
            {
                var notes = await evaluation.FetchNotes(_client);

                if (notes != null)
                {
                    vm.HasAnyNotes = true;
                    var noteCounts = new int[21];
                    foreach (var roundedNote in notes.Select(note => (int) Math.Floor(note)))
                    {
                        if (roundedNote is < 0 or > 20)
                            continue;
                
                        noteCounts[roundedNote]++;
                    }
            
                    vm.IsLoading = false;
                    vm.NoteValues = noteCounts.ToList();
                    try
                    {
                        vm.OwnerNote = (int)Math.Floor(double.Parse(evaluation.Grade.Value.Replace(".", ",")));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to parse owner note");
                    }   
                }
                else
                {
                    vm.IsLoading = false;
                    vm.NoteValues = [];
                    vm.HasAnyNotes = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching notes: {ex.Message}");
                
                // Check if it's a network issue
                if (await ConnectivityService.Instance.IsNetworkIssue())
                {
                    vm.IsLoading = false;
                    vm.HasAnyNotes = false;
                    MainView.ShowNotification("Erreur de connexion", 
                        "Impossible de récupérer les notes en raison d'un problème de connexion internet", 
                        NotificationType.Error);
                    dialog.Hide();
                }
            }
        });
        await dialog.ShowAsync();
    }

    [RelayCommand]
    public async Task RefreshBulletin()
    {
        // Check connectivity first
        if (!ConnectivityService.Instance.IsNetworkAvailable)
        {
            IsNetworkUnavailable = true;
            return;
        }
        
        await InitializeClientIfNeeded();
        if (_client == null!)
        {
            IsBulletinAvailable = false;
            return;
        }

        IsBulletinAvailable = true;
        IsLoading = true;
        IsNetworkUnavailable = false;
        
        try
        {
            // First connection to get all semesters
            var initialRoot = await _client.FetchDatas();
            if (initialRoot == null)
            {
                IsBulletinAvailable = false;
                return;
            }

            // Update available semesters
            AvailableSemesters = new ObservableCollection<InternalSemester>(
                initialRoot.Semesters.OrderByDescending(s => s.SemesterId)
                    .Select(s => new InternalSemester { Semester = s })
            );

            // Set current semester (latest by default) and load its data
            CurrentSemester = AvailableSemesters.FirstOrDefault();
            if (CurrentSemester != null)
            {
                await LoadSemesterData(CurrentSemester.Semester.SemesterId);
                CurrentSemester.IsSelected = true;
            }

            IsBulletinAvailable = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing bulletin: {ex.Message}");
            
            // Check if it's a network connectivity issue
            if (await ConnectivityService.Instance.IsNetworkIssue())
            {
                IsNetworkUnavailable = true;
            }
            else
            {
                // If it's not a network issue, it might be another problem
                IsBulletinAvailable = false;
                MainView.ShowNotification("Erreur", 
                    "Impossible de charger le bulletin.", 
                    NotificationType.Error);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ChangeSemester(InternalSemester semester)
    {
        if (semester == null || semester == CurrentSemester)
            return;
        CurrentSemester.IsSelected = false;

        IsLoading = true;
        try
        {
            CurrentSemester = semester;
            await LoadSemesterData(semester.Semester.SemesterId);

            CurrentSemester.IsSelected = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSemesterData(int semesterId)
    {
        // Clear current data
        ClearCurrentData();

        // Fetch new data for the semester
        var root = await _client.FetchDatas(semesterId);
        if (root == null) return;

        // We update the bulletin
        BulletinRoot = root;
        UpdateUnits();
        UpdateResources();
        UpdateSaes();
        UpdateAbsences(SelectedAbsenceSortingType);
        UpdateLatestEvaluations();

        // We load the charts
        ChartsViewModel.UpdateAllCommand.Execute(null);
        
        // we fianlly update all vms
        foreach (var vm in Tabs)
            vm.Update();
    }

    private void ClearCurrentData()
    {
        Units.Clear();
        Resources.Clear();
        Saes.Clear();
        Absences.Clear();
        
        foreach (var vm in Tabs)
            vm.Clear();
    }

    private async Task InitializeClientIfNeeded()
    {
        if (_client != null! && !_client.IsLoggedIn)
        {
            await _client.Login();
            return;
        }

        if (string.IsNullOrEmpty(Settings.Instance.BulletinUsername) ||
            string.IsNullOrEmpty(Settings.Instance.BulletinPassword))
            return;

        _client = new BulletinClient
        {
            Username = Settings.Instance.BulletinUsername,
            Password = SecureStorage.DecryptPassword(Settings.Instance.BulletinPassword)
        };
        await _client.Login();
    }

    private void UpdateUnits()
    {
        foreach (var unit in BulletinRoot.Transcript.TeachingUnits)
            Units.Add(new InternalTeachingUnit(BulletinRoot, unit.Value, unit.Key));
    }

    private void UpdateResources()
    {
        foreach (var resource in BulletinRoot.Transcript.Resources)
            Resources.Add(new InternalResource(resource.Value, this));
    }

    private void UpdateSaes()
    {
        foreach (var sae in BulletinRoot.Transcript.Saes)
            Saes.Add(new InternalSaeDisplay(sae.Value, this));
    }

    private void UpdateAbsences(AbsenceSortingType absenceSortingType)
    {
        // On convertit directement le dictionnaire en utilisant l'extension
        if (BulletinRoot?.Absences != null)
        {
            var filteredAbsences = BulletinRoot.Absences.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Where(abs => abs.Status != "present").ToList()
            );
            
            if (absenceSortingType == AbsenceSortingTypes[1]) // Absences
            {
                filteredAbsences = filteredAbsences
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(abs => abs.Status == "absent").ToList());
            }
            else if (absenceSortingType == AbsenceSortingTypes[2]) // Retards
            {
                filteredAbsences = filteredAbsences
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(abs => abs.Status == "retard").ToList());
            }

            var nonEmptyAbsences = filteredAbsences
                .Where(kvp => kvp.Value.Any())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // On utilise l'extension pour convertir en InternalAbsenceDays
            Absences = nonEmptyAbsences.ToAbsenceDays();
        }
        else
        {
            Absences = new ObservableCollection<InternalAbsenceDay>();
        }
    }
    
    private void UpdateLatestEvaluations()
    {
        
    }

    private bool _lastExpandSaeState = true;
    private bool _lastExpandResourceState = true;

    [RelayCommand]
    public void ToggleExpandSae()
    {
        _lastExpandSaeState = !_lastExpandSaeState;
        foreach (var sae in Saes)
            sae.IsExpanded = _lastExpandSaeState;
    }
    
    [RelayCommand]
    public void ToggleExpandResource()
    {
        _lastExpandResourceState = !_lastExpandResourceState;
        foreach (var resource in Resources)
            resource.IsExpanded = _lastExpandResourceState;
    }

    public static AsyncRelayCommand ConfigureBulletin => new(async () =>
    {
        var dialog = new ContentDialog
        {
            Title = "Connexion au bulletin",
            Content = new BulletinLoginDialog(),
            CloseButtonText = "Fermer"
        };

        await dialog.ShowAsync();
    });
}

public record InternalEvaluation(Evaluation Evaluation, bool IsAboveAverage, string DisplayedDate);

public partial class AbsenceSortingType : ObservableObject
{

    [ObservableProperty] private string _displayedName;
    [ObservableProperty] private Symbol _icon;
    [ObservableProperty] private bool _isSelected;

}

public enum AbsenceType
{
    Late,       // Retard
    Absence     // Absence complète
}

public record InternalAbsence
{
    public InternalAbsence(Absence absence, DateOnly date)
    {
        Absence = absence;
        Date = date;

        // Détermine si c'est un retard ou une absence
        AbsenceType = absence.Status == "retard" ? AbsenceType.Late : AbsenceType.Absence;

        // Génération des affichages
        TimeDisplay = $"{absence.StartTime:hh\\:mm} - {absence.EndTime:hh\\:mm}";
        TextDisplay = AbsenceType == AbsenceType.Late 
            ? $"Retard (cours de {(absence.EndTime - absence.StartTime).TotalMinutes:0} min)"
            : "Absence complète";

        TextColor = absence.IsJustified
            ? ColorMatcher.GreenBrush
            : AbsenceType == AbsenceType.Late
                ? ColorMatcher.AmberBrush
                : ColorMatcher.RedBrush;

        Icon = AbsenceType == AbsenceType.Late
            ? Symbol.Clock 
            : Symbol.PersonQuestionMark;

        // Statut pour le badge
        IsJustified = absence.IsJustified;
        IsNotJustified = !absence.IsJustified;
        
        IsLate = AbsenceType == AbsenceType.Late;
        if (IsLate)
        {
            IsJustified = IsNotJustified = false;
        }

        StatusText = (AbsenceType, absence.IsJustified) switch
        {
            (AbsenceType.Late, _) => "Retard",
            (_, true) => "Justifié(e)",
            (_, false) => "Non justifié(e)"
        };
    }

    public Absence Absence { get; }
    public DateOnly Date { get; }
    public AbsenceType AbsenceType { get; }
    public IBrush TextColor { get; }
    public string TextDisplay { get; }
    public string TimeDisplay { get; }
    public Symbol Icon { get; }
    public bool IsJustified { get; }
    public bool IsNotJustified { get; }
    public bool IsLate { get; }
    public string StatusText { get; }
}

public partial class InternalAbsenceDay : ObservableObject
{
    [ObservableProperty]
    private string dateDisplay;

    [ObservableProperty]
    private ObservableCollection<InternalAbsence> dayAbsences;

    public InternalAbsenceDay(DateOnly date, IEnumerable<Absence> absences)
    {
        DateDisplay = date.ToString("dddd dd MMMM yyyy");
        DayAbsences = new ObservableCollection<InternalAbsence>(
            absences.Select(a => new InternalAbsence(a, date)));
    }
}

// Extensions pour faciliter la conversion depuis le modèle API
public static class AbsenceExtensions
{
    public static ObservableCollection<InternalAbsenceDay> ToAbsenceDays(
        this Dictionary<DateOnly, List<Absence>> absences)
    {
        return new ObservableCollection<InternalAbsenceDay>(
            absences.Select(kvp => 
                new InternalAbsenceDay(kvp.Key, kvp.Value))
            .OrderByDescending(day => day.DayAbsences.First().Date));
    }
}

public partial class InternalSemester : ObservableObject
{
    [ObservableProperty] private Semester _semester;
    [ObservableProperty] private bool _isSelected;
}