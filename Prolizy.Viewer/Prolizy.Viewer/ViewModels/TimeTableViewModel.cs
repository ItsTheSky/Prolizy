using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.API;
using Prolizy.Viewer.Controls.Edt;
using Prolizy.Viewer.Controls.Wizard.Steps;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.Views;
using Prolizy.Viewer.Views.Panes;
using Calendar = Avalonia.Controls.Calendar;

namespace Prolizy.Viewer.ViewModels;

public partial class TimeTableViewModel : ObservableObject
{
    [ObservableProperty] private bool _isDisplayList = false;

    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly SelectedDate
    {
        get => _selectedDate;
        set
        {
            SetProperty(ref _selectedDate, value);
            FormattedDate = value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

            OnPropertyChanged(nameof(DisplayNextDay));
            OnPropertyChanged(nameof(DisplayPreviousDay));
            OnPropertyChanged(nameof(DisplayCurrentDay));
        }
    }

    public DateOnly NextDay => SelectedDate.AddDays(1);
    public DateOnly PreviousDay => SelectedDate.AddDays(-1);

    public string DisplayNextDay => NextDay.ToString("ddd dd MMM", CultureInfo.CurrentCulture);
    public string DisplayPreviousDay => PreviousDay.ToString("ddd dd MMM", CultureInfo.CurrentCulture);
    public string DisplayCurrentDay => SelectedDate.ToString("ddd dd MMM", CultureInfo.CurrentCulture);

    [ObservableProperty] private string _formattedDate = DateTime.Now.ToString("DD/MM/YYYY");
    [ObservableProperty] private bool _isEdtAvailable;

    private readonly Dictionary<DateOnly, List<ScheduleItem>> _scheduleCache = new();
    private readonly TimeTablePane _timeTablePane;

    /// <inheritdoc/>
    public TimeTableViewModel(TimeTablePane timeTablePane)
    {
        _timeTablePane = timeTablePane;
        IsEdtAvailable = !string.IsNullOrEmpty(Settings.Instance.StudentGroup);
    }

    [RelayCommand]
    public async Task GoToNextDay()
    {
        SelectedDate = SelectedDate.AddDays(1);
        await GoToDay();
    }

    [RelayCommand]
    public async Task GoToPreviousDay()
    {
        SelectedDate = SelectedDate.AddDays(-1);
        await GoToDay();
    }

    [RelayCommand]
    public async Task GoToToday()
    {
        SelectedDate = DateOnly.FromDateTime(DateTime.Today);
        await GoToDay();
    }

    [RelayCommand]
    public void DoNothing()
    {
        // Do nothing
    }

