using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.Viewer.Controls.Wizard.Steps;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.ViewModels.Sacoche;
using SpacedGridControl.Avalonia;

namespace Prolizy.Viewer.Views.SettingsMenu.Sub;

public partial class SacocheCategory : SettingCategory
{
    public override string Title => "SACoche";
    public override string? ModuleId => "sacoche";

    public override List<SettingEntry> Entries => IsModuleEnabled
        ?
        [
            new SettingEntry(this, "sacoche_keys")
            {
                Title = "Identifiants/Clé d'API",
                Description = "Modifiez le compte (via vos identifiants ou clé d'API) utilisé pour accéder à SACoche. vous pouvez également les supprimer.",
                Control = new SpacedGrid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    ColumnSpacing = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Children =
                    {
                        ControlsHelper.CreateSettingButton("Modifier", SacochePaneViewModel.ConfigureSacocheCommand).WithColumn(0),
                        ControlsHelper.CreateSettingButton("Supprimer", new AsyncRelayCommand(async () =>
                        {
                            Settings.Instance.SacocheApiKey = "";
                            MainView.Instance.NotificationManager.Show(new Notification("Clé d'API effacée", "La clé d'API a été effacée avec succès.", NotificationType.Success));
                        }), "danger").WithColumn(1),
                    }
                }
            }
        ]
        : [];
}