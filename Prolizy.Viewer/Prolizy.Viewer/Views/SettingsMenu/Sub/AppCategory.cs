using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.Viewer.Controls;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Views.SettingsMenu.Sub;

public partial class AppCategory : SettingCategory
{
    public override string Title => "Application";
    public override string? ModuleId => null;
    
    private static readonly string[] _newThemes =
    {
        "emerald", "sky", "rose", 
        "cappuccino", "tiramisu",
        "midnight", "deep_ocean"
    };

    public override List<SettingEntry> Entries =>
    [
        new (this, "color_scheme")
        {
            Title = "Thème de couleur",
            Description = "Le thème général de couleur de l'application: change le fond, les dégradés et la couleur d'accent sur les différents éléments.",
            Control = ControlsHelper.CreateSettingComboBox(nameof(Settings.ThemeScheme), [
                    "emerald", "sky", "rose",
                    "cappuccino", "tiramisu",
                    "midnight", "deep_ocean", 
                    "purple", "cyan", "green", "red", "base"],
                choice => CreateThemeEntry(choice switch
                {
                    "midnight" => "Minuit",
                    "deep_ocean" => "Océan Profond",
                    "cappuccino" => "Cappuccino",
                    "tiramisu" => "Tiramisu",
                    "emerald" => "Émeraude",
                    "sky" => "Ciel",
                    "rose" => "Rose",
                    "purple" => "Violet",
                    "cyan" => "Cyan",
                    "green" => "Vert",
                    "red" => "Rouge",
                    "base" => "Violet (Sans Dégradé)",
                    _ => "Inconnu"
                }, _newThemes.Contains(choice)), onChanged: () => ColorSchemeManager.ApplyTheme(Settings.Instance.ThemeScheme)),
        },
        new (this, "default_module")
        {
            Title = "Module par défaut",
            Description = "Choisissez le module qui sera affiché par défaut au démarrage de l'application.",
            Control = ControlsHelper.CreateSettingComboBox(nameof(Settings.DefaultModule), 
                WelcomeChoices.Modules.Where(m => m.ShowInWelcome).Select(m => m.Id).ToList(),
                moduleId => {
                    var module = WelcomeChoices.Modules.FirstOrDefault(m => m.Id == moduleId);
                    return module?.Name ?? "Inconnu";
                },
                onChanged: ValidateDefaultModule)
        },
        new (this, "debug_mode")
        {
            Title = "Mode de débogage",
            Description = "Active le mode de débogage pour afficher des informations supplémentaires et ajouter une page de débogage.",
            Control = ControlsHelper.CreateSettingToggleSwitch(nameof(Settings.Debug), onChanged: () =>
            {
                Settings.Instance.AnonymousMode = false;
                if (Settings.Instance.Debug)
                    Settings.Instance.EnabledModules.Add("debug");
                else
                    Settings.Instance.EnabledModules.Remove("debug");
            })
        },
        new (this, "anonymous_mode")
        {
            Title = "Mode anonyme",
            Description = "Active le mode anonyme: cache les nom/prénom de l'utilisateur connecté au bulletin et à SACoche.",
            Control = ControlsHelper.CreateSettingToggleSwitch(nameof(Settings.AnonymousMode), 
                onChanged: () => MainView.ShowNotification("Redémarrage nécessaire", 
                    "Le mode anonyme nécessite un redémarrage de l'application pour être pris en compte.", NotificationType.Warning)),
            IsVisible = Settings.Instance.Debug
        },
        new (this, "open_data_folder")
        {
            Title = "Ouvrir le dossier de données",
            Description = "Ouvre le dossier de données de l'application dans l'explorateur de fichiers. Ne marche que sous Windows & Android.",
            Control = ControlsHelper.CreateSettingButton("Ouvrir le dossier", new RelayCommand(() =>
            {
                var path = Paths.Build();
                DebugPane.AddDebugText("Opening folder: " + path);
                if (OperatingSystem.IsAndroid())
                    AndroidAccessManager.AndroidAccess?.OpenFolder(Paths.Build("appsettings.json"));
                else if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("explorer.exe", path));
            })),
            IsVisible = Settings.Instance.Debug
        },
        new (this, "force_refresh_widget")
        {
            Title = "Rafrachir le widget",
            Description = "Force le rafraîchissement de tous les widgets de l'application.",
            Control = ControlsHelper.CreateSettingButton("Rafraîchir", new AsyncRelayCommand(async () =>
            {
                await TimeTablePane.Instance.ViewModel.UpdateAndroidWidget();
            })),
            IsVisible = Settings.Instance.Debug && OperatingSystem.IsAndroid()
        },
        new (this, "test_notification")
        {
            Title = "Tester les notifications",
            Description = "Petit test de notification (android) pour vérifier que les notifications fonctionnent.",
            Control = ControlsHelper.CreateSettingButton("Tester", new AsyncRelayCommand(async () =>
            {
                if (AndroidAccessManager.AndroidAccess != null)
                {
                    if (!AndroidAccessManager.AndroidAccess.IsNotificationPermissionGranted())
                    {
                        await Dialogs.AskChoice("Permission de notification",
                            "L'application n'a pas la permission de notification. Voulez-vous l'activer ?",
                            "Oui", "Non", granted =>
                            {
                                if (granted)
                                    AndroidAccessManager.AndroidAccess?.AskForNotificationPermission();
                            });
                        if (!AndroidAccessManager.AndroidAccess.IsNotificationPermissionGranted())
                            return;
                    }
                    
                    AndroidAccessManager.AndroidAccess?.ShowNotification(new AndroidNotification(
                        "Test de notification",
                        "Ceci est un test de notification.", NotificationChannel.UpdateBulletin));
                }
            })),
            IsVisible = Settings.Instance.Debug && OperatingSystem.IsAndroid()
        }
    ];

    private Control CreateThemeEntry(string theme, bool isNew)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Spacing = 5,

            Children =
            {
                new TextBlock
                {
                    Text = theme,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                },
                new InfoBadge()
                {
                    IsVisible = isNew,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    IconSource = new FontIconSource
                    {
                        Glyph = "New!",
                    }
                }
            }
        };
    }

    private void ValidateDefaultModule()
    {
        string currentDefaultModule = Settings.Instance.DefaultModule;
    
        // Vérifie si le module par défaut est toujours activé
        if (!Settings.Instance.EnabledModules.Contains(currentDefaultModule))
        {
            // Si le module n'est pas activé, choisis le premier module activé comme défaut
            if (Settings.Instance.EnabledModules.Count > 0)
            {
                Settings.Instance.DefaultModule = Settings.Instance.EnabledModules[0];
                MainView.ShowNotification("Module par défaut modifié", 
                    "Le module par défaut a été changé car celui sélectionné n'est pas activé.", 
                    NotificationType.Warning);
            }
            else
            {
                // Si aucun module n'est activé, on utilise "home" par défaut
                Settings.Instance.DefaultModule = "home";
            }
            Settings.Instance.Save();
        }
    }
}