    [RelayCommand]
    public async Task ChangeDate()
    {
        var datePicker = new Calendar
        {
            SelectedDate = SelectedDate.ToDateTime(new TimeOnly(0, 0)),
        };
        var dialog = new ContentDialog
        {
            Title = "Changer la Date",
            Content = datePicker,
            PrimaryButtonText = "Ok",
            CloseButtonText = "Fermer"
        };
        dialog.Loaded += (sender, args) =>
            dialog.SetButtonClasses(ContentDialogExtensions.ButtonType.Primary, "success");

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            SelectedDate = DateOnly.FromDateTime(datePicker.SelectedDate!.Value);
            await GoToDay();
        }
    }

    public async Task GoToDay()
    {
        try
        {
            await UpdateAndroidWidget();

            if (_scheduleCache.ContainsKey(SelectedDate))
            {
                _timeTablePane.UpdateItems(_scheduleCache[SelectedDate]);
                await HomePane.UpdateCards("edt");
                Console.WriteLine("Today's schedule has been loaded from cache.");
                return;
            }

            Console.WriteLine("Loading schedule...");

            // Obtenir le lundi de la semaine courante
            var monday = SelectedDate.AddDays(-(int)SelectedDate.DayOfWeek + 1);
            if (SelectedDate.DayOfWeek == DayOfWeek.Sunday) // Si on est dimanche, prendre le lundi de la semaine précédente
                monday = monday.AddDays(-7);

            // Charger du lundi au dimanche (+1 jour après la date sélectionnée)
            var start = monday;
            var end = SelectedDate.AddDays(1);

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (!_scheduleCache.ContainsKey(date))
                {
                    var items = await LoadCoursesForDate(date);
                    _scheduleCache[date] = items;
                    Console.WriteLine($"Added {items.Count} courses to cache for {date}");
                }
            }

            _timeTablePane.UpdateItems(_scheduleCache[SelectedDate]);
            
            await HomePane.UpdateCards("edt");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            MainView.ShowNotification("Erreur", "Impossible de charger l'emploi du temps.", NotificationType.Error);
        }
    }

    public void RefreshAll()
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _scheduleCache.Clear();
            await GoToDay();
        });
    }

    [RelayCommand]
    public async Task ConfigureEdt()
    {
        var dialog = new ContentDialog
        {
            Title = "Configurer l'emploi du temps",
            Content = new EdtGroupStep(),
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel"
        };
        dialog.Loaded += (sender, args) =>
            dialog.SetButtonClasses(ContentDialogExtensions.ButtonType.Primary, "success");

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            IsEdtAvailable = !string.IsNullOrEmpty(Settings.Instance.StudentGroup);
            RefreshAll();
        }
    }

    public async Task UpdateAndroidWidget()
    {
        var (current, isCurrent) = await GetCurrentOrNextCourse();
        if (AndroidAccessManager.AndroidAccess == null)
        {
#if ANDROID
        MainView.ShowNotification("Erreur", "Impossible de mettre à jour le widget Android.", NotificationType.Error);
#endif
            return;
        }

        if (current == null)
        {
            Console.WriteLine("No course found for the widget.");
            return;
        }

        AndroidAccessManager.AndroidAccess.UpdateWidget(current, isCurrent);
    }

    private const int MaxDays = 20;

    public static async Task<(ScheduleItem? current, bool isCurrent)> GetCurrentOrNextCourse()
    {
        var now = DateTime.Now;
        var currentDate = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        // Try to get the ViewModel if available
        TimeTableViewModel? viewModel = TimeTablePane.Instance?.ViewModel;

        // We'll try for MaxDays days
        for (int dayOffset = 0; dayOffset < MaxDays; dayOffset++)
        {
            var targetDate = currentDate.AddDays(dayOffset);
            List<ScheduleItem> daySchedule;

            // Try to get schedule from cache if ViewModel exists
            if (viewModel?._scheduleCache.TryGetValue(targetDate, out daySchedule) == true)
            {
                // Cache hit
                Console.WriteLine($"Using cached schedule for {targetDate}");
            }
            else
            {
                // No cache or cache miss - load from API
                Console.WriteLine($"Loading schedule for {targetDate} from API");
                daySchedule = await LoadCoursesForDate(targetDate);
            }

            if (daySchedule.Count == 0)
            {
                Console.WriteLine($"No courses found for {targetDate}");
                continue;
            }

            // Sort schedule by start time
            daySchedule = daySchedule.OrderBy(x => x.StartTime).ToList();

            if (targetDate == currentDate)
            {
                // Check for current course first
                var currentCourse = daySchedule.FirstOrDefault(item =>
                    TimeOnly.FromDateTime(item.StartTime) <= currentTime &&
                    TimeOnly.FromDateTime(item.EndTime) > currentTime);

                if (currentCourse != null)
                {
                    Console.WriteLine($"Found current course: {currentCourse.Subject} at {currentCourse.StartTime}");
                    return (currentCourse, true);
                }

                // If no current course, find next course today
                var nextCourse = daySchedule.FirstOrDefault(item =>
                    TimeOnly.FromDateTime(item.StartTime) > currentTime);

                if (nextCourse != null)
                {
                    Console.WriteLine($"Found next course today: {nextCourse.Subject} at {nextCourse.StartTime}");
                    return (nextCourse, false);
                }
            }
            else
            {
                // For future dates, just take the first course
                if (daySchedule.Count > 0)
                {
                    var nextCourse = daySchedule[0];
                    Console.WriteLine(
                        $"Found next course on {targetDate}: {nextCourse.Subject} at {nextCourse.StartTime}");
                    return (nextCourse, false);
                }
            }
        }

        Console.WriteLine("No courses found within the next 20 days");
        return (null, false);
    }

    private static async Task<List<ScheduleItem>> LoadCoursesForDate(DateOnly date)
    {
        var items = new List<ScheduleItem>();
        var group = Settings.Instance.StudentGroup;
        if (string.IsNullOrEmpty(group))
            return items;

        try
        {
            var schedule = await EDTClient.GetCourses(new CalendarRequest
            {
                StartDate = date,
                EndDate = date,
                FederationId = group,
                ViewType = CalendarRequest.CalendarViewType.AgendaWeek,
                ColourScheme = Settings.Instance.ColorScheme.ToString()
            });

            foreach (var course in schedule)
            {
                var description = await course.GetDescription(!Settings.Instance.BetterDescription);
                var overlay = null as ScheduleItemOverlay;

                // Check bulletin if overlay is enabled
                if (Settings.Instance.Overlay)
                {
                    var bulletinVm = BulletinPane.Instance.ViewModel;
                    if (Settings.Instance.LinkEdt && bulletinVm.IsBulletinAvailable)
                    {
                        var absencesDay = bulletinVm.Absences;
                        var absences = absencesDay
                            .FirstOrDefault(a => a.DayAbsences[0].Date == DateOnly.FromDateTime(course.Start.Date))
                            ?.DayAbsences;

                        if (absences != null)
                        {
                            var absence = absences.FirstOrDefault(a =>
                                a.Date == DateOnly.FromDateTime(course.Start.Date)
                                && new TimeOnly(a.Absence.StartTime.Ticks) == TimeOnly.FromDateTime(course.Start)
                                && new TimeOnly(a.Absence.EndTime.Ticks) == TimeOnly.FromDateTime(course.End));

                            if (absence != null)
                            {
                                overlay = absence.Absence.Status == "retard"
                                    ? new ScheduleItemOverlay(ColorMatcher.OrangeBrush, Brushes.Yellow, "Retard")
                                    : new ScheduleItemOverlay(ColorMatcher.RedBrush, Brushes.OrangeRed, "Absent");
                            }
                        }
                    }

                    if (overlay == null && DateOnly.FromDateTime(course.End) <= DateOnly.FromDateTime(DateTime.Now))
                    {
                        var current = TimeOnly.FromDateTime(DateTime.Now);
                        var courseTime = TimeOnly.FromDateTime(course.End);

                        if (DateOnly.FromDateTime(course.End) != DateOnly.FromDateTime(DateTime.Now) ||
                            current > courseTime)
                            overlay = new ScheduleItemOverlay(ColorMatcher.GrayBrush, Brushes.Azure, "Passé");
                    }
                }

                items.Add(new ScheduleItem
                {
                    StartTime = course.Start,
                    EndTime = course.End,
                    Group = string.Join(", ", description.Groups),
                    Subject = string.Join(", ", description.Subjects),
                    Room = string.Join(", ", description.Rooms),
                    Professor = string.Join(", ", description.Professors),
                    Overlay = overlay,
                    BackgroundColor = ColorMatcher.FindClosestColor(course.BackgroundColor),
                    ForegroundColor = Colors.White,
                    BorderColor = Colors.Gray,
                    Course = course,
                    Type = course.CourseType
                });
            }
        }
        catch (Exception)
        {
            return items;
        }

        return items;
    }
}