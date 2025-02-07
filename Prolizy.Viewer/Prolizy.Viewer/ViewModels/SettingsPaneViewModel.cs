using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.Viewer.Controls.Wizard;
using Prolizy.Viewer.Controls.Wizard.Steps;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.ViewModels;

public partial class SettingsPaneViewModel : ObservableObject
{
    
    public SettingsPaneViewModel()
    {
        Debug = Settings.Instance.Debug;

        ShowAsList = Settings.Instance.ShowAsList;
        Caching = Settings.Instance.Caching;
        BetterDescription = Settings.Instance.BetterDescription;
        SelectedIndex = Settings.Instance.ColorScheme switch
        {
            3 => 0,
            5 => 1,
            6 => 2,
            _ => 0
        };
    }

    #region Core

    private bool _debug;
    public bool Debug
    {
        get => _debug;
        set
        {
            SetProperty(ref _debug, value);
            Settings.Instance.Debug = value;
            if (value)
                Settings.Instance.EnabledModules.Add("debug");
            else
                Settings.Instance.EnabledModules.Remove("debug");
        }
    }
    
    public bool AnonymousMode
    {
        get => Settings.Instance.AnonymousMode;
        set
        {
            Settings.Instance.AnonymousMode = value;
            Settings.Instance.Save();
            OnPropertyChanged();
            
            MainView.ShowNotification("Redémarrage nécessaire", "Pour appliquer les changements, veuillez redémarrer l'application.", NotificationType.Warning);
        }
    }

    #endregion

    #region EDT

    private bool _showAsList;
    public bool ShowAsList
    {
        get => _showAsList;
        set
        {
            SetProperty(ref _showAsList, value);
            Settings.Instance.ShowAsList = value;
        }
    }
    
    private bool _caching;
    public bool Caching
    {
        get => _caching;
        set
        {
            SetProperty(ref _caching, value);
            Settings.Instance.Caching = value;
        }
    }
    
    private bool _betterDescription;
    public bool BetterDescription
    {
        get => _betterDescription;
        set
        {
            SetProperty(ref _betterDescription, value);
            Settings.Instance.BetterDescription = value;
            if (value)
            {
                MainView.ShowNotification("Attention!", "Cette option va envoyer une nouvelle requête à l'EDT pour chaque cours, ainsi le temps de chargement de l'emploi du temps sera plus long.",
                    NotificationType.Warning);
            }
        }
    }
    
    public bool Overlay
    {
        get => Settings.Instance.Overlay;
        set
        {
            Settings.Instance.Overlay = value;
            Settings.Instance.Save();
            OnPropertyChanged();
            if (!value)
            {
                // on désactive le lien
                IsEditLinked = false;
            }
        }
    }

    [ObservableProperty] private int _selectedIndex = 3;
    private ComboBoxItem _colorScheme;
    public ComboBoxItem ColorScheme
    {
        get => _colorScheme;
        set
        {
            SetProperty(ref _colorScheme, value);
            
            var tag = value.Tag!.ToString();
            Settings.Instance.ColorScheme = int.Parse(tag);
        }
    }

    #endregion

    #region Bulletin

    public bool IsEditLinked
    {
        get => Settings.Instance.LinkEdt;
        set
        {
            if (value && !Overlay)
            {
                Overlay = true;
            }
            Settings.Instance.LinkEdt = value;
            Settings.Instance.Save();
            OnPropertyChanged();
            if (value)
            {
                MainView.ShowNotification("Attention!", "Soyez sûr que le groupe renseigné dans les paramètres de l'emploi du temps est le même que celui de votre bulletin.",
                    NotificationType.Warning);
            }
        }
    }

    #endregion
    
    [RelayCommand]
    public async Task OpenWizard()
    {
        await WizardManager.ShowWizard(true);
    }

    #region SACoche

    [RelayCommand]
    public async Task ClearApiKey()
    {
        Settings.Instance.SacocheApiKey = "";
        MainView.Instance.NotificationManager.Show(new Notification("Clé d'API effacée", "La clé d'API a été effacée avec succès.", NotificationType.Success));
    }

    #endregion

    #region EDT

    [RelayCommand]
    public async Task OpenEditGroupDialog()
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
    }

    #endregion

    #region Bulletin

    [RelayCommand]
    public async Task OpenBulletinWizard()
    {
        var dialog = new ContentDialog
        {
            Title = "Connexion au bulletin",
            Content = new BulletinLoginDialog(),
            CloseButtonText = "Fermer"
        };
        
        await dialog.ShowAsync();
    }
    
    [RelayCommand]
    public void ClearBulletinCredentials()
    {
        Settings.Instance.BulletinUsername = "";
        Settings.Instance.BulletinPassword = "";
        
        MainView.ShowNotification("Informations effacées", "Les informations de connexion ont été effacées avec succès.", NotificationType.Success);
    }

    #endregion

    [RelayCommand] public void EnableEdtModule() { EdtModuleEnabled = true; }
    
    [RelayCommand] public void DisableEdtModule() { EdtModuleEnabled = false; }
    
    [RelayCommand] public void EnableSacModule() { SacModuleEnabled = true; }
    [RelayCommand] public void DisableSacModule() { SacModuleEnabled = false; }
    
    [RelayCommand] public void EnableBulletinModule() { BulletinModuleEnabled = true; }
    [RelayCommand] public void DisableBulletinModule() { BulletinModuleEnabled = false; }

    [RelayCommand]
    public async Task OpenDataFolder()
    {
        if (OperatingSystem.IsWindows())
        {
            System.Diagnostics.Process.Start("explorer.exe", Paths.Build());
        } 
        else
        {
            MainView.ShowNotification("Erreur", "Cette fonctionnalité n'est pas disponible sur votre système.", NotificationType.Error);
        }
    }

    [RelayCommand]
    public async Task DisplayCredits()
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
            Text = "Prolizy est un projet développé par moi-même (Nicolas RACOT) dans mon temps libre.  pour faciliter l'accès aux outils numériques de l'IUT de Vélizy. Je ne suis en aucun cas affilié à l'IUT de Vélizy ou à l'Université Paris-Saclay. Si vous avez des questions ou des suggestions, n'hésitez pas à me contacter par Discord: itsthesky",
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

    #region Module Activation

    public bool EdtModuleEnabled
    {
        get => Settings.Instance.EnabledModules.Contains("edt");
        set
        {
            if (value)
                Settings.Instance.EnabledModules.Add("edt");
            else
                Settings.Instance.EnabledModules.Remove("edt");
            OnPropertyChanged();
            Settings.Instance.Save();
        }
    }
    
    public bool SacModuleEnabled
    {
        get => Settings.Instance.EnabledModules.Contains("sacoche");
        set
        {
            if (value)
                Settings.Instance.EnabledModules.Add("sacoche");
            else
                Settings.Instance.EnabledModules.Remove("sacoche");
            OnPropertyChanged();
            Settings.Instance.Save();
        }
    }
    
    public bool BulletinModuleEnabled
    {
        get => Settings.Instance.EnabledModules.Contains("bulletin");
        set
        {
            if (value)
                Settings.Instance.EnabledModules.Add("bulletin");
            else
                Settings.Instance.EnabledModules.Remove("bulletin");
            OnPropertyChanged();
            Settings.Instance.Save();
        }
    }

    #endregion
}