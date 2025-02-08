using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.Viewer.Controls.Wizard.Steps;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Views.SettingsMenu.Sub;

public partial class TimeTableCategory : SettingCategory
{
    public override string Title => "Emploi du temps";
    public override string? ModuleId => "edt";

    public override List<SettingEntry> Entries => !IsModuleEnabled
        ? []
        :
        [
            new SettingEntry(this, "edt_group")
            {
                Title = "Groupe",
                Description = "Votre groupe qui sera utilisé pour afficher le bonemploi du temps. Uniquement les groupes de l'IUT de Vélizy sont supportés!",
                Control = ControlsHelper.CreateSettingButton("Modifier", new AsyncRelayCommand(async () =>
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
                        var edt = TimeTablePane.Instance?.ViewModel;
                        if (edt != null)
                        {
                            edt.IsEdtAvailable = !string.IsNullOrEmpty(Settings.Instance.StudentGroup);
                            edt.RefreshAll();

                            MainView.ShowNotification("Groupe mis à jour", "Le groupe a été mis à jour avec succès.",
                                NotificationType.Success);
                        }
                    }  
                }))
            },
            new SettingEntry(this, "add_widget")
            {
                Title = "Ajouter un Widget",
                Description = "LAisser l'application proposer l'ajout d'un widget sur l'écran d'accueil, permettant de voir votre prochain cours.",
                Control = ControlsHelper.CreateSettingButton("Ajouter", new RelayCommand(() =>
                {
                    AndroidAccessManager.AndroidAccess?.RequestAddWidget();
                })),
                IsVisible =  OperatingSystem.IsAndroid()
            },
            new SettingEntry(this, "better_description")
            {
                Title = "Meilleurs Descriptions",
                Description = "Affiche des descriptions plus précises, en renvoyant une requête à l'EDT pour chaque cours. Dans 95% des cas cette option est inutile.",
                Control = ControlsHelper.CreateSettingToggleSwitch(nameof(Settings.BetterDescription))
            },
            new SettingEntry(this, "show_mode")
            {
                Title = "Affichage",
                Description = "Change le mode d'affichage de la chronologie des cours.",
                Control = ControlsHelper.CreateSettingToggleSwitch(nameof(Settings.ShowAsList),
                    "Liste", "Grille")
            },
            new SettingEntry(this, "color_scheme")
            {
                Title = "Thème de couleur",
                Description = "Change le thème de couleur de des cours/éléments de l'emploi du temps.",
                Control = ControlsHelper.CreateSettingComboBox(nameof(Settings.ColorScheme),
                    [3, 5, 6], i =>
                    {
                        return i switch
                        {
                            3 => "Défaut",
                            5 => "Ambre",
                            6 => "Écarlate",
                            _ => "Inconnu"
                        };
                    })
            },
            new SettingEntry(this, "overlay")
            {
                Title = "Superposition",
                Description = "Affiche une superposition sur les cours de l'emploi du temps pour des indications supplémentaires (exemple: cours passé, retard, absence, ...)",
                Control = ControlsHelper.CreateSettingToggleSwitch(nameof(Settings.Overlay))
            },
            
        ];

}