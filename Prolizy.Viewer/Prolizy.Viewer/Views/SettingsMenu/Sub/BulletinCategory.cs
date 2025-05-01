using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using SpacedGridControl.Avalonia;

namespace Prolizy.Viewer.Views.SettingsMenu.Sub;

public partial class BulletinCategory : SettingCategory
{
    public override string Title => "Bulletin";
    public override string? ModuleId => "bulletin";

    public override List<SettingEntry> Entries => IsModuleEnabled
        ?
        [
            new SettingEntry(this, "bulletin_ids")
            {
                Title = "Identifiants",
                Description = "Modifiez les identifiants utilisés pour accéder à votre bulletin. Sachez que ces derniers sont cryptés et uniquement stockés en local.",
                Control = new SpacedGrid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    ColumnSpacing = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Children =
                    {
                        ControlsHelper.CreateSettingButton("Modifier", BulletinPaneViewModel.ConfigureBulletin).WithColumn(0),
                        ControlsHelper.CreateSettingButton("Supprimer", new RelayCommand(() =>
                        {
                            Settings.Instance.BulletinUsername = "";
                            Settings.Instance.BulletinPassword = "";
        
                            MainView.ShowNotification("Informations effacées", "Les informations de connexion ont été effacées avec succès.", NotificationType.Success);
                        }), "danger").WithColumn(1),
                    }
                }
            }
        ]
        : [];
}