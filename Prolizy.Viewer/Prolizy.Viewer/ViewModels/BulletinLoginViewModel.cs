using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Prolizy.API;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels;

public partial class BulletinLoginViewModel : ObservableObject
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

        var client = new BulletinClient
        {
            Username = Username,
            Password = Password
        };
        
        InfoBarMessage = "Connexion en cours...";
        InfoBarSeverity = InfoBarSeverity.Warning;
        
        try
        {
            var code = await client.Login();
            if (code == HttpStatusCode.Unauthorized)
                throw new Exception("Identifiants incorrects");
            var data = await client.FetchDatas();
            if (data == null)
                throw new Exception("Failed to fetch API key (null)");
            
            InfoBarMessage = $"Connexion réussie ({data.Transcript.Student.FullName})! Identifiants sauvegardés.";
            InfoBarSeverity = InfoBarSeverity.Success;
            
            Settings.Instance.BulletinUsername = Username;
            Settings.Instance.BulletinPassword = SecureStorage.EncryptPassword(Password);
            
            Settings.Instance.Save();
        }
        catch (Exception e)
        {
            InfoBarMessage = $"Impossible de se connecter: {e.Message}";
            InfoBarSeverity = InfoBarSeverity.Error;
            if (Settings.Instance.Debug)
                await Dialogs.ShowMessage("Exception", e.ToString());
        }
    }
}