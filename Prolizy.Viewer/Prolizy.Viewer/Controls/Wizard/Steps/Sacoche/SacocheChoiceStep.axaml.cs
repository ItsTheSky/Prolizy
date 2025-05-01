using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace Prolizy.Viewer.Controls.Wizard.Steps.Sacoche;

public partial class SacocheChoiceStep : UserControl
{
    public static SacocheChoiceStep Instance { get; private set; }
    
    public ContentDialog CurrentDialog { get; set; }
    public SacocheChoiceStep()
    {
        InitializeComponent();
        
        Instance = this;
        DataContext = this;
        
        DirectLoginRadio.IsCheckedChanged += (sender, args) =>
        {
            if (DirectLoginRadio.IsChecked == true)
            {
                ApiKeyRadio.IsChecked = false;
                CurrentDialog.IsPrimaryButtonEnabled = true;
            }
        };
        
        ApiKeyRadio.IsCheckedChanged += (sender, args) =>
        {
            if (ApiKeyRadio.IsChecked == true)
            {
                DirectLoginRadio.IsChecked = false;
                CurrentDialog.IsPrimaryButtonEnabled = true;
            }
        };
    }

    public string Explanation => """
                                 Choisissez ci-dessous la méthode de connexion voulue pour SACoche.
                                 Si vous avez déjà une clé d'API, alors prenez la seconde option et renseignez la clé.
                                 Dans le cas contraire, ou si vous n'avez pas connaissance de ce qu'est une clé d'API, alors prenez la première option.
                                 """;

    public static void Refresh()
    {
        if (Instance.DirectLoginRadio.IsChecked == true)
            WizardManager.CanContinue(WizardManager.SacocheLogin);
        else if (Instance.ApiKeyRadio.IsChecked == true)
            WizardManager.CanContinue(WizardManager.SacocheApiKey);
        else
            WizardManager.CannotContinue();
    }
}