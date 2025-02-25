using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using HarfBuzzSharp;
using Prolizy.Viewer.Controls;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views.Panes;
using Prolizy.Viewer.Views.SettingsMenu;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;

namespace Prolizy.Viewer.Views;

public partial class MainView : UserControl
{
    public static MainView Instance { get; private set; }
    private static bool lastPaneWasSettings;
    private readonly Dictionary<Type, NavigationViewItem> NavigationItemsDict = new();
    
    public IStorageProvider StorageProvider { get; private set; }
    public IClipboard Clipboard { get; private set; }
    
    public INotificationManager NotificationManager { get; private set; }
    public MainView()
    {
        InitializeComponent();

        ColorSchemeManager.ApplyTheme(Settings.Instance.ThemeScheme);
        
        Console.WriteLine("DATA FOLDER: " + Paths.Build() + " (exists: " + Directory.Exists(Paths.Build()) + ")");
        try
        { 
            Instance = this;

            #region Main Navigation Setup

            MainNavigationView.SelectionChanged += (sender, args) =>
            {
                if (args.SelectedItem is not NavigationViewItem item)
                    return;

                MoveToPane(item.Tag as Type);
            };
            Console.WriteLine("Reloading navbar...");
            ReloadNavbar();
            
            // Preload all panes
            Console.WriteLine("Preloading panes...");
            foreach (var item in MainNavigationView.MenuItems.Cast<NavigationViewItem>())
                MoveToPane(item.Tag as Type, false);

            #endregion
            
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    Console.WriteLine("Preloading ...");
                    await Preloader.Check();
                    Console.WriteLine("Preloader done, moving to pane.. (now " +
                                      Settings.Instance.EnabledModules.Count +
                                      " modules enabled)");
                    MoveToPane(Settings.Instance.EnabledModules.Count == 0
                        ? null
                        : (MainNavigationView.MenuItems[0] as NavigationViewItem)!.Tag as Type);

                    Console.WriteLine("Everything's loaded! Reloading home cards...");
                    if (HomePane.Instance != null!) 
                        await HomePane.Instance.ViewModel.ReloadCards();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    DebugPane.AddDebugText(e.ToString());
                }
            });

            Settings.Instance.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Settings.Instance.EnabledModules))
                {
                    Console.WriteLine("Settings changed, reloading modules... (now " +
                                      Settings.Instance.EnabledModules.Count + " modules enabled: " +
                                        string.Join(", ", Settings.Instance.EnabledModules) + ")");
                    ReloadNavbar();
                }
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            DebugPane.AddDebugText(e.ToString());
            Frame.NavigateToType(typeof(DebugPane), null, new FrameNavigationOptions
            {
                TransitionInfoOverride = new SuppressNavigationTransitionInfo()
            });
        }
    }

    public void InitializeTopLevel(TopLevel topLevel)
    {
        StorageProvider = topLevel.StorageProvider;
        Clipboard = topLevel.Clipboard ?? throw new InvalidOperationException("Clipboard is null, cannot continue");
        NotificationManager = new WindowNotificationManager(topLevel)
        {
            Position = NotificationPosition.TopCenter
        };
    }

    public void ReloadNavbar()
    {
        MainNavigationView.MenuItems.Clear();
        NavigationItemsDict.Clear();
        
        foreach (var module in WelcomeChoices.Modules)
        {
            if (Settings.Instance.EnabledModules.Contains(module.Id))
            {
                var item = new NavigationViewItem
                {
                    Content = module.Name,
                    IconSource = new SymbolIconSource
                    {
                        Symbol = module.Icon
                    },
                    Tag = module.ControlType
                };
                MainNavigationView.MenuItems.Add(item);
                NavigationItemsDict[module.ControlType] = item;   
            }
        }
    }
    
    public void MoveToPane(Type? paneType, bool animation = true)
    {
        if (paneType != null && NavigationItemsDict.TryGetValue(paneType, out var item))
        {
            if (lastPaneWasSettings)
            {
                lastPaneWasSettings = false;
                Settings.Instance.Save();
            }
            
            MainNavigationView.SelectedItem = item;
        }
        else // if (paneType == typeof(SettingsPane))
        {
            paneType = typeof(NewSettingsPane);
            MainNavigationView.SelectedItem = MainNavigationView.SettingsItem;
            lastPaneWasSettings = true;
        }

        Frame.NavigateToType(paneType, null, new FrameNavigationOptions
        {
            TransitionInfoOverride = animation ? new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromLeft
            } : new SuppressNavigationTransitionInfo()
        });
    }

    public static void ShowNotification(string title, string message, NotificationType type)
    {
        Instance.NotificationManager.Show(new Notification(title, message, type));
    }
}