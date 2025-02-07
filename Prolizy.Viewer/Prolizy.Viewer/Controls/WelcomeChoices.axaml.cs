using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Controls;

public partial class WelcomeChoices : UserControl
{
    public record Module(string Id, string Name, string Description, Symbol Icon, Type ControlType, bool ShowInWelcome = true);

    public static readonly List<Module> Modules =
    [
        new ("home", "Accueil", "Réunit les informations des différents modules", Symbol.Home, typeof(HomePane)),
        new ("edt", "Emloi du temps", "EDT CELCAT de l'IUT de Vélizy", Symbol.Calendar, typeof(TimeTablePane)),
        new ("sacoche", "Compétences", "Compétences SACoche de l'académie de Versailles", Symbol.Trophy, typeof(SacochePane)),
        new ("bulletin", "Bulletin", "Bulletins de l'IUT de Vélizy", Symbol.Document, typeof(BulletinPane)),
        new ("debug", "Debug", "Outil de débogage", Symbol.Bug, typeof(DebugPane), false)
    ];
    
    public WelcomeChoices()
    {
        InitializeComponent();

        DataContext = new WelcomeChoicesViewModel
        {
            Choices = new ObservableCollection<ModuleChoice>(Modules.Where(m => m.ShowInWelcome).Select(m => new ModuleChoice { Module = m, IsSelected = true }))
        };
    }
}

public partial class WelcomeChoicesViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ModuleChoice> _choices = [];
}

public partial class ModuleChoice : ObservableObject
{
    [ObservableProperty] private WelcomeChoices.Module _module;
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            SetProperty(ref _isSelected, value);
            switch (value)
            {
                case true when !Settings.Instance.EnabledModules.Contains(Module.Id):
                    Settings.Instance.EnabledModules.Add(Module.Id);
                    break;
                case false when Settings.Instance.EnabledModules.Contains(Module.Id):
                    Settings.Instance.EnabledModules.Remove(Module.Id);
                    break;
            }
            Settings.Instance.Save();
        }
    }
}