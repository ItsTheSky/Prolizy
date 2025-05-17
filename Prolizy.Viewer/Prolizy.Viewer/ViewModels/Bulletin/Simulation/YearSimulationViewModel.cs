using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Prolizy.API.Model;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels.Bulletin.Simulation;

public partial class YearSimulationViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<UnitSimulationItem> _units = new();
    [ObservableProperty] private ObservableCollection<AbsencesSemesterItem> _absencesItems = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private double _loadingProgress;
    [ObservableProperty] private string _loadingStatus = "Initialisation...";
    [ObservableProperty] private bool _yearSuccessful;
    [ObservableProperty] private string _simulationResultMessage = string.Empty;
    [ObservableProperty] private InfoBarSeverity _resultSeverity = InfoBarSeverity.Informational;
    [ObservableProperty] private bool _hasFailedDueToAbsences;
    [ObservableProperty] private bool _hasFailedDueToUnits;
    [ObservableProperty] private bool _includeAbsences;
    
    // Règles pour réussir l'année
    private const double MIN_UE_THRESHOLD = 8.0;
    private const double PASSING_UE_THRESHOLD = 10.0;
    private const int MIN_PASSING_UES = 4;
    private const int MAX_ABSENCES_PER_SEMESTER = 5;
    
    private BulletinPaneViewModel _baseViewModel;
    
    public YearSimulationViewModel(BulletinPaneViewModel baseViewModel, bool includeAbsences)
    {
        _baseViewModel = baseViewModel;
        _includeAbsences = includeAbsences;
    }
    
    public async Task RunSimulation()
    {
        try
        {
            IsLoading = true;
            Units.Clear();
            AbsencesItems.Clear();
            
            // 1. Récupérer tous les semestres de l'année courante
            LoadingStatus = "Récupération des semestres...";
            LoadingProgress = 0.1;
            
            var baseRoot = await _baseViewModel.BulletinClient.FetchDatas();
            var currentYear = DateTime.Now.Year;
            
            var allSemesters = baseRoot.Semesters
                .Where(semester => {
                    var yearStr = semester.AcademicYear.Split('/')[1];
                    return int.TryParse(yearStr, out var year) && year == currentYear;
                })
                .OrderBy(s => s.SemesterNumber)
                .ToList();
            
            if (allSemesters.Count < 2)
            {
                SimulationResultMessage = "Impossible de simuler l'année : moins de 2 semestres trouvés pour l'année en cours.";
                ResultSeverity = InfoBarSeverity.Error;
                IsLoading = false;
                return;
            }
            
            // 2. Récupérer les données pour chaque semestre
            var semesterData = new Dictionary<int, BulletinRoot>();
            var absencesData = new Dictionary<int, List<Absence>>();
            
            for (int i = 0; i < allSemesters.Count; i++)
            {
                var semester = allSemesters[i];
                LoadingStatus = $"Récupération du semestre {semester.SemesterNumber}...";
                LoadingProgress = 0.1 + (0.4 * (i + 1) / allSemesters.Count);
                
                var bulletinRoot = semester.SemesterId == baseRoot.Transcript.SemesterId
                    ? baseRoot
                    : await _baseViewModel.BulletinClient.FetchDatas(semester.SemesterId);
                
                if (bulletinRoot == null) 
                    continue;
                
                semesterData[semester.SemesterNumber] = bulletinRoot;
                
                // Collecter les absences non justifiées pour ce semestre
                var nonJustifiedAbsences = bulletinRoot.Absences
                    .SelectMany(kvp => kvp.Value)
                    .Where(a => !a.IsJustified && a.Status == "absent")
                    .ToList();
                
                absencesData[semester.SemesterNumber] = nonJustifiedAbsences;
            }
            
            // 3. Calculer les absences par semestre
            LoadingStatus = "Calcul des absences...";
            LoadingProgress = 0.6;
            
            var halfDaysBySemester = new Dictionary<int, int>();
            foreach (var semesterItem in absencesData)
            {
                var semesterNumber = semesterItem.Key;
                var absences = semesterItem.Value;
                
                // Calculer le nombre de demi-journées d'absences
                var halfDayCount = CountHalfDays(absences);
                halfDaysBySemester[semesterNumber] = halfDayCount;
                
                // Créer l'élément d'absence pour ce semestre
                var absentItem = new AbsencesSemesterItem
                {
                    SemesterNumber = semesterNumber,
                    AbsencesCount = absences.Count,
                    HalfDayCount = halfDayCount,
                    IsFailing = halfDayCount > MAX_ABSENCES_PER_SEMESTER
                };
                
                AbsencesItems.Add(absentItem);
            }
            
            // 4. Calculer les moyennes par UE et par semestre
            LoadingStatus = "Calcul des moyennes...";
            LoadingProgress = 0.8;
            
            var unitsBySemester = new Dictionary<int, Dictionary<string, TeachingUnit>>();
            var unitValues = new Dictionary<string, UnitSimulationItem>();
            
            foreach (var semesterEntry in semesterData)
            {
                var semesterNumber = semesterEntry.Key;
                var root = semesterEntry.Value;
                
                unitsBySemester[semesterNumber] = root.Transcript.TeachingUnits;
                
                foreach (var unitEntry in root.Transcript.TeachingUnits)
                {
                    var unitKey = unitEntry.Key[^1].ToString();
                    var unit = unitEntry.Value;
                    
                    if (!double.TryParse(unit.Average.Value.Replace(".", ","), out var unitAvg))
                        continue;
                    
                    if (!unitValues.ContainsKey(unitKey))
                    {
                        unitValues[unitKey] = new UnitSimulationItem
                        {
                            UnitId = unitKey,
                            UnitName = unit.Title,
                            SemesterValues = new Dictionary<int, double>()
                        };
                    }
                    
                    unitValues[unitKey].SemesterValues[semesterNumber] = unitAvg;
                }
            }
            
            // 5. Calculer les moyennes annuelles et déterminer le résultat
            LoadingStatus = "Calcul du résultat...";
            LoadingProgress = 0.9;
            
            // Calculer la moyenne annuelle pour chaque UE
            foreach (var unitItem in unitValues.Values)
            {
                double sum = 0;
                int count = 0;
                
                foreach (var semesterValue in unitItem.SemesterValues)
                {
                    sum += semesterValue.Value;
                    count++;
                }
                
                if (count > 0)
                {
                    unitItem.YearAverage = Math.Round(sum / count, 2);
                    unitItem.IsBelowMinimum = unitItem.YearAverage < MIN_UE_THRESHOLD;
                    unitItem.IsAbovePass = unitItem.YearAverage >= PASSING_UE_THRESHOLD;
                    
                    Units.Add(unitItem);
                }
            }
            
            // 6. Déterminer si l'étudiant réussit son année
            var unitsAboveMin = Units.Count(u => u.YearAverage >= MIN_UE_THRESHOLD);
            var unitsAbovePass = Units.Count(u => u.YearAverage >= PASSING_UE_THRESHOLD);
            
            bool failedDueToUnits = (unitsAboveMin < Units.Count) || (unitsAbovePass < MIN_PASSING_UES);
            
            bool failedDueToAbsences = false;
            if (_includeAbsences)
            {
                failedDueToAbsences = AbsencesItems.Any(a => a.IsFailing);
            }
            
            HasFailedDueToUnits = failedDueToUnits;
            HasFailedDueToAbsences = failedDueToAbsences;
            
            YearSuccessful = !failedDueToUnits && !failedDueToAbsences;
            
            // Créer le message de résultat
            GenerateResultMessage(failedDueToUnits, failedDueToAbsences, unitsAboveMin, unitsAbovePass);
            
            LoadingProgress = 1.0;
        }
        catch (Exception ex)
        {
            SimulationResultMessage = $"Une erreur est survenue lors de la simulation : {ex.Message}";
            ResultSeverity = InfoBarSeverity.Error;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private int CountHalfDays(List<Absence> absences)
    {
        // Une demi-journée étant de 0h à 12h ou de 12h à 24h
        var distinctMornings = absences
            .Where(abs => abs.StartTime.Hours < 12)
            .Select(abs => DateOnly.FromDateTime(DateTime.Today))  // Utiliser une date arbitraire car seule l'heure compte
            .Distinct()
            .Count();
            
        var distinctAfternoons = absences
            .Where(abs => abs.StartTime.Hours >= 12)
            .Select(abs => DateOnly.FromDateTime(DateTime.Today))  // Utiliser une date arbitraire car seule l'heure compte
            .Distinct()
            .Count();
            
        return distinctMornings + distinctAfternoons;
    }
    
    private void GenerateResultMessage(bool failedDueToUnits, bool failedDueToAbsences, int unitsAboveMin, int unitsAbovePass)
    {
        if (!failedDueToUnits && !failedDueToAbsences)
        {
            SimulationResultMessage = "Félicitations ! Selon la simulation, vous réussirez votre année.";
            ResultSeverity = InfoBarSeverity.Success;
            return;
        }
        
        var reasons = new List<string>();
        
        if (failedDueToUnits)
        {
            var belowMinUnits = Units.Count - unitsAboveMin;
            if (belowMinUnits > 0)
            {
                reasons.Add($"vous avez {belowMinUnits} UE{(belowMinUnits > 1 ? "s" : "")} en dessous du minimum de {MIN_UE_THRESHOLD}/20");
            }
            
            var missingPassUnits = MIN_PASSING_UES - unitsAbovePass;
            if (missingPassUnits > 0)
            {
                reasons.Add($"vous n'avez que {unitsAbovePass} UE{(unitsAbovePass > 1 ? "s" : "")} au-dessus de {PASSING_UE_THRESHOLD}/20 (il en faut au moins {MIN_PASSING_UES})");
            }
        }
        
        if (failedDueToAbsences)
        {
            var failingSemesters = AbsencesItems.Where(a => a.IsFailing).ToList();
            if (failingSemesters.Count > 0)
            {
                var semesterText = string.Join(" et ", failingSemesters.Select(s => $"S{s.SemesterNumber} ({s.HalfDayCount} demi-journées)"));
                reasons.Add($"vous dépassez le maximum de {MAX_ABSENCES_PER_SEMESTER} demi-journées d'absences non justifiées au(x) semestre(s) {semesterText}");
            }
        }
        
        SimulationResultMessage = $"Attention ! Selon la simulation, vous ne réussirez pas votre année car {string.Join(" et ", reasons)}.";
        ResultSeverity = InfoBarSeverity.Error;
    }
}

public partial class UnitSimulationItem : ObservableObject
{
    [ObservableProperty] private string _unitId;
    [ObservableProperty] private string _unitName;
    [ObservableProperty] private Dictionary<int, double> _semesterValues = new();
    [ObservableProperty] private double _yearAverage;
    [ObservableProperty] private bool _isBelowMinimum;
    [ObservableProperty] private bool _isAbovePass;
    
    public string DisplayedUnitId => UnitId;
    
    public string DisplayedS1 => SemesterValues.TryGetValue(1, out var s1) ? $"{s1:F2}" : "N/A";
    
    public string DisplayedS2 => SemesterValues.TryGetValue(2, out var s2) ? $"{s2:F2}" : "N/A";
    
    public string DisplayedYear => YearAverage > 0 ? $"{YearAverage:F2}" : "N/A";
    
    // S'assurer que les propriétés calculées sont mises à jour quand les propriétés sous-jacentes changent
    partial void OnYearAverageChanged(double value) => OnPropertyChanged(nameof(DisplayedYear));
    partial void OnSemesterValuesChanged(Dictionary<int, double> value)
    {
        OnPropertyChanged(nameof(DisplayedS1));
        OnPropertyChanged(nameof(DisplayedS2));
        OnPropertyChanged(nameof(DisplayedYear));
    }
}

public partial class AbsencesSemesterItem : ObservableObject
{
    [ObservableProperty] private int _semesterNumber;
    [ObservableProperty] private int _absencesCount;
    [ObservableProperty] private int _halfDayCount;
    [ObservableProperty] private bool _isFailing;
}