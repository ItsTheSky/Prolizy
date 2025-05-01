using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.API;
using Prolizy.Viewer.Controls.Wizard;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels.SacocheWizard;

public partial class WizardLoginViewModel : ObservableObject
{
    [ObservableProperty] private string _password;
    [ObservableProperty] private string _username;
    [ObservableProperty] private bool _showPassword;
    
    [ObservableProperty] private string _infoBarMessage = "En attente ...";
    [ObservableProperty] private InfoBarSeverity _infoBarSeverity = InfoBarSeverity.Informational;

    [RelayCommand]
    public async Task FetchApiKey()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            InfoBarMessage = "Veuillez renseigner un nom d'utilisateur et un mot de passe";
            InfoBarSeverity = InfoBarSeverity.Error;
            return;
        }

        var client = new SacocheClient();
        
        InfoBarMessage = "Récupération de la clé d'API... (cela peut prendre quelques secondes)";
        InfoBarSeverity = InfoBarSeverity.Warning;
        
        try
        {
            var apiKey = await client.FetchApiKey(Username, Password);
            if (apiKey == null)
                throw new Exception("Failed to fetch API key");
            
            InfoBarMessage = "Clé d'API récupérée avec succès! Elle a été sauvegardée.";
            InfoBarSeverity = InfoBarSeverity.Success;
            
            Settings.Instance.SacocheApiKey = apiKey;
            Settings.Instance.Save();
            if (WizardManager.HasBeenOpened)
                WizardManager.CanContinue(WizardManager.Finish);
        }
        catch (Exception e)
        {
            InfoBarMessage = "Échec de la récupération de la clé d'API, veuillez vérifier vos identifiants";
            InfoBarSeverity = InfoBarSeverity.Error;
            if (Settings.Instance.Debug)
                await Dialogs.ShowMessage("Exception", e.ToString());
        }
    }
    
}