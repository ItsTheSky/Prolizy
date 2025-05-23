using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using Prolizy.Viewer.Controls;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views.Panes;
using Prolizy.Viewer.Views.SettingsMenu;
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
            
            // Add a safety timeout to ensure preloading state doesn't get stuck
            var preloadingTimeout = 10000; // 10 seconds
            var timeoutTimer = new System.Threading.Timer(_ => 
            {
                Dispatcher.UIThread.InvokeAsync(() => 
                {
                    if (ViewModel.IsPreLoading)
                    {
                        Console.WriteLine("Preloading timeout reached, forcing IsPreLoading to false");
                        ViewModel.IsPreLoading = false;
                    }
                });
            }, null, preloadingTimeout, System.Threading.Timeout.Infinite);
            
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    Console.WriteLine("Preloading core ...");
                    await Preloader.Check();
                    Console.WriteLine("Preloader core done, preloading panes...");

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

                    Console.WriteLine("Preloader done, moving to pane.. (now " +
                                      Settings.Instance.EnabledModules.Count +
                                      " modules enabled)");
                    LoadDefaultModule();

                    Console.WriteLine("Everything's loaded! Reloading home cards...");
                    if (HomePane.Instance != null!)
                        await HomePane.Instance.ViewModel.ReloadCards();
                    
                    ViewModel.IsPreLoading = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    DebugPane.AddDebugText(e.ToString());
                }
                finally
                {
                    Console.WriteLine("Setting IsPreLoading to false");
                    ViewModel.IsPreLoading = false;
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
            
                    // Vérifier si le module par défaut est toujours activé
                    if (!Settings.Instance.EnabledModules.Contains(Settings.Instance.DefaultModule))
                    {
                        Settings.Instance.DefaultModule = Settings.Instance.EnabledModules.Count > 0 ? Settings.Instance.EnabledModules[0] : "settings";
                        Settings.Instance.Save();
                    }
                }
            };
        }
        catch (Exception e)
        {
            ViewModel.IsPreLoading = false;
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
        
        Frame.NavigationFailed += (sender, args) =>
        {
            Console.WriteLine("Navigation failed: " + args.Exception);
            DebugPane.AddDebugText(args.Exception.ToString());
        };

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
    
    public void LoadDefaultModule()
    {
        if (Settings.Instance.EnabledModules.Count > 0)
        {
            string defaultModuleId = Settings.Instance.DefaultModule;
        
            // Vérifie si le module par défaut est activé
            if (Settings.Instance.EnabledModules.Contains(defaultModuleId))
            {
                // Trouve le module correspondant au defaultModuleId
                var defaultModule = WelcomeChoices.Modules.FirstOrDefault(m => m.Id == defaultModuleId);
                if (defaultModule != null)
                {
                    MoveToPane(defaultModule.ControlType);
                    return;
                }
            }
        
            // Fallback: affiche le premier module si le module par défaut n'est pas disponible
            if (MainNavigationView.MenuItems.Count > 0)
            {
                MoveToPane((MainNavigationView.MenuItems[0] as NavigationViewItem)!.Tag as Type);
            }
            else
            {
                // Aucun module dans la barre de navigation: afficher la page de paramètres
                MoveToPane(null);
            }
        }
        else
        {
            // Aucun module activé: afficher la page de paramètres
            MoveToPane(null);
        }
    }
    
    public MainViewModel ViewModel => (MainViewModel) DataContext!;
}