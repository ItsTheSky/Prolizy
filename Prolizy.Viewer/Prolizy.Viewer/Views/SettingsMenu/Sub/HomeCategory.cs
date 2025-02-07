using System.Collections.Generic;
using Avalonia.Controls;

namespace Prolizy.Viewer.Views.SettingsMenu.Sub;

public partial class HomeCategory : SettingCategory
{
    public override string Title => "Accueil";
    public override string? ModuleId => "home";

    public override List<SettingEntry> Entries => [];

    public override Control CustomControl { get; } = new HomeSettingsControl();
}