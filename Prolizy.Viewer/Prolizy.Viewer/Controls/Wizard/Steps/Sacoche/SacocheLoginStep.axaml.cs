using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Prolizy.Viewer.ViewModels.Sacoche;

namespace Prolizy.Viewer.Controls.Wizard.Steps;

public partial class SacocheLoginStep : UserControl
{
    public SacocheLoginStep()
    {
        InitializeComponent();

        DataContext = new WizardLoginViewModel();
    }
}