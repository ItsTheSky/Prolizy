using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Prolizy.API.Model;
using Prolizy.Viewer.Utilities;

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
            .Where(kvp => kvp.Value.Any())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        Absences = nonEmptyAbsences.ToAbsenceDays();

        // On compte les demi journées justifiées et non justifiées
        // deux absences la même après-midi ne compte qu'une
        // demi-journée (une demie journée étant de 0h à 12h ou de 12h à 24h)
        HalfDayJustifiedCount = baseAbsences
            .SelectMany(kvp => kvp.Value)
            .Count(abs => abs.IsJustified &&
                          (abs.StartTime.Hours < 12 && abs.EndTime.Hours >= 12 ||
                           abs.StartTime.Hours >= 12 && abs.EndTime.Hours < 24));

        HalfDayNotJustifiedCount = baseAbsences
            .SelectMany(kvp => kvp.Value)
            .Count(abs => !abs.IsJustified &&
                          (abs.StartTime.Hours < 12 && abs.EndTime.Hours >= 12 ||
                           abs.StartTime.Hours >= 12 && abs.EndTime.Hours < 24));

        HalfDayCount = HalfDayJustifiedCount + HalfDayNotJustifiedCount;
    }

    #region Simulations

    [RelayCommand]
    public async Task SimulateYear(bool simulateAbsences)
    {
        Console.WriteLine("Simulating year... with absences: " + simulateAbsences);
        var currentYear = DateTime.Now.Year;

        var baseRoot = await BaseViewModel.BulletinClient.FetchDatas();
        var semesters = baseRoot.Semesters
            .Where(semester => int.Parse(semester.AcademicYear.Split("/")[1]) == currentYear)
            .Select(semester => semester.SemesterId);
        //.Where(id => id != baseRoot.Transcript.SemesterId);

        var units = new Dictionary<string, TeachingUnit>();
        var absences = new List<Absence>();

        Console.WriteLine("Fetching semesters...");
        foreach (var semesterId in semesters)
        {
            var bulletinRoot = semesterId == baseRoot.Transcript.SemesterId
                ? baseRoot
                : await BaseViewModel.BulletinClient.FetchDatas(semesterId);
            Console.WriteLine("   -> Fetched semester: " + semesterId + " successfully (was base? " +
                              (semesterId == baseRoot.Transcript.SemesterId) + ")");
            
            if (bulletinRoot == null)
            {
                Console.WriteLine("Failed to fetch bulletin root for semester: " + semesterId);
                continue;
            }

            foreach (var unit in bulletinRoot.Transcript.TeachingUnits)
                units.Add(unit.Key, unit.Value);

            foreach (var abs in bulletinRoot.Absences.SelectMany(kvp => kvp.Value))
                if (abs is { IsJustified: false, Status: "absent" })
                    absences.Add(abs);
        }

        Console.WriteLine("Fetched " + units.Count + " units and " + absences.Count +
                          " absences in total. Applying base calculations...");

        // Calcul des moyennes.
        // Clé: ID (= numéro) de l'UE, Valeur: Moyenne entre les (deux, ou plus) semestres
        var allSemestersUnitAvg = new Dictionary<int, (int, double)>();

        foreach (var unitPair in units)
        {
            var unit = unitPair.Value;
            if (!double.TryParse(unit.Average.Value.Replace(".", ","), out var avg))
            {
                Console.WriteLine("Unit " + unit.Title + " ["+ unitPair.Key +"] has no average. (found '" + unit.Average.Value + "')");
                continue;
            }
            
            var unitNumber = int.Parse(unitPair.Key[^1].ToString());

            if (allSemestersUnitAvg.ContainsKey(unitNumber))
            {
                var (count, sum) = allSemestersUnitAvg[unitNumber];
                allSemestersUnitAvg[unitNumber] = (count + 1, sum + avg);
            }
            else
            {
                allSemestersUnitAvg[unitNumber] = (1, avg);
            }
        }

        // Calcul de la moyenne générale par UE
        var allUnitsAvg = new Dictionary<int, double>();
        foreach (var unit in allSemestersUnitAvg)
        {
            var (count, sum) = unit.Value;
            allUnitsAvg[unit.Key] = sum / count;
        }

        foreach (var unit in allUnitsAvg)
        {
            var unitId = unit.Key;
            var avg = unit.Value;

            Console.WriteLine("Unit " + unitId + " has an average of " + avg + " in the year.");
        }
    }

    #endregion
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
            absences.Select(kvp =>
                    new InternalAbsenceDay(kvp.Key, kvp.Value))
                .OrderByDescending(day => day.DayAbsences.First().Date));
    }
}