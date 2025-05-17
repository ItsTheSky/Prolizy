using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentIcons.Common;
using Prolizy.API.Model;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels.Bulletin.Simulation;
using Symbol = FluentIcons.Common.Symbol;

namespace Prolizy.Viewer.ViewModels.Bulletin.Tabs;

public partial class AbsencesTabViewModel : BaseBulletinTabViewModel
{
    [ObservableProperty] private ObservableCollection<InternalAbsenceDay> _absences = [];

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

    [ObservableProperty] private int _halfDayJustifiedCount;
    [ObservableProperty] private int _halfDayNotJustifiedCount;
    [ObservableProperty] private int _halfDayCount;
    [ObservableProperty] private int _retardsCount;

    public AbsencesTabViewModel(BulletinPaneViewModel baseVm) : base(baseVm)
    {
        SelectedAbsenceSortingType = AbsenceSortingTypes[0];
        PropertyChanged += (source, args) =>
        {
            if (args.PropertyName == nameof(SelectedAbsenceSortingType))
            {
                foreach (var otherSortingType in AbsenceSortingTypes)
                {
                    otherSortingType.IsSelected = otherSortingType == SelectedAbsenceSortingType;
                }

                UpdateAbsences();
            }
        };
    }

    public override void Update()
    {
        UpdateAbsences();
    }

    public override void Clear()
    {
        Absences.Clear();
    }

    private void UpdateAbsences()
    {
        if (BaseViewModel.BulletinRoot?.Absences == null)
        {
            Absences = new ObservableCollection<InternalAbsenceDay>();
            return;
        }

        // On filtre les absences selon le type sélectionné
        var baseAbsences = BaseViewModel.BulletinRoot.Absences.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Where(abs => abs.Status != "present").ToList()
        );

        var filteredAbsences = baseAbsences
            .Where(kvp => kvp.Value.Any())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        if (SelectedAbsenceSortingType == AbsenceSortingTypes[1]) // Absences
        {
            filteredAbsences = filteredAbsences
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(abs => abs.Status == "absent").ToList());
        }
        else if (SelectedAbsenceSortingType == AbsenceSortingTypes[2]) // Retards
        {
            filteredAbsences = filteredAbsences
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(abs => abs.Status == "retard").ToList());
        }
        
        var nonEmptyAbsences = filteredAbsences
            .Where(kvp => kvp.Value.Count != 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Absences = nonEmptyAbsences
            .ToAbsenceDays();

        RetardsCount = baseAbsences.SelectMany(kv => kv.Value).Count(abs => abs.Status == "retard");

        var absencesList = baseAbsences.ToAbsenceDays()
            .SelectMany(day => day.DayAbsences)
            .Select(abs => abs.Absence)
            .ToList();

        var scannedJustifiedHalfDaysData = new Dictionary<string, List<AbsenceDayPart>>();
        var scannedNotJustifiedHalfDaysData = new Dictionary<string, List<AbsenceDayPart>>();

        // On compte les demi journées justifiées et non justifiées
        // deux absences la même après-midi ne compte qu'une
        // demi-journée (une demie journée étant de 0h à 12h ou de 12h à 24h)
        HalfDayJustifiedCount = 0;
        foreach (var absence in absencesList.Where(abs => abs is { IsJustified: true, Status: "absent" }))
        {
            var dayPart = absence.StartTime.Hours < 12 
                ? AbsenceDayPart.Morning
                : AbsenceDayPart.Afternoon;
            
            if (scannedJustifiedHalfDaysData.ContainsKey(absence.EndDate))
            {
                if (!scannedJustifiedHalfDaysData[absence.EndDate].Contains(dayPart))
                {
                    HalfDayJustifiedCount++;
                    scannedJustifiedHalfDaysData[absence.EndDate].Add(dayPart);
                }
            }
            else
            {
                HalfDayJustifiedCount++;
                scannedJustifiedHalfDaysData.Add(absence.EndDate, [dayPart]);
            }
        }
        
        HalfDayNotJustifiedCount = 0;
        foreach (var absence in absencesList.Where(abs => abs is { IsJustified: false, Status: "absent" }))
        {
            var dayPart = absence.StartTime.Hours < 12 
                ? AbsenceDayPart.Morning
                : AbsenceDayPart.Afternoon;
            
            if (scannedNotJustifiedHalfDaysData.ContainsKey(absence.EndDate))
            {
                if (!scannedNotJustifiedHalfDaysData[absence.EndDate].Contains(dayPart))
                {
                    HalfDayNotJustifiedCount++;
                    scannedNotJustifiedHalfDaysData[absence.EndDate].Add(dayPart);
                }
            }
            else
            {
                HalfDayNotJustifiedCount++;
                scannedNotJustifiedHalfDaysData.Add(absence.EndDate, [dayPart]);
            }
        }

        HalfDayCount = HalfDayJustifiedCount + HalfDayNotJustifiedCount;
    }

    #region Simulations

    [RelayCommand]
    public async Task SimulateYear(bool simulateAbsences)
    {
        // Créer le ViewModel pour la simulation
        var simulationViewModel = new Simulation.YearSimulationViewModel(BaseViewModel, simulateAbsences);

        // Créer la vue pour afficher les résultats
        var simulationView = new Controls.Bulletin.Simulation.YearSimulationView(simulationViewModel);

        // Créer la boîte de dialogue
        var dialog = new FluentAvalonia.UI.Controls.ContentDialog
        {
            Title = "Simulation de réussite de l'année",
            Content = simulationView,
            CloseButtonText = "Fermer",
            DefaultButton = FluentAvalonia.UI.Controls.ContentDialogButton.Close
        };

        // Lancer la simulation en arrière-plan
        _ = Task.Run(async () => { await simulationViewModel.RunSimulation(); });

        // Afficher la boîte de dialogue
        await dialog.ShowAsync();
    }

    #endregion
}

public enum AbsenceDayPart
{
    Morning,
    Afternoon
}

public partial class AbsenceSortingType : ObservableObject
{
    [ObservableProperty] private string _displayedName;
    [ObservableProperty] private Symbol _icon;
    [ObservableProperty] private bool _isSelected;
}

public enum AbsenceType
{
    Late, // Retard
    Absence // Absence complète
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
    [ObservableProperty] private string dateDisplay;

    [ObservableProperty] private ObservableCollection<InternalAbsence> dayAbsences;

    public InternalAbsenceDay(DateOnly date, IEnumerable<Absence> absences)
    {
        DateDisplay = date.ToString("dddd dd MMMM yyyy");
        DayAbsences = new ObservableCollection<InternalAbsence>(
            absences.Select(a => new InternalAbsence(a, date)));
    }
}

public static class AbsenceExtensions
{
    public static ObservableCollection<InternalAbsenceDay> ToAbsenceDays(
        this Dictionary<DateOnly, List<Absence>> absences)
    {
        return new ObservableCollection<InternalAbsenceDay>(
            absences.Select(kvp => new InternalAbsenceDay(kvp.Key, kvp.Value))
                .Where(day => day.DayAbsences.Any())
                .OrderByDescending(day => day.DayAbsences.First().Absence.StartTime));
    }
}