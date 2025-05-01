using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using Prolizy.Viewer.Controls.Edt;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Views.Panes;

public partial class TimeTablePane : UserControl
{
    public static TimeTablePane Instance { get; private set; } 
    public TimeTablePane()
    {
        InitializeComponent();
        Instance = this;
        
        DataContext = new TimeTableViewModel(this)
        {
            IsDisplayList = Settings.Instance.ShowAsList
        };
        Settings.Instance.PropertyChanged += (sender, args) =>
        {
            switch (args.PropertyName)
            {
                case nameof(Settings.ShowAsList):
                    ViewModel.IsDisplayList = Settings.Instance.ShowAsList;
                    break;
                case nameof(Settings.ColorScheme):
                case nameof(Settings.Overlay):
                    ViewModel.RefreshAll();
                    break;
            }
        };
        
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await ViewModel.GoToToday();
            await ViewModel.UpdateAndroidWidget();
        });
        
        // Make a timer to Update the android widget every minute
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        
        timer.Tick += (sender, args) => Dispatcher.UIThread.InvokeAsync(async () => await ViewModel.UpdateAndroidWidget());
        timer.Start();
        _ = Dispatcher.UIThread.InvokeAsync(async () => await ViewModel.UpdateAndroidWidget());
    }
    
    public TimeTableViewModel ViewModel => (DataContext as TimeTableViewModel)!;
    
    public void UpdateItems(List<ScheduleItem> items)
    {
        DailyControl.Items = items;
        ListControl.Items = items;
        
        DailyControl.UpdateVisual();
        ListControl.UpdateVisual();
    }

    private void RefreshContainer_OnRefreshRequested(object? sender, RefreshRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        ViewModel.RefreshAll();
        deferral.Complete();
    }
}