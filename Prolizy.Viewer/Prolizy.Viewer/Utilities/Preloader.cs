using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Prolizy.API;
using Prolizy.Viewer.Controls;
using Prolizy.Viewer.Views;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Utilities;

/// <summary>
/// Manager for the preload of the app.
/// This class will handle data's configuration in the case the app
/// is launched for the first time or the settings file is missing.
///
/// It'll help the user set up its group, Sacoche API key and other settings.
/// </summary>
public static class Preloader
{

    public static async Task Check()
    {
        if (Settings.Instance.IsFirstLaunch)
        {
            MainView.Instance.MoveToPane(typeof(TimeTablePane), true);
            await Dialogs.ShowMessage("Bienvenue!", new WelcomeChoices());
            Settings.Instance.IsFirstLaunch = false;
            Settings.Instance.Save();
        }
        else
        {
            var apiKey = Settings.Instance.SacocheApiKey!;
            var client = new SacocheClient(apiKey: apiKey);
            try
            {
                await client.EnsureLogout();
                await client.Login();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Connexion déjà établie")) 
                    return;
                
                Settings.Instance.SacocheApiKey = null;
                if (SacochePane.Instance != null!) 
                    await SacochePane.Instance.ViewModel.RefreshState();
                //await Dialogs.ShowMessage("Erreur", "La clé API SACoche n'est plus valide. Veuillez la changer ou vous reconnecter pour que SACoche puisse fonctionner.");
                if (Settings.Instance.Debug)
                {
                    await Dialogs.ShowMessage("Erreur", e.ToString());
                }
            }
        }
    }
    
}