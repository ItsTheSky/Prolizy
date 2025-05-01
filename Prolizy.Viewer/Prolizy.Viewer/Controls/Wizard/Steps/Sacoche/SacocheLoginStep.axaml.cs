using Avalonia.Controls;
using Prolizy.Viewer.ViewModels.SacocheWizard;

namespace Prolizy.Viewer.Controls.Wizard.Steps.Sacoche;

public partial class SacocheLoginStep : UserControl
{
    public SacocheLoginStep()
    {
        InitializeComponent();

        DataContext = new WizardLoginViewModel();
    }
}