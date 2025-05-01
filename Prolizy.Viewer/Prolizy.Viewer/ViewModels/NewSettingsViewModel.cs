using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using Prolizy.Viewer.Views.SettingsMenu;

namespace Prolizy.Viewer.ViewModels;

public partial class NewSettingsViewModel(NewSettingsPane pane, Frame frame) : ObservableObject
{
    #region Navigation

    [ObservableProperty] private string _displayTitle = "Paramètres";
    [ObservableProperty] private bool _canGoBack;

    [RelayCommand]
    public void Navigate(Type settingType)
    {
        if (Activator.CreateInstance(settingType) is not SettingCategory obj)
            return;
        
        CurrentCategory = obj;
        
        frame.NavigateToType(typeof(SettingsSub), null, new FrameNavigationOptions
        {
            IsNavigationStackEnabled = false,
            TransitionInfoOverride = new EntranceNavigationTransitionInfo()
            {
                FromHorizontalOffset = 200,
                FromVerticalOffset = 0
            }
        });

        CanGoBack = true;
    }

    [RelayCommand]
    public void GoBack()
    {
        CurrentCategory = null;
        CanGoBack = false;
        
        frame.NavigateToType(typeof(SettingsMenu), null, new FrameNavigationOptions
        {
            IsNavigationStackEnabled = false,
            TransitionInfoOverride = new EntranceNavigationTransitionInfo()
            {
                FromHorizontalOffset = -200,
                FromVerticalOffset = 0
            }
        });
    }

    #endregion

    #region Core

    [ObservableProperty] private SettingCategory? _currentCategory;
    
    [RelayCommand]
    public async Task OpenCredits()
    {
        var grid = Utilities.Controls.CreateDataGrid(new Dictionary<string, string>
        {
            {"Nom", "Prolizy"},
            {"Version", Assembly.GetExecutingAssembly().GetName().Version!.ToString()},
            {"Auteur", "Nicolas RACOT"},
            {"Licence", "MIT"}
        });
        var text = new TextBlock
        {
            Text = "Prolizy est un projet développé par moi-même (Nicolas RACOT) dans mon temps libre, pour faciliter l'accès aux outils numériques de l'IUT de Vélizy. Je ne suis en aucun cas affilié à l'IUT de Vélizy ou à l'Université Paris-Saclay. Si vous avez des questions ou des suggestions, n'hésitez pas à me contacter par Discord: itsthesky",
            TextWrapping = TextWrapping.Wrap
        };
        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(text);
        
        var dialog = new ContentDialog
        {
            Title = "Crédits",
            Content = stack,
            CloseButtonText = "Fermer"
        };
        
        await dialog.ShowAsync();
    }

    #endregion
}