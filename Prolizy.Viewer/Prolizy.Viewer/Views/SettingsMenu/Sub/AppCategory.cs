using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Views.SettingsMenu.Sub;

public partial class AppCategory : SettingCategory
{
    public override string Title => "Application";
    public override string? ModuleId => null;

    public override List<SettingEntry> Entries =>
    [
        new (this, "color_scheme")
        {
            Title = "Thème de couleur",
            Description = "Le thème général de couleur de l'application: change le fond, les dégradés et la couleur d'accent sur les différents éléments.",
            Control = ControlsHelper.CreateSettingComboBox(nameof(Settings.ThemeScheme), ["purple", "cyan", "green", "red", "base"],
                choice => choice switch
                {
                    "purple" => "Violet",
                    "cyan" => "Cyan",
                    "green" => "Vert",
                    "red" => "Rouge",
                    "base" => "Violet (Sans Dégradé)",
                    _ => "Inconnu"
                }, onChanged: () => ColorSchemeManager.ApplyTheme(Settings.Instance.ThemeScheme)),
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

}