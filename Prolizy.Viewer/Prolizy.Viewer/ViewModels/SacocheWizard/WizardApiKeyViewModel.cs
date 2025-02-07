using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.API;
using Prolizy.Viewer.Controls.Wizard;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.ViewModels.Sacoche;

public partial class WizardApiKeyViewModel : ObservableObject
{
    
    [ObservableProperty] private string _apiKey;
    [ObservableProperty] private bool _showApiKey;
    
    [ObservableProperty] private string _infoBarMessage = "En attente de votre clé d'API... (cliquez sur 'Vérifier' pour continuer)";
    [ObservableProperty] private InfoBarSeverity _infoBarSeverity = InfoBarSeverity.Informational;

    [RelayCommand]
    public async Task VerifyApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            InfoBarMessage = "Veuillez renseigner une clé d'API";
            InfoBarSeverity = InfoBarSeverity.Error;
            return;
        }

        var client = new SacocheClient(apiKey: ApiKey);
        try
        {
            await client.EnsureLogout();
            var student = await client.Login();
            if (student == null)
                throw new Exception("Failed to login with the provided API key");
            
            InfoBarMessage = "Clé d'API valide (connecté en tant que " + student.FirstName + " " + student.LastName + ")";
            InfoBarSeverity = InfoBarSeverity.Success;
            Settings.Instance.SacocheApiKey = ApiKey;
            Settings.Instance.Save();
            if (WizardManager.HasBeenOpened)
                WizardManager.CanContinue(WizardManager.Finish);

            if (SacochePane.Instance != null!)
            {
                await SacochePane.Instance.ViewModel.RefreshState();
            }
        }
        catch (Exception e)
        {
            InfoBarMessage = "Clé d'API invalide";
            InfoBarSeverity = InfoBarSeverity.Error;
            if (Settings.Instance.Debug)
                await Dialogs.ShowMessage("Exception", e.ToString()); 
        }
    }

}