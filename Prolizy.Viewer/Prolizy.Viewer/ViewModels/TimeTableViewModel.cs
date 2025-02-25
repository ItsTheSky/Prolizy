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
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private bool _isNetworkUnavailable = false;

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
        
        // Subscribe to connectivity changes
        ConnectivityService.Instance.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ConnectivityService.Instance.IsNetworkAvailable))
            {
                UpdateNetworkStatus();
            }
        };
        
        // Initialize network status
        UpdateNetworkStatus();
    }
    
    private void UpdateNetworkStatus()
    {
        // Only set IsNetworkUnavailable to true if we're connected to internet
        // and the EDT is properly configured
        IsNetworkUnavailable = !ConnectivityService.Instance.IsNetworkAvailable && IsEdtAvailable;
    }

    [RelayCommand]
    public async Task GoToNextDay()
    {
        if (await CheckNetworkAvailability())
        {
            SelectedDate = SelectedDate.AddDays(1);
            await GoToDay();
        }
    }

    [RelayCommand]
    public async Task GoToPreviousDay()
    {
        if (await CheckNetworkAvailability())
        {
            SelectedDate = SelectedDate.AddDays(-1);
            await GoToDay();
        }
    }

    [RelayCommand]
    public async Task GoToToday()
    {
        if (await CheckNetworkAvailability())
        {
            SelectedDate = DateOnly.FromDateTime(DateTime.Today);
            await GoToDay();
        }
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
                await GoToDay();
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

    [RelayCommand]
    public void DoNothing()
    {
        // Do nothing
    }

    [RelayCommand]
    public async Task ChangeDate()
    {
        if (!await CheckNetworkAvailability())
            return;
            
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
    
    private async Task<bool> CheckNetworkAvailability()
    {
        if (!ConnectivityService.Instance.IsNetworkAvailable)
        {
            IsNetworkUnavailable = true;
            return false;
        }
        
        return true;
    }

    public async Task GoToDay()
    {
        if (!await CheckNetworkAvailability())
            return;
            
        try
        {
            IsLoading = true;
            IsNetworkUnavailable = false;
            await UpdateAndroidWidget();

            if (_scheduleCache.ContainsKey(SelectedDate))
            {
                _timeTablePane.UpdateItems(_scheduleCache[SelectedDate]);
                await HomePane.UpdateCards("edt");
                Console.WriteLine("Today's schedule has been loaded from cache.");
                IsLoading = false;
                return;
            }

            // Obtenir le lundi de la semaine courante
            var monday = SelectedDate.AddDays(-(int)SelectedDate.DayOfWeek + 1);
            // si on est samedi ou dimanche, on récupère le lundi de la semaine prochaine
            if (SelectedDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                monday = SelectedDate.AddDays(1 - (int)SelectedDate.DayOfWeek + 1);

            var start = monday;
            var end = monday.AddDays(6);

            Console.WriteLine("Loading schedule... from {0} to {1}", start, end);

            // Load the entire week at once instead of day by day
            var weekSchedule = await LoadCoursesForDateRange(start, end);

            // Add all days to cache
            foreach (var (date, courses) in weekSchedule)
            {
                _scheduleCache[date] = courses;
                Console.WriteLine($"Added {courses.Count} courses to cache for {date}");
            }

            // Update UI with the selected date's courses
            if (_scheduleCache.TryGetValue(SelectedDate, out var items))
            {
                _timeTablePane.UpdateItems(items);
            }
            else
            {
                _timeTablePane.UpdateItems(new List<ScheduleItem>());
            }

            await HomePane.UpdateCards("edt");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            
            // Check if it's a network connectivity issue
            if (await ConnectivityService.Instance.IsNetworkIssue())
            {
                IsNetworkUnavailable = true;
            }
            else
            {
                MainView.ShowNotification("Erreur", "Impossible de charger l'emploi du temps.", NotificationType.Error);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RefreshAll()
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            IsLoading = true;
            try
            {
                // Check network availability first
                if (!await CheckNetworkAvailability())
                    return;
                
                _scheduleCache.Clear();
                await GoToDay();
            }
            finally
            {
                IsLoading = false;
            }
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

        try
        {
            // Check network availability first
            if (!ConnectivityService.Instance.IsNetworkAvailable)
            {
                return (null, false);
            }
            
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
                        Console.WriteLine(
                            $"Found current course: {currentCourse.Subject} at {currentCourse.StartTime}");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding course: {ex.Message}");
            return (null, false);
        }
        finally
        {
            // Reset loading state if we can access the view model
            if (TimeTablePane.Instance?.ViewModel != null)
            {
                TimeTablePane.Instance.ViewModel.IsLoading = false;
            }
        }
    }

    // New method to load courses for a date range (e.g., a week)
    private static async Task<Dictionary<DateOnly, List<ScheduleItem>>> LoadCoursesForDateRange(DateOnly startDate,
        DateOnly endDate)
    {
        var result = new Dictionary<DateOnly, List<ScheduleItem>>();
        var group = Settings.Instance.StudentGroup;
        if (string.IsNullOrEmpty(group))
            return result;

        try
        {
            // Single API request for the entire date range
            var schedule = await EDTClient.GetCourses(new CalendarRequest
            {
                StartDate = startDate,
                EndDate = endDate, // Now we're requesting the whole week
                FederationId = group,
                ViewType = CalendarRequest.CalendarViewType.AgendaWeek,
                ColourScheme = Settings.Instance.ColorScheme.ToString()
            });

            // Process each course and organize by date
            foreach (var course in schedule)
            {
                var courseDate = DateOnly.FromDateTime(course.Start.Date);
                var description = await course.GetDescription(!Settings.Instance.BetterDescription);
                var overlay = null as ScheduleItemOverlay;

                // Check bulletin if overlay is enabled
                if (Settings.Instance.Overlay)
                {
                    var bulletinVm = BulletinPane.Instance?.ViewModel;
                    if (Settings.Instance.LinkEdt && bulletinVm != null && bulletinVm.IsBulletinAvailable)
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

                var scheduleItem = new ScheduleItem
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
                };

                // Add to the appropriate day in our result dictionary
                if (!result.ContainsKey(courseDate))
                {
                    result[courseDate] = new List<ScheduleItem>();
                }

                result[courseDate].Add(scheduleItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading courses for date range: {ex.Message}");
            
            // Check if it's a network issue
            if (await ConnectivityService.Instance.IsNetworkIssue())
            {
                throw new Exception("Network connectivity issue: Unable to load courses", ex);
            }
            
            return result;
        }

        return result;
    }

    // Keep the original method as a wrapper for compatibility with existing code
    private static async Task<List<ScheduleItem>> LoadCoursesForDate(DateOnly date)
    {
        var result = await LoadCoursesForDateRange(date, date);
        return result.TryGetValue(date, out var items) ? items : new List<ScheduleItem>();
    }
}