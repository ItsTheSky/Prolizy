using Avalonia.Controls;

namespace Prolizy.Viewer.Controls.Wizard.Steps;

public partial class WelcomeStep : UserControl
{
    public WelcomeStep()
    {
        InitializeComponent();

        Text.Text = """
                    Bienvenue dans Prolizy!

                    Cette application réunit les fonctionnalités comme l'EDT ou SACoche en une seule application.
                    Pour commencer, veuillez configurer une première fois l'application.

                    Pas de panique! On vous guide pas à pas :)
                    """;
    }
}